using CsCGraph;

internal static class Program
{
    private static int TutorialSimple()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode2>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [a], "nodeC");
        status += pipeline.RegisterGElement<MyNode2>(out _, [b, c], "nodeD");

        status += pipeline.Init();
        for (var loop = 1; loop <= 3 && status.IsOk(); loop++)
        {
            status += pipeline.Run();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] tutorial_simple, loop : [{loop}], and run status = [{status.GetCode()}].");
        }

        status += pipeline.Destroy();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialSimple();
    }
}
