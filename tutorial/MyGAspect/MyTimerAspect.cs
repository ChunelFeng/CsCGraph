using CsCGraph;
using System.Diagnostics;

public sealed class MyTimerAspect : GAspect
{
    private long _start;
    public override CStatus BeginRun() { _start = Stopwatch.GetTimestamp(); return new(); }
    public override void FinishRun(CStatus status)
    {
        var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTimerAspect] [{GetName()}] time cost [{elapsed:F2}] ms");
    }
}
