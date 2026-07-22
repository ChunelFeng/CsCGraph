using CsCGraph;

public sealed class MyWriteParamNode : GNode
{
    protected override CStatus Init() => CreateGParam<MyParam>("param1");

    protected override CStatus Run()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        param.Lock();
        int value;
        int count;
        try
        {
            value = ++param.IValue;
            count = ++param.ICount;
        }
        finally
        {
            param.Unlock();
        }

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], iValue value is : [{value}], iCount value is [{count}] ...");
        return new CStatus();
    }
}
