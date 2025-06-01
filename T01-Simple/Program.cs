using src;


namespace T01_Simple;

class Program
{
    private class MyNode1 : GNode
    {
        protected override CStatus Run()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleep for 1s.");
            Thread.Sleep(1000);
            return new CStatus();
        }
    }
    
    private class MyNode2 : GNode
    {
        protected override CStatus Run()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleep for 2s.");
            Thread.Sleep(2000);
            return new CStatus();
        }
    }

    private static void Main(string[] args)
    {
        var pipeline = new GPipeline();
        pipeline.RegisterGElement<MyNode1>(out var a, [], "nodeA");
        pipeline.RegisterGElement<MyNode2>(out var b, [a], "nodeB");
        pipeline.RegisterGElement<MyNode1>(out var c, [a], "nodeC");
        pipeline.RegisterGElement<MyNode2>(out var d, [b, c], "nodeD");

        pipeline.Process();
    }
}