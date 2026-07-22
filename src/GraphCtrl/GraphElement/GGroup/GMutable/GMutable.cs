namespace CsCGraph;

public abstract class GMutable : GGroup
{
    private readonly HashSet<GElement> _active =
        new(ReferenceEqualityComparer.Instance);

    protected GMutable()
        : base(GElementType.Mutable)
    {
    }

    protected abstract CStatus Reshape(IReadOnlyList<GElement> elements);

    protected CStatus Link(GElement before, GElement after, int beforeLoop = 1)
    {
        Activate(before, beforeLoop);
        Activate(after);
        return after.AddDependencyInternal(before);
    }

    protected void Activate(GElement element, int loop = 1)
    {
        element.SetLoopInternal(loop);
        _active.Add(element);
    }

    protected internal override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        _active.Clear();
        foreach (var child in Children) child.ResetRelationsInternal();
        var status = Reshape(Children);
        if (status.IsErr()) return status;

        status = GElementSorter.Sort(_active.ToArray(), out var sorted);
        if (status.IsErr()) return status;
        var engine = new GDynamicEngine(
            new AsyncManualResetEvent(),
            new GConcurrencyGate());
        status = engine.Setup(sorted);
        return status.IsErr()
            ? status
            : await engine.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
