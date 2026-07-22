using CsCGraph;

public sealed class MyEventNode : GNode
{
    protected override CStatus Run()
        => Notify("my-print-event");
}
