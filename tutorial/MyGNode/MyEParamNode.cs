using CsCGraph;

public sealed class MyEParamNode : GNode
{
    protected override CStatus Run()
    {
        if (GetEParam<MyVersionParam>(MyVersionParam.Key) is { } version)
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}] version [{version.Priority}-{version.Secondary}]");
        if (GetEParam<MyConnParam>(MyConnParam.Key) is { } connection)
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}] ip [{connection.Ip}], port [{connection.Port}]");
        return new CStatus();
    }
}
