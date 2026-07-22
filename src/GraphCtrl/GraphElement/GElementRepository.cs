namespace CsCGraph;

internal sealed class GElementRepository
{
    private readonly HashSet<GElement> _elements =
        new(ReferenceEqualityComparer.Instance);
    private int _state = (int)GElementState.Normal;

    internal CStatus Insert(GElement element)
        => _elements.Add(element)
            ? new CStatus()
            : new CStatus($"element [{element.GetName()}] registered twice");

    internal bool Find(GElement element) => _elements.Contains(element);

    internal void PushAllState(GElementState state)
    {
        foreach (var element in _elements)
        {
            element.SetState(state);
        }

        Interlocked.Exchange(ref _state, (int)state);
    }

    internal bool IsCancelState()
        => (GElementState)Volatile.Read(ref _state) == GElementState.Cancel;

    internal GElementState CurrentState
        => (GElementState)Volatile.Read(ref _state);

    internal void Clear()
    {
        _elements.Clear();
        Interlocked.Exchange(ref _state, (int)GElementState.Normal);
    }
}
