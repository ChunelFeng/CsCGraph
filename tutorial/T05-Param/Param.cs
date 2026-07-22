using CsCGraph;

internal static class Program
{
    private static int TutorialParam()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyReadParamNode>(out var a, [], "readNodeA");
        status += pipeline.RegisterGElement<MyReadParamNode>(out var b, [a], "readNodeB");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out var c, [a], "writeNodeC");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out var d, [a], "writeNodeD", 2);
        status += pipeline.RegisterGElement<MyReadParamNode>(out var e, [a], "readNodeE");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out _, [b, c, d, e], "writeNodeF");

        status += pipeline.Init();
        for (var loop = 1; loop <= 3 && status.IsOk(); loop++)
        {
            status += pipeline.Run();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] tutorial_param, loop : {loop}, and run status = {status.GetCode()}");
        }

        status += pipeline.Destroy();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialParam();
    }
}
