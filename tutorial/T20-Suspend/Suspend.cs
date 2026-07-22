using CsCGraph;

internal static class Program
{
    private static async Task<int> TutorialSuspend()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode2>(out var b, [a], "nodeB", 3);
        status += pipeline.RegisterGElement<MyNode1>(out var c, [a], "nodeC", 3);
        status += pipeline.RegisterGElement<MyNode2>(out _, [b, c], "nodeD");
        if (status.IsErr()) return status.GetCode();

        status += pipeline.Init();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run, BEGIN.");
        var result = pipeline.RunAsync().AsTask();
        await Task.Delay(2600);

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run, SUSPEND.");
        status += pipeline.Suspend();
        await Task.Delay(7200);

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run, RESUME after 7200ms.");
        status += pipeline.Resume();
        status += await result;

        status += pipeline.Destroy();

        return status.GetCode();
    }

    public static Task<int> Main()
    {
        return TutorialSuspend();
    }
}
