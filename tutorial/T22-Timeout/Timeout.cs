using CsCGraph;

internal static class Program
{
    private static int TutorialTimeout()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode1>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyNode2>(out var c, [b], "nodeC");
        status += pipeline.RegisterGElement<MyNode1>(out _, [c], "nodeD");
        if (status.IsErr()) return status.GetCode();

        status = pipeline.Process();
        if (status.IsErr()) return status.GetCode();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ---- pipeline run finish");

        c.SetTimeout(10, GElementTimeoutStrategy.AsError);
        var timeoutStatus = pipeline.Process();
        if (timeoutStatus.IsOk())
            return new CStatus("expected timeout status").GetCode();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] timeout error: {timeoutStatus.GetInfo()}");

        c.SetTimeout(0);
        status = pipeline.Process();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] return to no timeout, error code is: {status.GetCode()}");

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialTimeout();
    }
}
