using CsCGraph;

internal static class Program
{
    private static int TutorialStage()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyStageNode<FastThenSlow>>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<MyStageNode<SlowThenFast>>(out var c, [a], "nodeC");
        status += pipeline.RegisterGElement<MyStageNode<SlowThenFast>>(out var d, [a], "nodeD");
        status += pipeline.RegisterGElement<MyNode1>(out _, [b, c, d], "nodeE");
        status += pipeline.AddGStage<GStage>("stage", 3);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialStage();
    }
}
