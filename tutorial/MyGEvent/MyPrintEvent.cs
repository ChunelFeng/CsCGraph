using CsCGraph;

public sealed class MyPrintEvent : GEvent
{
    protected override void Trigger(GPassedParam? param)
    {
        var mp = GetGParam<MyParam>("param1");
        mp?.ICount += 1;
        Thread.Sleep(100);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] event triggered, param iCount [{mp?.ICount ?? -1}]");
    }
}
