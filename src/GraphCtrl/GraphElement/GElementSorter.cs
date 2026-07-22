namespace CsCGraph;

internal static class GElementSorter
{
    internal static CStatus Sort(
        IReadOnlyList<GElement> source,
        out GElement[] sorted)
    {
        sorted = [];
        if (source.Count == 0)
        {
            return new CStatus();
        }

        var index = new Dictionary<GElement, int>(
            source.Count,
            ReferenceEqualityComparer.Instance);
        for (var current = 0; current < source.Count; current++)
        {
            if (!index.TryAdd(source[current], current))
            {
                return new CStatus($"element [{source[current].GetName()}] registered twice");
            }
        }

        var indegrees = new int[source.Count];
        var successors = new List<int>[source.Count];
        for (var current = 0; current < successors.Length; current++)
        {
            successors[current] = [];
        }

        for (var current = 0; current < source.Count; current++)
        {
            foreach (var dependency in source[current].Dependencies)
            {
                if (!index.TryGetValue(dependency, out var dependencyIndex))
                {
                    return new CStatus($"element [{source[current].GetName()}] has an unregistered dependency " +
                        $"[{dependency.GetName()}]");
                }

                if (dependencyIndex == current)
                {
                    return new CStatus($"element [{source[current].GetName()}] depends on itself");
                }

                indegrees[current]++;
                successors[dependencyIndex].Add(current);
            }
        }

        var ready = new Queue<int>();
        for (var current = 0; current < indegrees.Length; current++)
        {
            if (indegrees[current] == 0)
            {
                ready.Enqueue(current);
            }
        }

        var result = new GElement[source.Count];
        var count = 0;
        while (ready.Count > 0)
        {
            var current = ready.Dequeue();
            result[count++] = source[current];
            foreach (var successor in successors[current])
            {
                if (--indegrees[successor] == 0)
                {
                    ready.Enqueue(successor);
                }
            }
        }

        if (count != source.Count)
        {
            var cyclicNames = new List<string>();
            for (var current = 0; current < indegrees.Length; current++)
            {
                if (indegrees[current] > 0)
                {
                    cyclicNames.Add(source[current].GetName());
                }
            }

            return new CStatus($"graph contains cycle: {string.Join(", ", cyclicNames)}");
        }

        sorted = result;
        return new CStatus();
    }
}
