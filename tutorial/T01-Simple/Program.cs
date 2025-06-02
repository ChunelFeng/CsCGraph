using src;

namespace T01_Simple;

class Program
{
    private class MyNode1 : GNode
    {
        protected override async Task<CStatus> RunAsync()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleeping for 1s.");
            await Task.Delay(1000);
            return new CStatus();
        }
    }

    private class MyNode2 : GNode
    {
        protected override async Task<CStatus> RunAsync()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleeping for 2s.");
            await Task.Delay(2000);
            return new CStatus();
        }
    }

    private static async Task Main(string[] args)
    {
        var pipeline = new GPipeline();
        pipeline.RegisterGElement<MyNode1>(out var a, Array.Empty<GElement>(), "nodeA");
        pipeline.RegisterGElement<MyNode2>(out var b, new[] { a }, "nodeB");
        pipeline.RegisterGElement<MyNode1>(out var c, new[] { a }, "nodeC");
        pipeline.RegisterGElement<MyNode2>(out var d, new[] { b, c }, "nodeD");

        await pipeline.ProcessAsync();
    }
}