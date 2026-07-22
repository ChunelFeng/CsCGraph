using CsCGraph;

internal static class Program
{
    private static int TutorialFence()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<GFunction>(out var functionA, [], "functionA");
        status += pipeline.RegisterGElement<MyNode2>(out var b, [functionA], "nodeB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [functionA], "nodeC");
        status += pipeline.RegisterGElement<GFence>(out var fenceD, [b, c], "fenceD");
        status += pipeline.RegisterGElement<MyNode1>(out _, [fenceD], "nodeE");

        functionA.SetFunction(CFunctionType.Run, () =>
        {
            var seconds = 5;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [functionA] begin sleep for [{seconds}]s");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [functionA] run finished.");
            return new CStatus();
        });
        functionA.SetTimeout(200, GElementTimeoutStrategy.HoldByPipeline);
        b.SetTimeout(200, GElementTimeoutStrategy.HoldByPipeline);
        fenceD.WaitGElements([functionA, b]);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialFence();
    }
}
