namespace CsCGraph;

internal sealed class GAspectManager
{
    private readonly List<GAspect> _aspects = [];

    internal CStatus Add(GAspect aspect, GElement belong, GParamManager? manager)
    {
        aspect.SetBelong(belong);
        if (manager is not null)
        {
            aspect.SetGParamManager(manager);
        }
        _aspects.Add(aspect);
        return new CStatus();
    }

    internal void SetGParamManager(GParamManager manager)
    {
        foreach (var aspect in _aspects)
        {
            aspect.SetGParamManager(manager);
        }
    }

    internal CStatus BeginInit() => Begin(static aspect => aspect.BeginInit(), "beginInit");
    internal CStatus BeginRun() => Begin(static aspect => aspect.BeginRun(), "beginRun");
    internal CStatus BeginDestroy() => Begin(static aspect => aspect.BeginDestroy(), "beginDestroy");

    internal CStatus FinishInit(CStatus status)
        => Finish((aspect, current) => aspect.FinishInit(current), status, "finishInit");

    internal CStatus FinishRun(CStatus status)
        => Finish((aspect, current) => aspect.FinishRun(current), status, "finishRun");

    internal CStatus FinishDestroy(CStatus status)
        => Finish((aspect, current) => aspect.FinishDestroy(current), status, "finishDestroy");

    internal IReadOnlyList<GAspect> RegisteredAspects => _aspects.ToArray();

    internal void EnterCrashed()
    {
        foreach (var aspect in _aspects)
            try { aspect.EnterCrashed(); }
            catch
            {
                // ignored
            }
    }

    internal void EnterTimeout()
    {
        foreach (var aspect in _aspects)
            try { aspect.EnterTimeout(); }
            catch
            {
                // ignored
            }
    }

    private CStatus Begin(Func<GAspect, CStatus> callback, string phase)
    {
        var status = new CStatus();
        foreach (var aspect in _aspects)
        {
            try
            {
                status += callback(aspect);
            }
            catch (Exception exception)
            {
                status += CException.FromException(exception,
                    $"aspect [{aspect.GetType().Name}] {phase}");
            }
            if (status.IsErr()) break;
        }
        return status;
    }

    private CStatus Finish(
        Action<GAspect, CStatus> callback,
        CStatus currentStatus,
        string phase)
    {
        var status = currentStatus;
        for (var index = _aspects.Count - 1; index >= 0; index--)
        {
            try
            {
                callback(_aspects[index], currentStatus);
            }
            catch (Exception exception)
            {
                status += CException.FromException(exception,
                    $"aspect [{_aspects[index].GetType().Name}] {phase}");
            }
        }
        return status;
    }
}
