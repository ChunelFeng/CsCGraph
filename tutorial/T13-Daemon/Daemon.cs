using CsCGraph;

internal static class Program
{
    private static int TutorialDaemon()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out _, [a], "writeParamNodeB");
        var connection = new MyConnParam { Ip = "127.0.0.1", Port = 6666 };

        status += pipeline.AddGDaemon<MyMonitorDaemon>(777);
        status += pipeline.AddGDaemon<MyParamDaemon, MyConnParam>(1500, connection);
        status += pipeline.AddGDaemon<MyTemplateDaemon<int>>(1234, 300);

        status += pipeline.Process(6);

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialDaemon();
    }
}
