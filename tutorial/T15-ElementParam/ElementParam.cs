using CsCGraph;

internal static class Program
{
    private static int TutorialElementParam()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "node-1");
        status += pipeline.RegisterGElement<MyEParamNode>(out var version1, [a], "version-1");
        status += pipeline.RegisterGElement<MyEParamNode>(out var version2, [version1], "version-2");

        status += version1.AddEParam(MyVersionParam.Key,
            new MyVersionParam { Priority = 1, Secondary = 1 });
        status += version2.AddEParam(MyVersionParam.Key,
            new MyVersionParam { Priority = 2, Secondary = 2 });
        status += version2.AddEParam(MyConnParam.Key,
            new MyConnParam { Ip = "127.0.0.1", Port = 8080 });

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialElementParam();
    }
}
