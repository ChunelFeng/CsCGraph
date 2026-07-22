namespace CsCGraph;

public sealed class GCluster : GGroup
{
    public GCluster()
        : base(GElementType.Cluster)
    {
    }

    protected internal override async ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        var status = new CStatus();
        foreach (var child in Children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            status += await child.FatRunAsync(cancellationToken).ConfigureAwait(false);
            if (status.IsErr())
            {
                break;
            }
        }

        return status;
    }

    internal override bool IsSeparate(GElement first, GElement second) => true;
}
