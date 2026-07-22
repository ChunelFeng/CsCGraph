namespace CsCGraph;

public sealed class GElementRelation
{
    public IReadOnlyList<GElement> Predecessors { get; internal init; } = [];
    public IReadOnlyList<GElement> Successors { get; internal init; } = [];
    public IReadOnlyList<GElement> Children { get; internal init; } = [];
    public GElement? Belong { get; internal init; }
}
