namespace src;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class GPipeline
{
    private readonly List<GElement> _elements = new List<GElement>();
    private int _finishedSize = 0;
    private readonly object _executeLock = new object();
    private readonly TaskCompletionSource<bool> _executeTcs = new TaskCompletionSource<bool>();
    private CStatus _status = new CStatus();
    private readonly GParamManager _paramManager = new GParamManager();

    public async Task<CStatus> ProcessAsync(int times = 1)
    {
        Init();
        while (times-- > 0 && _status.IsOk())
        {
            await RunAsync();
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

    private async Task<CStatus> RunAsync()
    {
        Setup();
        await ExecuteAllAsync();
        await ResetAsync();
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

    private async Task ExecuteAllAsync()
    {
        var tasks = _elements.Where(e => e.Dependence.Count == 0).Select(element => Task.Run(() => ExecuteOneAsync(element))).ToList();
        await Task.WhenAll(tasks); 
    }

    private async Task ExecuteOneAsync(GElement element)
    {
        if (!_status.IsOk()) return;

        _status += await element.FatRunAsync();
        foreach (var cur in element.RunBefore.Where(cur => cur.DecrementDepend()))
        {
            _ = Task.Run(() => ExecuteOneAsync(cur));
        }

        lock (_executeLock)
        {
            if (Interlocked.Increment(ref _finishedSize) >= _elements.Count || !_status.IsOk())
            {
                _executeTcs.TrySetResult(true);
            }
        }
    }

    private void Setup()
    {
        _finishedSize = 0;
        if (_executeTcs.Task.IsCompleted)
        {
            _executeTcs.TrySetCanceled();
        }

        foreach (var element in _elements)
        {
            element.ResetDepend();
        }

        _status += _paramManager.Setup();
    }

    private async Task ResetAsync()
    {
        await _executeTcs.Task;
        _paramManager.Reset(_status);
    }
}