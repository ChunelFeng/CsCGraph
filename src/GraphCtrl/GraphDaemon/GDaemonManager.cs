namespace CsCGraph;

internal sealed class GDaemonManager
{
    private readonly List<GDaemon> _daemons = [];

    internal CStatus Add(GDaemon daemon)
    {
        if (_daemons.Contains(daemon, ReferenceEqualityComparer.Instance))
            return new CStatus("daemon registered twice");
        _daemons.Add(daemon);
        return new CStatus();
    }

    internal CStatus Start()
    {
        var status = new CStatus();
        foreach (var daemon in _daemons) status += daemon.Start();
        return status;
    }

    internal CStatus Stop()
    {
        var status = new CStatus();
        for (var index = _daemons.Count - 1; index >= 0; index--)
            status += _daemons[index].StopAsync().AsTask().GetAwaiter().GetResult();
        return status;
    }

    internal CStatus Clear()
    {
        var status = Stop();
        _daemons.Clear();
        return status;
    }

    internal IReadOnlyList<GDaemon> RegisteredDaemons => _daemons.ToArray();
}
