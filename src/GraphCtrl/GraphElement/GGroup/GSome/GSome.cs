namespace CsCGraph;

public abstract class GSome : GGroup
{
    protected GSome()
        : base(GElementType.Some)
    {
    }

    protected abstract int GetThreshold();

    protected internal override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        var threshold = GetThreshold();
        if (threshold <= 0 || threshold > Children.Count)
        {
            return new CStatus("Some threshold must be in the range [1, children count]");
        }

        var pending = Children.Select(child => Task.Run(
            async () => await child.FatRunAsync(cancellationToken).ConfigureAwait(false),
            CancellationToken.None)).ToList();
        foreach (var task in pending) TrackEpochTask(task);

        var completedCount = 0;
        var status = new CStatus();
        while (completedCount < threshold && status.IsOk())
        {
            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(completed);
            status += await completed.ConfigureAwait(false);
            completedCount++;
        }

        foreach (var child in Children)
        {
            child.SetState(GElementState.Timeout);
        }

        return status;
    }
}
