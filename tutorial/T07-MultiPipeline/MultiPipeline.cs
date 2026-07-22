using CsCGraph;

internal static class Program
{
    private static ValueTask<CStatus> StartPipeline1(GPipeline pipeline)
    {
        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "node1A");
        status += pipeline.RegisterGElement<MyNode1>(out var b, [a], "node1B");
        status += pipeline.RegisterGElement<MyNode1>(out _, [b], "node1C");

        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(3);
    }

    private static ValueTask<CStatus> StartPipeline2(GPipeline pipeline)
    {
        var status = pipeline.RegisterGElement<MyNode2>(out var a, [], "node2A");
        status += pipeline.RegisterGElement<MyNode2>(out _, [a], "node2B");
        status += pipeline.RegisterGElement<MyNode2>(out _, [a], "node2C");

        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(2);
    }

    private static ValueTask<CStatus> StartPipeline3(GPipeline pipeline)
    {
        var a = pipeline.CreateGNode<MyNode1>(new GNodeInfo([], "node3A", 1));
        var b = pipeline.CreateGNode<MyNode2>(new GNodeInfo([a], "node3B", 1));
        var c = pipeline.CreateGNode<MyNode1>(new GNodeInfo([a], "node3C", 1));
        var d = pipeline.CreateGNode<MyNode1>(new GNodeInfo([b, c], "node3D", 1));
        var region = pipeline.CreateGGroup<GRegion>([a, b, c, d]);

        var status = pipeline.RegisterGElement(region);

        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(2);
    }

    private static async Task<int> TutorialMultiPipeline()
    {
        var pipelines = new[]
        {
            new GPipeline(),
            new GPipeline(),
            new GPipeline(),
        };
        var results = await Task.WhenAll(
            StartPipeline1(pipelines[0]).AsTask(),
            StartPipeline2(pipelines[1]).AsTask(),
            StartPipeline3(pipelines[2]).AsTask());

        var status = results.Aggregate(new CStatus(), (current, result) => current + result);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] multi pipeline status = [{status.GetCode()}]");

        return status.GetCode();
    }

    public static Task<int> Main()
    {
        return TutorialMultiPipeline();
    }
}
