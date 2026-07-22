namespace CsCGraph;

internal static class GPerf
{
    internal static CStatus Perf(GPipeline pipeline, TextWriter writer)
    {
        var elements = Collect(pipeline.ElementManager.RegisteredElements);
        foreach (var element in elements) element.ResetPerfInfo();

        var status = pipeline.Process();
        if (status.IsErr()) return status;

        MarkLongestPath(pipeline.ElementManager.RegisteredElements);
        writer.WriteLine("digraph CGraph {");
        foreach (var element in elements)
        {
            var info = element.GetPerfInfo();
            var marker = info.InLongestPath ? ", color=red" : string.Empty;
            writer.WriteLine(
                $"  \"{element.GetName()}\" [label=\"{element.GetName()}\\n{info.AccuCostTs:F2} ms / {info.Loop}\"{marker}];");
            foreach (var dependency in element.Dependencies)
                writer.WriteLine($"  \"{dependency.GetName()}\" -> \"{element.GetName()}\";");
        }
        writer.WriteLine("}");
        return status;
    }

    private static List<GElement> Collect(IReadOnlyList<GElement> roots)
    {
        var result = new List<GElement>();
        var visited = new HashSet<GElement>(ReferenceEqualityComparer.Instance);
        void Visit(GElement element)
        {
            if (!visited.Add(element)) return;
            result.Add(element);
            if (element is not GGroup group) return;
            foreach (var child in group.Children) Visit(child);
        }
        foreach (var root in roots) Visit(root);
        return result;
    }

    private static void MarkLongestPath(IReadOnlyList<GElement> elements)
    {
        if (GElementSorter.Sort(elements, out var sorted).IsErr()) return;
        var cost = new Dictionary<GElement, double>(ReferenceEqualityComparer.Instance);
        var parent = new Dictionary<GElement, GElement?>(ReferenceEqualityComparer.Instance);
        foreach (var element in sorted)
        {
            GElement? bestParent = null;
            var best = 0.0;
            foreach (var dependency in element.Dependencies)
                if (cost.TryGetValue(dependency, out var candidate) && candidate >= best)
                { best = candidate; bestParent = dependency; }
            cost[element] = best + element.GetPerfInfo().AccuCostTs;
            parent[element] = bestParent;
        }
        if (cost.Count == 0) return;
        var current = cost.MaxBy(static pair => pair.Value).Key;
        while (current is not null)
        {
            current.MarkLongestPath();
            current = parent[current];
        }
    }
}
