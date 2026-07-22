using CsCGraph;

public sealed class MyRecvMessageNode : GNode
{
    protected override async ValueTask<CStatus> ExecuteOnceAsync(CancellationToken token)
    {
        var result = await GMessageUtils.RecvTopicValueAsync<MyMessageParam>("send-recv", TimeSpan.FromSeconds(2));
        if (result.Status.IsOk()) Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] recv [{result.Value!.Value}]");
        return result.Status;
    }
}
