namespace CsCGraph;

public sealed class GFunction : GAdapter
{
    private Func<CStatus>? _initFunction;
    private Func<CStatus>? _runFunction;
    private Func<CStatus>? _destroyFunction;

    public GFunction()
        : base(GElementType.Function)
    {
    }

    public GFunction SetFunction(CFunctionType type, Func<CStatus> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        switch (type)
        {
            case CFunctionType.Init: _initFunction = function; break;
            case CFunctionType.Run: _runFunction = function; break;
            case CFunctionType.Destroy: _destroyFunction = function; break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        return this;
    }

    public void SetFunction(Func<CStatus> function)
        => SetFunction(CFunctionType.Run, function);

    public new T GetGParamWithNoEmpty<T>(string key) where T : GParam
        => base.GetGParamWithNoEmpty<T>(key);

    protected override CStatus Init() => _initFunction?.Invoke() ?? new CStatus();
    protected override CStatus Run() => _runFunction?.Invoke() ?? new CStatus();
    protected override CStatus Destroy() => _destroyFunction?.Invoke() ?? new CStatus();
}
