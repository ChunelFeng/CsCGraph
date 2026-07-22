using CsCGraph;

public sealed class MyNode2 : GNode
{
    protected override CStatus Init()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INIT] [{GetName()}], enter MyNode2 init function.");
        return new CStatus();
    }

    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], enter MyNode2 run function. Sleep for 2 second ...");
        Thread.Sleep(2000);
        return new CStatus();
    }

    protected override CStatus Destroy()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [DESTROY] [{GetName()}], enter MyNode2 destroy function.");
        return new CStatus();
    }
}
