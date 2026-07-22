using CsCGraph;

internal static class Program
{
    private static int TutorialAspect()
    {
        var pipeline = new GPipeline();

        var b1 = pipeline.CreateGNode<MyNode1>(new GNodeInfo([], "nodeB1", 1));
        var b2 = pipeline.CreateGNode<MyNode2>(new GNodeInfo([], "nodeB2", 2));
        var region = pipeline.CreateGGroup<GRegion>([b1, b2]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement(region, [a], "regionB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [region], "nodeC");

        status += a.AddGAspect<MyTraceAspect>();
        status += a.AddGAspect<MyTemplateAspect<int, double>>(20, 7.0);
        status += region.AddGAspect<MyTimerAspect>();
        status += pipeline.AddGAspect<MyTraceAspect>([region, c]);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialAspect();
    }
}
