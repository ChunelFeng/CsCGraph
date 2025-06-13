namespace src;

public class GExecutor
{
    private readonly List<GElement> _elements = new ();
    private CStatus _status = new ();
    private readonly object _executeLock = new ();
    private readonly ManualResetEventSlim _executeEvent = new (false);
    private int _finishedSize = 0;

    internal CStatus Init()
    {
        _status = new CStatus();
        foreach (var element in _elements)
        {
            _status += element.Init();
        }
        return _status;
    }

    internal CStatus Run()
    {
        Setup();
        ExecuteAll();
        Reset();
        return _status;
    }

    internal CStatus Destroy()
    {
        foreach (var element in _elements)
        {
            _status += element.Destroy();
        }
        return _status;
    }

    internal CStatus AddElement(GElement element)
    {
        _elements.Add(element);
        return new CStatus();
    }

    private void Setup()
    {
        _finishedSize = 0;
        _executeEvent.Reset();
        
        foreach (var element in _elements)
        {
            element.ResetDepend();
        }
    }

    private void Reset()
    {
        _executeEvent.Wait();
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
        if (_status.IsErr())
        {
            return;
        }

        _status += element.FatRun();
        foreach (var cur in element.RunBefore.Where(cur => cur.DecrementDepend()))
        {
            Task.Run(() => {
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
}