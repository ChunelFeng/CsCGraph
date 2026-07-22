namespace CsCGraph;

internal sealed class GStageManager
{
    private readonly Dictionary<string, GStage> _stages = new(StringComparer.Ordinal);

    internal CStatus Create<TStage>(string key, int threshold, GStageParam? param = null)
        where TStage : GStage, new()
    {
        if (string.IsNullOrWhiteSpace(key) || threshold <= 0)
            return new CStatus("stage key and threshold are invalid");
        if (_stages.ContainsKey(key))
            return new CStatus($"stage [{key}] duplicate");
        var stage = new TStage();
        stage.Configure(key, threshold, param);
        _stages.Add(key, stage);
        return new CStatus();
    }

    internal T? Get<T>(string key) where T : GStage
        => _stages.TryGetValue(key, out var stage) ? stage as T : null;

    internal ValueTask<CStatus> WaitAsync(string key, CancellationToken cancellationToken)
        => _stages.TryGetValue(key, out var stage)
            ? stage.WaitingAsync(cancellationToken)
            : ValueTask.FromResult(new CStatus($"[{key}] is not a valid stage key"));

    internal void Clear() => _stages.Clear();

    internal IReadOnlyList<KeyValuePair<string, GStage>> RegisteredStages
        => _stages.ToArray();

    internal CStatus Create(Type type, string key, int threshold)
    {
        if (!typeof(GStage).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid stage type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GStage stage)
            return new CStatus($"stage type [{type}] needs a parameterless constructor");
        if (_stages.ContainsKey(key))
            return new CStatus($"stage [{key}] duplicate");
        stage.Configure(key, threshold, null);
        _stages.Add(key, stage);
        return new CStatus();
    }
}
