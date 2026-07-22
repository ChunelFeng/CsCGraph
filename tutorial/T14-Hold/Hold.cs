using CsCGraph;

internal static class Program
{
    private static int TutorialHold()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyHoldNode>(out var hold, [], "myHold");
        status += pipeline.RegisterGElement<MyNode1>(out _, [hold], "node1");

        status += pipeline.Process(3);

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialHold();
    }
}
