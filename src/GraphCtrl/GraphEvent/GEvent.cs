namespace CsCGraph;

public abstract class GEvent
{
    private GParamManager? _paramManager;
    private GPassedParam? _eventParam;

    protected abstract void Trigger(GPassedParam? param);
    protected T? GetGParam<T>(string key) where T : GParam => _paramManager?.Get<T>(key);
    protected T? GetEParam<T>() where T : GPassedParam => _eventParam as T;

    internal void Configure(GParamManager manager, GPassedParam? param)
    {
        _paramManager = manager;
        _eventParam = param;
    }

    internal CStatus Fire()
    {
        try { Trigger(_eventParam); return new CStatus(); }
        catch (Exception exception)
        {
            return CException.FromException(exception, $"event [{GetType().Name}] trigger");
        }
    }
}
