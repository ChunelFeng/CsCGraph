namespace CsCGraph;

public abstract class GraphManager<T>
{
    public virtual CStatus Add(T item) => new("add is not supported");

    public virtual CStatus Remove(T item) => new("remove is not supported");

    public virtual bool Find(T item) => false;

    public abstract CStatus Clear();

    public virtual int GetSize() => 0;
}
