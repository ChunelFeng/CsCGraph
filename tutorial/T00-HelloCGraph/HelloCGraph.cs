using CsCGraph;

internal static class Program
{
    private static int TutorialHelloCGraph()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<HelloCGraphNode>(out _);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialHelloCGraph();
    }
}
