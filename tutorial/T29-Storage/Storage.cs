using CsCGraph;

internal static class Program
{
    private static int TutorialStorage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "storage.cgraph");
        try
        {
            var source = new GPipeline();

            var status = source.RegisterGElement<MyNode1>(out var a, [], "nodeA");
            status += source.RegisterGElement<MyNode2>(out var b, [a], "nodeB");
            status += source.RegisterGElement<MyNode1>(out var c, [a], "nodeC");
            status += source.RegisterGElement<MyNode2>(out _, [b, c], "nodeD");

            status += source.Save(path);

            if (status.IsErr()) return status.GetCode();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] pipeline has been saved to {path}");

            var loaded = new GPipeline();

            status = loaded.Load(path);
            if (status.IsErr()) return status.GetCode();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] created another pipeline and loaded it through C# reflection");

            status += loaded.Process();

            return status.GetCode();
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static int Main()
    {
        return TutorialStorage();
    }
}
