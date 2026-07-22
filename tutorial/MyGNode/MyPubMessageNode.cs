using CsCGraph;

public sealed class MyPubMessageNode : GNode
{
    private int _value;
    protected override CStatus Init()
    {
        Thread.Sleep(300);
        return new CStatus();
    }

    protected override async ValueTask<CStatus> ExecuteOnceAsync(CancellationToken token)
    {
        var value = Interlocked.Increment(ref _value);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] publish [{value}]");
        return await GMessageUtils.PubTopicValueAsync("pub-sub", new MyMessageParam { Value = value });
    }
}
