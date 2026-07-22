using CsCGraph;

public sealed class MyParamDaemon : GDaemon
{
    protected override void DaemonTask(GPassedParam? param)
    {
        var pipelineParam = GetGParam<MyParam>("param1");
        var count = pipelineParam?.ICount ?? -1;
        var connection = param as MyConnParam;
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyParamDaemon] iCount [{count}], address [{connection?.Ip}:{connection?.Port}]");
    }
}
