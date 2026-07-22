using System.Text.Json;

namespace CsCGraph;

internal static class GStorage
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    internal static CStatus Save(GPipeline pipeline, string path)
    {
        try
        {
            var storage = Build(pipeline);
            File.WriteAllText(path, JsonSerializer.Serialize(storage, s_options));
            return new CStatus();
        }
        catch (Exception exception)
        {
            return CException.FromException(exception, $"save pipeline [{path}]");
        }
    }

    internal static CStatus Load(GPipeline pipeline, string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var storage = JsonSerializer.Deserialize<PipelineStorage>(json, s_options);
            if (storage is null || storage.Version != CurrentVersion)
                return new CStatus("unsupported pipeline storage version");
            return Recover(pipeline, storage);
        }
        catch (Exception exception)
        {
            return CException.FromException(exception, $"load pipeline [{path}]");
        }
    }

    private static PipelineStorage Build(GPipeline pipeline)
    {
        var duplicateName = pipeline.ElementManager.RegisteredElements
            .GroupBy(static element => element.GetName(), StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateName is not null)
            throw new InvalidOperationException(
                $"pipeline storage does not support duplicate element name [{duplicateName.Key}]");

        var ids = new Dictionary<GElement, int>(ReferenceEqualityComparer.Instance);
        var topLevel = new HashSet<GElement>(
            pipeline.ElementManager.RegisteredElements,
            ReferenceEqualityComparer.Instance);

        void Visit(GElement element)
        {
            if (ids.ContainsKey(element)) return;
            ids.Add(element, ids.Count + 1);
            foreach (var dependency in element.Dependencies) Visit(dependency);
            if (element is GGroup group)
                foreach (var child in group.Children) Visit(child);
        }

        foreach (var element in pipeline.ElementManager.RegisteredElements) Visit(element);

        var records = ids.OrderBy(static pair => pair.Value).Select(pair =>
        {
            var element = pair.Key;
            var typeName = element.GetType().AssemblyQualifiedName
                ?? throw new InvalidOperationException($"type name unavailable for [{element.GetType()}]");
            return new ElementStorage
            {
                Id = pair.Value,
                TypeName = typeName,
                ElementType = element.ElementType,
                Name = element.GetName(),
                Loop = element.GetLoop(),
                TimeoutMilliseconds = element.GetTimeout(),
                TimeoutStrategy = element.GetTimeoutStrategy(),
                IsTopLevel = topLevel.Contains(element),
                DependencyIds = element.Dependencies.Select(dependency => ids[dependency]).ToList(),
                ChildIds = element is GGroup group
                    ? group.Children.Select(child => ids[child]).ToList()
                    : [],
                AspectTypeNames = element.RegisteredAspects
                    .Select(static aspect => GetTypeName(aspect.GetType())).ToList(),
                ElementParams = element.RegisteredEParams.Select(param => new TypedKeyStorage
                {
                    Key = param.Key,
                    TypeName = GetTypeName(param.Value.GetType()),
                }).ToList(),
            };
        }).ToList();

        return new PipelineStorage
        {
            Version = CurrentVersion,
            Elements = records,
            Events = pipeline.EventManager.RegisteredEvents.Select(item => new TypedKeyStorage
            {
                Key = item.Key,
                TypeName = GetTypeName(item.Value.GetType()),
            }).ToList(),
            Params = pipeline.ParamManager.RegisteredParams.Select(param => new ParamStorage
            {
                Key = param.GetKey(),
                TypeName = GetTypeName(param.GetType()),
                Backtrace = param.BacktraceEnabled,
            }).ToList(),
            Daemons = pipeline.DaemonManager.RegisteredDaemons.Select(daemon => new DaemonStorage
            {
                IntervalMilliseconds = daemon.Interval,
                TypeName = GetTypeName(daemon.GetType()),
            }).ToList(),
            Stages = pipeline.StageManager.RegisteredStages.Select(item => new StageStorage
            {
                Key = item.Key,
                Threshold = item.Value.TargetCount,
                TypeName = GetTypeName(item.Value.GetType()),
            }).ToList(),
            DefaultThreadSize = pipeline.ThreadPoolConfig.DefaultThreadSize,
            SecondaryThreadSize = pipeline.ThreadPoolConfig.SecondaryThreadSize,
        };
    }

    private static CStatus Recover(GPipeline pipeline, PipelineStorage storage)
    {
        if (storage.Elements.Select(static item => item.Id).Distinct().Count()
            != storage.Elements.Count)
            return new CStatus("duplicate element id in storage");

        var elements = new Dictionary<int, GElement>();
        foreach (var record in storage.Elements)
        {
            var type = Type.GetType(record.TypeName, throwOnError: false);
            if (type is null || type.IsAbstract || !typeof(GElement).IsAssignableFrom(type))
                return new CStatus($"element type [{record.TypeName}] cannot be resolved");
            if (Activator.CreateInstance(type, nonPublic: true) is not GElement element)
                return new CStatus($"element type [{record.TypeName}] needs a parameterless constructor");
            if (element.ElementType != record.ElementType)
                return new CStatus($"element [{record.Name}] type check failed");
            elements.Add(record.Id, element);
        }

        foreach (var record in storage.Elements)
        {
            if (!TryResolve(record.DependencyIds, elements, out var dependencies))
                return new CStatus($"element [{record.Name}] has an invalid dependency id");
            var status = elements[record.Id].Configure(dependencies, record.Name, record.Loop);
            if (status.IsErr()) return status;
            elements[record.Id].SetTimeout(record.TimeoutMilliseconds, record.TimeoutStrategy);
            foreach (var aspectTypeName in record.AspectTypeNames)
            {
                if (!TryResolveType(aspectTypeName, typeof(GAspect), out var aspectType))
                    return InvalidType("aspect", aspectTypeName);
                status += elements[record.Id].AddGAspect(aspectType);
            }
            foreach (var param in record.ElementParams)
            {
                if (!TryResolveType(param.TypeName, typeof(GPassedParam), out var paramType))
                    return InvalidType("element param", param.TypeName);
                status += elements[record.Id].AddEParam(param.Key, paramType);
            }
            if (status.IsErr()) return status;
        }

        foreach (var record in storage.Elements)
        {
            if (record.ChildIds.Count == 0) continue;
            if (elements[record.Id] is not GGroup group)
                return new CStatus($"non-group element [{record.Name}] contains children");
            foreach (var childId in record.ChildIds)
            {
                if (!elements.TryGetValue(childId, out var child))
                    return new CStatus($"group [{record.Name}] has an invalid child id");
                var status = group.AddElement(child);
                if (status.IsErr()) return status;
            }
        }

        foreach (var record in storage.Elements.Where(static item => item.IsTopLevel))
        {
            var status = pipeline.RegisterLoadedElement(elements[record.Id]);
            if (status.IsErr())
            {
                pipeline.ElementManager.Clear();
                return status;
            }
        }


        var globalStatus = new CStatus();
        foreach (var param in storage.Params)
        {
            if (!TryResolveType(param.TypeName, typeof(GParam), out var type))
                return InvalidType("param", param.TypeName);
            globalStatus += pipeline.RegisterLoadedParam(type, param.Key, param.Backtrace);
        }
        foreach (var item in storage.Events)
        {
            if (!TryResolveType(item.TypeName, typeof(GEvent), out var type))
                return InvalidType("event", item.TypeName);
            globalStatus += pipeline.RegisterLoadedEvent(type, item.Key);
        }
        foreach (var item in storage.Daemons)
        {
            if (!TryResolveType(item.TypeName, typeof(GDaemon), out var type))
                return InvalidType("daemon", item.TypeName);
            globalStatus += pipeline.RegisterLoadedDaemon(type, item.IntervalMilliseconds);
        }
        foreach (var item in storage.Stages)
        {
            if (!TryResolveType(item.TypeName, typeof(GStage), out var type))
                return InvalidType("stage", item.TypeName);
            globalStatus += pipeline.RegisterLoadedStage(type, item.Key, item.Threshold);
        }
        if (globalStatus.IsErr()) return globalStatus;

        pipeline.SetUniqueThreadPoolConfig(new GThreadPoolConfig
        {
            DefaultThreadSize = storage.DefaultThreadSize,
            SecondaryThreadSize = storage.SecondaryThreadSize,
        });

        return new CStatus();
    }

    private static string GetTypeName(Type type)
        => type.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"type name unavailable for [{type}]");

    private static bool TryResolveType(string name, Type baseType, out Type type)
    {
        type = Type.GetType(name, throwOnError: false)!;
        return type is not null && !type.IsAbstract && baseType.IsAssignableFrom(type);
    }

    private static CStatus InvalidType(string category, string name)
        => new($"{category} type [{name}] cannot be resolved");

    private static bool TryResolve(
        IReadOnlyList<int> ids,
        IReadOnlyDictionary<int, GElement> elements,
        out GElement[] result)
    {
        result = new GElement[ids.Count];
        for (var current = 0; current < ids.Count; current++)
            if (!elements.TryGetValue(ids[current], out result[current]!)) return false;
        return true;
    }

    private sealed class PipelineStorage
    {
        public int Version { get; set; }
        public List<ElementStorage> Elements { get; set; } = [];
        public List<TypedKeyStorage> Events { get; set; } = [];
        public List<ParamStorage> Params { get; set; } = [];
        public List<DaemonStorage> Daemons { get; set; } = [];
        public List<StageStorage> Stages { get; set; } = [];
        public int DefaultThreadSize { get; set; }
        public int SecondaryThreadSize { get; set; }
    }

    private sealed class ElementStorage
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public GElementType ElementType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Loop { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public GElementTimeoutStrategy TimeoutStrategy { get; set; }
        public bool IsTopLevel { get; set; }
        public List<int> DependencyIds { get; set; } = [];
        public List<int> ChildIds { get; set; } = [];
        public List<string> AspectTypeNames { get; set; } = [];
        public List<TypedKeyStorage> ElementParams { get; set; } = [];
    }

    private class TypedKeyStorage
    {
        public string Key { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
    }

    private sealed class ParamStorage : TypedKeyStorage
    {
        public bool Backtrace { get; set; }
    }

    private sealed class DaemonStorage
    {
        public string TypeName { get; set; } = string.Empty;
        public long IntervalMilliseconds { get; set; }
    }

    private sealed class StageStorage : TypedKeyStorage
    {
        public int Threshold { get; set; }
    }
}
