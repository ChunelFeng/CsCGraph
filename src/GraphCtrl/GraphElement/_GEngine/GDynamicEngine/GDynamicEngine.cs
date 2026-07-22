namespace CsCGraph;

internal sealed class GDynamicEngine : GEngine
{
    private readonly AsyncManualResetEvent _dispatchGate;
    private readonly GConcurrencyGate _concurrencyGate;
    private GElement[] _elements = [];
    private int[][] _successors = [];
    private int[] _initialDependencies = [];

    internal GDynamicEngine(
        AsyncManualResetEvent dispatchGate,
        GConcurrencyGate concurrencyGate)
    {
        _dispatchGate = dispatchGate;
        _concurrencyGate = concurrencyGate;
    }

    internal override CStatus Setup(IReadOnlyList<GElement> elements)
    {
        _elements = new GElement[elements.Count];
        for (var current = 0; current < elements.Count; current++)
        {
            _elements[current] = elements[current];
        }

        var index = new Dictionary<GElement, int>(
            _elements.Length,
            ReferenceEqualityComparer.Instance);
        for (var current = 0; current < _elements.Length; current++)
        {
            index.Add(_elements[current], current);
        }

        var successorLists = new List<int>[_elements.Length];
        _initialDependencies = new int[_elements.Length];
        for (var current = 0; current < _elements.Length; current++)
        {
            successorLists[current] = [];
            _initialDependencies[current] = _elements[current].Dependencies.Count;
        }

        for (var current = 0; current < _elements.Length; current++)
        {
            foreach (var successor in _elements[current].Successors)
            {
                if (index.TryGetValue(successor, out var successorIndex))
                {
                    successorLists[current].Add(successorIndex);
                }
            }
        }

        _successors = new int[successorLists.Length][];
        for (var current = 0; current < successorLists.Length; current++)
        {
            _successors[current] = successorLists[current].ToArray();
        }

        return new CStatus();
    }

    internal override ValueTask<CStatus> RunAsync(CancellationToken cancellationToken)
    {
        return _elements.Length == 0 ? ValueTask.FromResult(new CStatus()) : new ValueTask<CStatus>(RunCoreAsync(cancellationToken));
    }

    private async Task<CStatus> RunCoreAsync(CancellationToken cancellationToken)
    {
        var remainingDependencies = (int[])_initialDependencies.Clone();
        var completion = new TaskCompletionSource<CStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var statusLock = new object();
        var firstStatus = new CStatus();
        var active = 0;

        bool CanSchedule()
        {
            lock (statusLock)
            {
                return firstStatus.IsOk() && !cancellationToken.IsCancellationRequested;
            }
        }

        for (var current = 0; current < _initialDependencies.Length; current++)
        {
            if (_initialDependencies[current] == 0)
            {
                Schedule(current);
            }
        }

        return await completion.Task.ConfigureAwait(false);

        void Schedule(int elementIndex)
        {
            Interlocked.Increment(ref active);
            _ = Task.Run(async () =>
            {
                var entered = false;
                try
                {
                    await _dispatchGate
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await _concurrencyGate.EnterAsync().ConfigureAwait(false);
                    entered = true;
                    cancellationToken.ThrowIfCancellationRequested();
                    var status = await _elements[elementIndex]
                        .FatRunAsync(cancellationToken)
                        .ConfigureAwait(false);
                    PreserveFirstError(status);

                    if (status.IsOk() && CanSchedule())
                    {
                        var successors = _successors[elementIndex];
                        foreach (var successor in successors)
                        {
                            if (Interlocked.Decrement(
                                    ref remainingDependencies[successor]) == 0)
                            {
                                Schedule(successor);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    PreserveFirstError(new CStatus(
                        $"scheduler element [{_elements[elementIndex].GetName()}] cancelled"));
                }
                catch (Exception exception)
                {
                    PreserveFirstError(CException.FromException(
                        exception,
                        $"scheduler element [{_elements[elementIndex].GetName()}]"));
                }
                finally
                {
                    if (entered) _concurrencyGate.Exit();
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        CStatus finalStatus;
                        lock (statusLock)
                        {
                            finalStatus = firstStatus;
                            if (finalStatus.IsOk()
                                && cancellationToken.IsCancellationRequested)
                            {
                                finalStatus = new CStatus(
                                    "pipeline run cancelled");
                            }
                        }

                        completion.TrySetResult(finalStatus);
                    }
                }
            }, CancellationToken.None);
        }

        void PreserveFirstError(CStatus status)
        {
            if (status.IsOk())
            {
                return;
            }

            lock (statusLock)
            {
                firstStatus += status;
            }
        }
    }
}
