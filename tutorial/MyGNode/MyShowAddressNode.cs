using CsCGraph;
using System.Runtime.CompilerServices;

public sealed class MyShowAddressNode : GNode
{
    protected override CStatus Init() => CreateGParam<MyParam>("param2");

    protected override CStatus Run()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param2");
        param.Lock();
        int count;
        try { count = ++param.ICount; }
        finally { param.Unlock(); }
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}] identity [{RuntimeHelpers.GetHashCode(this)}], count [{count}]");
        return new CStatus();
    }
}
