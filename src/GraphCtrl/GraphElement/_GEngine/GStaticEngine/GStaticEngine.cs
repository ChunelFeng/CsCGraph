namespace CsCGraph;

internal sealed class GStaticEngine : GEngine
{
    private readonly GDynamicEngine _engine;

    internal GStaticEngine(
        AsyncManualResetEvent dispatchGate,
        GConcurrencyGate concurrencyGate)
    {
        _engine = new GDynamicEngine(dispatchGate, concurrencyGate);
    }

    internal override CStatus Setup(IReadOnlyList<GElement> elements)
        => _engine.Setup(elements);

    internal override ValueTask<CStatus> RunAsync(CancellationToken cancellationToken)
        => _engine.RunAsync(cancellationToken);
}
