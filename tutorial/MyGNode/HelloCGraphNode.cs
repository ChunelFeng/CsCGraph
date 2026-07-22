using CsCGraph;

public sealed class HelloCGraphNode : GNode
{
    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Hello, CGraph.");
        return new CStatus();
    }
}
