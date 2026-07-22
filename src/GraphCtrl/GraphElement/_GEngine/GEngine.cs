namespace CsCGraph;

internal abstract class GEngine
{
    internal abstract CStatus Setup(IReadOnlyList<GElement> elements);

    internal abstract ValueTask<CStatus> RunAsync(CancellationToken cancellationToken);
}
