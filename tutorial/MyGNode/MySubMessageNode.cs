using CsCGraph;

public sealed class MySubMessageNode<T>(int connectionId) : GTemplateNode<T>
{
    protected override async ValueTask<CStatus> ExecuteOnceAsync(CancellationToken token)
    {
        var result = await GMessageUtils.SubTopicValueAsync<MyMessageParam>(connectionId, TimeSpan.FromSeconds(2));
        if (result.Status.IsOk()) Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] subscriber [{connectionId}] recv [{result.Value!.Value}]");
        return result.Status;
    }
}
