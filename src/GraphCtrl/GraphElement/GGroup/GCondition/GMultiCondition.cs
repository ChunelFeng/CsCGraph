namespace CsCGraph;

public enum GMultiConditionType
{
    Serial,
    Parallel,
}

public interface IMultiConditionMode
{
    static abstract GMultiConditionType Type { get; }
}

public readonly struct GSerialMode : IMultiConditionMode
{
    public static GMultiConditionType Type => GMultiConditionType.Serial;
}

public readonly struct GParallelMode : IMultiConditionMode
{
    public static GMultiConditionType Type => GMultiConditionType.Parallel;
}

public sealed class GMultiCondition<TMode> : GGroup
    where TMode : struct, IMultiConditionMode
{
    public GMultiCondition()
        : base(GElementType.MultiCondition)
    {
    }

    protected internal override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        return TMode.Type switch
        {
            GMultiConditionType.Serial => await RunSerialAsync(cancellationToken)
                .ConfigureAwait(false),
            GMultiConditionType.Parallel => await RunParallelAsync(cancellationToken)
                .ConfigureAwait(false),
            _ => new CStatus("unknown multi condition type"),
        };
    }

    private async ValueTask<CStatus> RunSerialAsync(CancellationToken cancellationToken)
    {
        var status = new CStatus();
        foreach (var child in Children)
        {
            if (child.MatchInternal())
            {
                status += await child.FatRunAsync(cancellationToken).ConfigureAwait(false);
                if (status.IsErr()) break;
            }
        }

        return status;
    }

    private async ValueTask<CStatus> RunParallelAsync(CancellationToken cancellationToken)
    {
        var tasks = Children
            .Where(static child => child.MatchInternal())
            .Select(child => child.FatRunAsync(cancellationToken).AsTask())
            .ToArray();
        if (tasks.Length == 0) return new CStatus();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var status = new CStatus();
        foreach (var result in results) status += result;
        return status;
    }

    internal override bool IsSeparate(GElement first, GElement second)
        => TMode.Type == GMultiConditionType.Serial;
}
