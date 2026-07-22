using CsCGraph;

public sealed class MyTraceAspect : GAspect
{
    public override CStatus BeginInit() { Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] init begin"); return new(); }
    public override void FinishInit(CStatus status) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] init finished [{status.GetCode()}]");
    public override CStatus BeginRun() { Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] run begin"); return new(); }
    public override void FinishRun(CStatus status) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] run finished [{status.GetCode()}]");
    public override CStatus BeginDestroy() { Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] destroy begin"); return new(); }
    public override void FinishDestroy(CStatus status) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTraceAspect] [{GetName()}] destroy finished [{status.GetCode()}]");
}
