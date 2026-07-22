using CsCGraph;

public sealed class MyParam : GParam
{
    public int IValue { get; set; }
    public int ICount { get; set; }

    protected override void Reset(CStatus curStatus)
    {
        IValue = 0;
    }
}
