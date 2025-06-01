namespace src;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class GPipeline
{
    private readonly List<GElement> _elements = new List<GElement>();
    private int _finishedSize = 0;
    private readonly object _executeLock = new object();
    private readonly ManualResetEventSlim _executeEvent = new ManualResetEventSlim(false);
    private CStatus _status = new CStatus();
    private readonly GParamManager _paramManager = new GParamManager();

    public CStatus Process(int times = 1)
    {
        Init();
        while (times-- > 0 && _status.IsOk())
        {
            Run();
        }
        Destroy();
        return _status;
    }

    public CStatus RegisterGElement<T>(out GElement element, 
                                       IEnumerable<GElement> depends,
                                       string name,
                                       int loop = 1) where T : GElement, new()
    {
        element = new T();
        element.AddElementInfo(depends, name, loop);
        element.SetManager(_paramManager);
        _elements.Add(element);
        return new CStatus();
    }

    private CStatus Init()
    {
        _status = new CStatus();
        foreach (var element in _elements)
        {
            _status += element.Init();
        }
        return _status;
    }

    private CStatus Run()
    {
        Setup();
        ExecuteAll();
        Reset();
        return _status;
    }

    private CStatus Destroy()
    {
        foreach (var element in _elements)
        {
            _status += element.Destroy();
        }
        return _status;
    }

    private void ExecuteAll()
    {
        foreach (var element in _elements.Where(element => element.Dependence.Count == 0))
        { 
            Task.Run(() =>
                {
                    ExecuteOne(element);
                }
            );
        }
    }

    private void ExecuteOne(GElement element)
    {
        if (!_status.IsOk())
        {
            return;
        }

        _status += element.FatRun();
        foreach (var cur in element.RunBefore.Where(cur => cur.DecrementDepend()))
        {
            Task.Run(() =>
                {
                    ExecuteOne(cur);
                }
            );
        }

        lock (_executeLock)
        {
            if (++_finishedSize >= _elements.Count || !_status.IsOk())
            {
                _executeEvent.Set();
            }
        }
    }

    private void Setup()
    {
        _finishedSize = 0;
        _executeEvent.Reset();
        
        foreach (var element in _elements)
        {
            element.ResetDepend();
        }

        _status += _paramManager.Setup();
    }

    private void Reset()
    {
        _executeEvent.Wait();
        _paramManager.Reset(_status);
    }
}
