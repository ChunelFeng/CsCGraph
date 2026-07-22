using CsCGraph;

public sealed class MySendMessageNode : GNode
{
    private int _value;
    protected override async ValueTask<CStatus> ExecuteOnceAsync(CancellationToken token)
    {
        var value = Interlocked.Increment(ref _value);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] send [{value}]");
        return await GMessageUtils.SendTopicValueAsync("send-recv", new MyMessageParam { Value = value });
    }
}
