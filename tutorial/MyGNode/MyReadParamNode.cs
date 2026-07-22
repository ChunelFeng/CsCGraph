using CsCGraph;

public sealed class MyReadParamNode : GNode
{
    protected override CStatus Run()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        param.Lock();
        int value;
        try
        {
            value = param.IValue;
        }
        finally
        {
            param.Unlock();
        }

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], iValue is : [{value}] ...");
        return new CStatus();
    }
}
