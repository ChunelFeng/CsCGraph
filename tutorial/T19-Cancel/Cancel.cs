using CsCGraph;

internal static class Program
{
    private static async Task<int> TutorialCancel()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode1>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [b], "nodeC");
        status += pipeline.RegisterGElement<MyNode2>(out _, [c], "nodeD");
        if (status.IsErr()) return status.GetCode();

        status += pipeline.Init();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run first time, BEGIN.");
        status += await pipeline.RunAsync();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run first time, FINISH.");

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run second time, BEGIN.");
        var secondRun = pipeline.RunAsync().AsTask();
        await Task.Delay(1500);
        status += pipeline.Cancel();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline async run second time, CANCEL.");

        var cancelled = await secondRun;
        var isCancelled = cancelled.IsErr()
            && cancelled.GetInfo().Contains("cancel", StringComparison.OrdinalIgnoreCase);
        if (!isCancelled)
            status += new CStatus("second pipeline run was not cancelled");

        status += pipeline.Destroy();

        return status.IsOk() || isCancelled
            ? CStatus.StatusOk
            : status.GetCode();
    }

    public static Task<int> Main()
    {
        return TutorialCancel();
    }
}
