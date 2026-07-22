using CsCGraph;

internal static class Program
{
    private static int TutorialMutable()
    {
        var pipeline = new GPipeline();

        var mutableB = pipeline.CreateGGroup<MyMutable>([
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB1")),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("nodeB2")),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB3"))]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGGroup(mutableB, [a], "mutableB");
        status += pipeline.RegisterGElement<MyNode2>(out var c, [a], "nodeC");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out _, [mutableB, c], "nodeD");

        status += pipeline.Process(6);

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialMutable();
    }
}
