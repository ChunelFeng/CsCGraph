using CsCGraph;

public sealed class MyMonitorDaemon : GDaemon
{
    protected override void DaemonTask(GPassedParam? param)
        => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyMonitorDaemon] running every [{GetInterval()}] ms");
}
