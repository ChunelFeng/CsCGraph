using CsCGraph;

public sealed class MyPipelineParamAspect : GAspect
{
    public override CStatus BeginRun()
    {
        var param = GetGParam<MyParam>("param1");
        if (param is null)
        {
            return new CStatus("pipeline param1 is missing");
        }
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyPipelineParamAspect] iCount [{param.ICount}] before run");
        return param.ICount < 0 ? new CStatus("aspect demo error") : new CStatus();
    }
}
