namespace CsCGraph;

public sealed class GRegion : GGroup
{
    private readonly GElementManager _manager = new();

    public GRegion()
        : base(GElementType.Region)
    {
    }

    protected override CStatus AddElementEx(GElement element)
        => _manager.Add(element);

    protected override CStatus Init() => _manager.Init();

    protected override CStatus Destroy() => _manager.Destroy();

    protected internal override ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
        => _manager.RunAsync(cancellationToken);

    public GRegion SetGEngineType(GEngineType type)
    {
        var status = _manager.SetEngineType(type);
        return status.IsErr() ? throw new CException(status) : this;
    }

    public int Trim() => _manager.Trim();

    internal override bool IsSeparate(GElement first, GElement second)
        => _manager.CheckSeparate(first, second);
}
