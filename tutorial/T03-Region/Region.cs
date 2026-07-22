using CsCGraph;

internal static class Program
{
    private static int TutorialRegion()
    {
        var pipeline = new GPipeline();

        var b1 = pipeline.CreateGNode<MyNode1>(new GNodeInfo([], "nodeB1", 1));
        var b2 = pipeline.CreateGNode<MyNode2>(new GNodeInfo([b1], "nodeB2", 2));
        var b3 = pipeline.CreateGNode<MyNode1>(new GNodeInfo([b1], "nodeB3", 1));
        var b4 = pipeline.CreateGNode<MyNode1>(new GNodeInfo([b2, b3], "nodeB4", 1));
        var region = pipeline.CreateGGroup<GRegion>([b1, b2, b3, b4]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA", 1);
        status += pipeline.RegisterGElement(region, [a], "regionB", 2);
        status += pipeline.RegisterGElement<MyNode2>(out _, [region], "nodeC", 1);

        status += pipeline.Process();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline process status is : [{status.GetCode()}]");

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialRegion();
    }
}
