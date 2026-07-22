using CsCGraph;

internal static class Program
{
    private static int TutorialCondition()
    {
        var pipeline = new GPipeline();

        var conditionB = pipeline.CreateGGroup<MyCondition>(
        [
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("conditionNodeB0", 1)),
            pipeline.CreateGNode<MyNode2>(new GNodeInfo("conditionNodeB1", 1)),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("conditionNodeB2", 1)),
        ]);
        var conditionD = pipeline.CreateGGroup<MyParamCondition>(
        [
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("paramConditionNodeD0", 1)),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("paramConditionNodeD1", 1)),
            pipeline.CreateGNode<MyNode1>(new GNodeInfo("paramConditionNodeD2", 1)),
        ]);

        var status = pipeline.RegisterGElement<MyWriteParamNode>(out var a, [], "writeNodeA");
        status += pipeline.RegisterGElement(conditionB, [a], "conditionB");
        status += pipeline.RegisterGElement<MyReadParamNode>(out var c, [conditionB], "readNodeC");
        status += pipeline.RegisterGElement(conditionD, [c], "conditionD");

        status += pipeline.Init();
        for (var loop = 1; loop <= 3 && status.IsOk(); loop++)
        {
            status += pipeline.Run();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] tutorial_condition, loop : {loop}, status = {status.GetCode()}");
        }

        status += pipeline.Destroy();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialCondition();
    }
}
