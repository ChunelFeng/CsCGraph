namespace CsCGraph;

public abstract class GAspect
{
    private GElement? _belong;
    private GPassedParam? _aspectParam;
    private GParamManager? _paramManager;

    public virtual CStatus BeginInit() => new();
    public virtual void FinishInit(CStatus curStatus) { }
    public virtual CStatus BeginRun() => new();
    public virtual void FinishRun(CStatus curStatus) { }
    public virtual CStatus BeginDestroy() => new();
    public virtual void FinishDestroy(CStatus curStatus) { }
    public virtual void EnterCrashed() { }
    public virtual void EnterTimeout() { }

    public string GetName() => _belong?.GetName() ?? string.Empty;

    protected T? GetAParam<T>() where T : GPassedParam
        => _aspectParam as T;

    protected T? GetGParam<T>(string key) where T : GParam
        => _paramManager?.Get<T>(key);

    internal void SetBelong(GElement element) => _belong = element;
    internal void SetGParamManager(GParamManager manager) => _paramManager = manager;
    internal void SetAParam(GPassedParam param) => _aspectParam = param;
}
