namespace CsCGraph;

public sealed class GFence : GAdapter
{
    private readonly HashSet<GElement> _waitElements =
        new(ReferenceEqualityComparer.Instance);

    public GFence()
        : base(GElementType.Fence)
    {
    }

    public GFence WaitGElement(GElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.GetTimeout() <= 0)
        {
            throw new InvalidOperationException(
                $"fence accepts timeout elements only: [{element.GetName()}]");
        }

        _waitElements.Add(element);
        return this;
    }

    public GFence WaitGElements(IEnumerable<GElement> elements)
    {
        foreach (var element in elements) WaitGElement(element);
        return this;
    }

    public void Clear() => _waitElements.Clear();

    protected internal override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        var status = new CStatus();
        foreach (var element in _waitElements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            status += await element.WaitTimeoutTaskAsync().ConfigureAwait(false);
        }

        return status;
    }

}
