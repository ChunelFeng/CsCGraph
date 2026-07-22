using CsCGraph;

internal static class Program
{
    private static int TutorialFunction()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        status += pipeline.RegisterGElement<MyWriteParamNode>(out var b, [a], "nodeB");
        status += pipeline.RegisterGElement<GFunction>(out var functionC, [b], "functionC");
        status += pipeline.RegisterGElement<GFunction>(out var functionD, [functionC], "functionD", 3);
        var number = 10;
        var info = "Hello, CGraph";
        functionC.SetFunction(CFunctionType.Run, () =>
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] input num [{number}], info [{info}]");
            return new CStatus();
        });
        functionD
            .SetFunction(CFunctionType.Init, () => { Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] functionD init"); return new(); })
            .SetFunction(CFunctionType.Run, () =>
            {
                var param = functionD.GetGParamWithNoEmpty<MyParam>("param1");
                param.ICount += number;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] functionD run, iCount [{param.ICount}], iValue [{++param.IValue}]");
                return new CStatus();
            });

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialFunction();
    }
}
