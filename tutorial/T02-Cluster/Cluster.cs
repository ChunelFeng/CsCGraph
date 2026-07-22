using CsCGraph;

internal static class Program
{
    private static int TutorialCluster()
    {
        var pipeline = new GPipeline();

        var cluster = pipeline.CreateGGroup<GCluster>(
        [
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB1", 1)),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB2", 3)),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("nodeB3", 1)),
        ]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA", 1);
        status += pipeline.RegisterGElement(cluster, [a], "clusterB", 2);
        status += pipeline.RegisterGElement<MyNode1>(out var c, [a], "nodeC", 1);
        status += pipeline.RegisterGElement<MyNode2>(out _, [cluster, c], "nodeD", 2);

        status += pipeline.Process();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline process status is : [{status.GetCode()}]");

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialCluster();
    }
}
