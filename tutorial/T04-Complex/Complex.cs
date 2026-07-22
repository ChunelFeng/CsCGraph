using CsCGraph;

internal static class Program
{
    private static int TutorialComplex()
    {
        var pipeline = new GPipeline();

        var clusterB = pipeline.CreateGGroup<GCluster>(
        [
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB1", 1)),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB2", 3)),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("nodeB3", 1)),
        ]);
        var d1 = pipeline.CreateGNode<MyNode1>(new GNodeInfo([], "nodeD1", 1));
        var d2 = pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeD2", 1));
        var d3 = pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeD3", 1));
        var clusterD23 = pipeline.CreateGGroup<GCluster>([d2, d3], [d1], "clusterD23", 1);
        var d4 = pipeline.CreateGNode<MyNode2>(new GNodeInfo([d1], "nodeD4", 1));
        var regionD = pipeline.CreateGGroup<GRegion>([d1, clusterD23, d4]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA", 1);
        status += pipeline.RegisterGElement(clusterB, [], "clusterB", 1);
        status += pipeline.RegisterGElement<MyNode1>(out var c, [a, clusterB], "nodeC", 1);
        status += pipeline.RegisterGElement(regionD, [a, clusterB], "regionD", 2);
        status += pipeline.RegisterGElement<MyNode1>(out _, [c, regionD], "nodeE", 1);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialComplex();
    }
}
