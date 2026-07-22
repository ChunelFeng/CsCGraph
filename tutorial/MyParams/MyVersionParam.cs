using CsCGraph;

public sealed class MyVersionParam : GPassedParam
{
    public const string Key = "version";
    public int Priority { get; set; }
    public int Secondary { get; set; }
    public override void Clone(GPassedParam param)
    {
        var source = (MyVersionParam)param;
        Priority = source.Priority;
        Secondary = source.Secondary;
    }
}
