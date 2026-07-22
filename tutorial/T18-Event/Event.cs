using CsCGraph;

internal static class Program
{
    private static int TutorialEvent()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyWriteParamNode>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyEventNode>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyNode1>(out _, [b], "nodeC");
        status += pipeline.RegisterGElement<MyEventNode>(out _, [b], "nodeD");

        status += pipeline.AddGEvent<MyPrintEvent>("my-print-event");

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialEvent();
    }
}
