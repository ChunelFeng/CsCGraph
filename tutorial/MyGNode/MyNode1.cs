using CsCGraph;

public sealed class MyNode1 : GNode
{
    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], enter MyNode1 run function. Sleep for 1 second ...");
        Thread.Sleep(1000);
        return new CStatus();
    }
}
