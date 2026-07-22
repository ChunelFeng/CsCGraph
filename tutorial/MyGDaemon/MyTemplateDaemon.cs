using CsCGraph;

public sealed class MyTemplateDaemon<T>(int index) : GDaemon
{
    protected override void DaemonTask(GPassedParam? param)
        => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTemplateDaemon] index [{index}]");
}
