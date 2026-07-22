using CsCGraph;

public sealed class MyMatchNode : GNode
{
    protected override CStatus Run()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], iValue is [{param.IValue}], Sleep for [{param.IValue}] second ...");
        Thread.Sleep(TimeSpan.FromSeconds(param.IValue));
        return new CStatus();
    }

    protected override bool IsMatch()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        return param.ICount % 2 != 0;
    }
}
