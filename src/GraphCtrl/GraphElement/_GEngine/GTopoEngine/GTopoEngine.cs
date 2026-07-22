namespace CsCGraph;

internal sealed class GTopoEngine : GEngine
{
    private readonly AsyncManualResetEvent _dispatchGate;
    private readonly GConcurrencyGate _concurrencyGate;
    private GElement[] _elements = [];

    internal GTopoEngine(
        AsyncManualResetEvent dispatchGate,
        GConcurrencyGate concurrencyGate)
    {
        _dispatchGate = dispatchGate;
        _concurrencyGate = concurrencyGate;
    }

    internal override CStatus Setup(IReadOnlyList<GElement> elements)
    {
        _elements = elements.ToArray();
        return new CStatus();
    }

    internal override async ValueTask<CStatus> RunAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var status = new CStatus();
            foreach (var element in _elements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _dispatchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                await _concurrencyGate.EnterAsync().ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    status += await element.FatRunAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _concurrencyGate.Exit();
                }
                if (status.IsErr()) break;
            }
            return status;
        }
        catch (OperationCanceledException)
        {
            return new CStatus("topological engine run cancelled");
        }
    }
}
