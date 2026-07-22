namespace CsCGraph;

public abstract class GCondition : GGroup
{
    protected GCondition()
        : base(GElementType.Condition)
    {
    }

    protected abstract int Choose();

    protected int GetChildrenCount() => Children.Count;

    protected internal override ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        var index = Choose();
        if (index == GConditionIndex.Last)
        {
            index = Children.Count - 1;
        }

        return index < 0 || index >= Children.Count
            ? ValueTask.FromResult(new CStatus())
            : Children[index].FatRunAsync(cancellationToken);
    }

    internal override bool IsSeparate(GElement first, GElement second) => true;
}
