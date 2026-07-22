namespace CsCGraph;

public abstract class GPassedParam
{
    public abstract void Clone(GPassedParam param);

    internal static T CopyOf<T>(T source) where T : GPassedParam, new()
    {
        var copy = new T();
        copy.Clone(source);
        return copy;
    }
}
