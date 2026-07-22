using CsCGraph;

internal static class Program
{
    private static int TutorialMultiCondition()
    {
        var pipeline = new GPipeline();

        var conditionB = pipeline.CreateGGroup<GMultiCondition<GSerialMode>>([
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("nodeB1")),
            pipeline.CreateGNode<MyMatchNode>(new GNodeInfo("nodeB2"))]);
        var conditionD = pipeline.CreateGGroup<GMultiCondition<GParallelMode>>([
            pipeline.CreateGNode<MyMatchNode>(new GNodeInfo("nodeD1")),
            pipeline.CreateGNode<MyMatchNode>(new GNodeInfo("nodeD2"))]);

        var status = pipeline.RegisterGElement<MyWriteParamNode>(out var a, [], "nodeA");
        status += pipeline.RegisterGGroup(conditionB, [a], "multiConditionB");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out var c, [conditionB], "nodeC");
        status += pipeline.RegisterGGroup(conditionD, [c], "multiConditionD");

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialMultiCondition();
    }
}
