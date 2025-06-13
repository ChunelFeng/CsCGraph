namespace src;

using System.Collections.Generic;
using System.Threading;

public abstract class GElement
{
    internal HashSet<GElement> RunBefore { get; } = new ();
    internal HashSet<GElement> Dependence { get; } = new ();
    private GParamManager? _paramManager = null;
    private string _name = string.Empty;
    private int _loop = 1;
    private int _leftDependCounter = 0;

    protected internal virtual CStatus Init()
    {
        return new CStatus();
    }

    protected abstract CStatus Run();

    internal CStatus FatRun()
    {
        var status = new CStatus();
        for (var i = 0; i < _loop && !status.IsErr(); i++)
        {
            status += Run();
        }

        return status;
    }

    protected internal virtual CStatus Destroy()
    {
        return new CStatus();
    }

    protected string GetName()
    {
        return _name;
    }

    protected CStatus CreateGParam<T>(string key) where T : GParam, new()
    {
        return _paramManager == null 
            ? new CStatus("param manager is null")
            : _paramManager.Create<T>(key);
    }

    protected T? GetGParam<T>(string key) where T : GParam
    {
        return _paramManager?.Get<T>(key);
    }

    protected T GetGParamWithNoEmpty<T>(string key) where T : GParam
    {
        var param = GetGParam<T>(key);
        if (null == param)
        {
            throw new KeyNotFoundException($"The parameter with key '{key}' is not found.");
        }
        return param;
    }

    internal void AddElementInfo(IEnumerable<GElement> depends, string name, int loop)
    {
        foreach (var depend in depends)
        {
            Dependence.Add(depend);
            depend.RunBefore.Add(this);
        }

        ResetDepend();
        _name = name;
        _loop = loop;
    }

    internal void SetManager(GParamManager pm)
    {
        _paramManager = pm;
    }

    internal bool DecrementDepend()
    {
        return Interlocked.Decrement(ref _leftDependCounter) <= 0;
    }

    internal void ResetDepend()
    {
        Interlocked.Exchange(ref _leftDependCounter, Dependence.Count);
    }
}

