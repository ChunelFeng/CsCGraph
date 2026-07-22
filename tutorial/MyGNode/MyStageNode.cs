using CsCGraph;

public interface IStageTiming
{
    static abstract int BeforeSeconds { get; }
    static abstract int AfterSeconds { get; }
}

public sealed class MyStageNode<TTiming> : GNode
    where TTiming : struct, IStageTiming
{
    protected override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(TTiming.BeforeSeconds), cancellationToken);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}] wait for stage");
        var status = await EnterStageAsync("stage", cancellationToken);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}] finish stage");
        await Task.Delay(TimeSpan.FromSeconds(TTiming.AfterSeconds), cancellationToken);
        return status;
    }
}
