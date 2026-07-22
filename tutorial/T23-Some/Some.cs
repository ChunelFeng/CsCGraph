using CsCGraph;

internal static class Program
{
    private static int TutorialSome()
    {
        var pipeline = new GPipeline();

        var someB = pipeline.CreateGGroup<MySome>([
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB1")),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("nodeB2")),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("nodeB3"))]);

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGGroup(someB, [a], "someB");
        status += pipeline.RegisterGElement<MyNode1>(out var c, [someB], "nodeC");
        status += pipeline.RegisterGElement<MyNode2>(out _, [c], "nodeD");

        status += pipeline.Process();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline run finished, error code is [{status.GetCode()}]");

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialSome();
    }
}
