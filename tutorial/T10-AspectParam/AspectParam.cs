using CsCGraph;

internal static class Program
{
    private static int TutorialAspectParam()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode2>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out var c, [b], "nodeC", 2);
        var paramA = new MyConnParam { Ip = "127.0.0.1", Port = 6666 };
        var paramB = new MyConnParam { Ip = "255.255.255.255", Port = 9999 };

        status += a.AddGAspect<MyConnAspect, MyConnParam>(paramA);
        status += b.AddGAspect<MyConnAspect, MyConnParam>(paramB);
        status += b.AddGAspect<MyTimerAspect>();
        status += c.AddGAspect<MyPipelineParamAspect>();

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialAspectParam();
    }
}
