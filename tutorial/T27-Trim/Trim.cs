using CsCGraph;

internal static class Program
{
    private static int TutorialTrim()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyNode2>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [a], "nodeC");
        status += pipeline.RegisterGElement<MyNode2>(out _, [a, b, c], "nodeD");
        if (status.IsErr()) return status.GetCode();

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] trim size is: {pipeline.Trim()}");

        status += pipeline.Dump();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialTrim();
    }
}
