namespace CsCGraph;

public sealed class GNodeInfo
{
    public GNodeInfo(string name = "", int loop = GElementDefaults.CGraphDefaultLoopTimes)
    {
        Name = name;
        Loop = loop;
    }

    public GNodeInfo(
        IReadOnlyCollection<GElement>? dependence,
        string name = "",
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
    {
        Dependence = dependence ?? [];
        Name = name;
        Loop = loop;
    }

    public string Name { get; }
    public int Loop { get; }
    public IReadOnlyCollection<GElement> Dependence { get; } = [];
}
