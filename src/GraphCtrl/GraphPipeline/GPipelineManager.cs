namespace CsCGraph;

public sealed class GPipelineManager : GraphManager<GPipeline>
{
    private readonly Lock _mutex = new();
    private readonly Queue<GPipeline> _freeList = new();
    private readonly HashSet<GPipeline> _usedList =
        new(ReferenceEqualityComparer.Instance);

    private bool _initialized;

    public CStatus Init()
    {
        lock (_mutex)
        {
            if (_initialized || _usedList.Count != 0)
                return new CStatus("pipeline manager init state error");
        }
        var status = new CStatus();
        status = SnapshotFree().Aggregate(status, (current, pipeline) => current + pipeline.Init());
        if (status.IsOk()) _initialized = true;
        return status;
    }

    public CStatus Run()
    {
        if (!_initialized) return new CStatus("pipeline manager is not initialized");
        var pipeline = Fetch();
        if (pipeline is null) return new CStatus("no free pipeline");
        var status = pipeline.Run();
        status += Release(pipeline);
        return status;
    }

    public CStatus Destroy()
    {
        lock (_mutex)
            if (!_initialized || _usedList.Count != 0)
                return new CStatus("pipeline manager destroy state error");
        var status = new CStatus();
        foreach (var pipeline in SnapshotFree()) status += pipeline.Destroy();
        if (status.IsOk()) _initialized = false;
        return status;
    }

    public override CStatus Add(GPipeline pipeline)
    {
        lock (_mutex)
        {
            if (_initialized)
                return new CStatus("cannot add pipeline while manager is initialized");
            if (_usedList.Contains(pipeline) || _freeList.Contains(pipeline))
            {
                return new CStatus("pipeline registered twice");
            }

            _freeList.Enqueue(pipeline);
            return new CStatus();
        }
    }

    public GPipeline? Fetch()
    {
        lock (_mutex)
        {
            if (!_freeList.TryDequeue(out var pipeline))
            {
                return null;
            }

            _usedList.Add(pipeline);
            return pipeline;
        }
    }

    public CStatus Release(GPipeline pipeline)
    {
        lock (_mutex)
        {
            if (!_usedList.Remove(pipeline))
            {
                return new CStatus("pipeline is not in use");
            }

            _freeList.Enqueue(pipeline);
            return new CStatus();
        }
    }

    public override CStatus Remove(GPipeline pipeline)
    {
        lock (_mutex)
        {
            if (_initialized) return new CStatus("cannot remove pipeline while manager is initialized");
            if (_usedList.Contains(pipeline)) return new CStatus("cannot remove a used pipeline");
            if (!_freeList.Contains(pipeline)) return new CStatus("pipeline not found");
            var retained = _freeList.Where(item => !ReferenceEquals(item, pipeline)).ToArray();
            _freeList.Clear();
            foreach (var item in retained) _freeList.Enqueue(item);
            return new CStatus();
        }
    }

    public override bool Find(GPipeline pipeline)
    {
        lock (_mutex) return _usedList.Contains(pipeline) || _freeList.Contains(pipeline);
    }

    public override CStatus Clear()
    {
        lock (_mutex)
        {
            if (_initialized) return new CStatus("cannot clear initialized pipeline manager");
            _freeList.Clear();
            _usedList.Clear();
        }
        return new CStatus();
    }

    public override int GetSize()
    {
        lock (_mutex)
        {
            return _freeList.Count + _usedList.Count;
        }
    }

    private GPipeline[] SnapshotFree()
    {
        lock (_mutex) return _freeList.ToArray();
    }
}
