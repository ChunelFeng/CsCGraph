using CsCGraph;

public sealed class MyTemplateAspect<T1, T2>(int age, double score)
    : GAspect
{
    public override CStatus BeginInit()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyTemplateAspect] [{GetName()}] age [{age}], score [{score:F2}]");
        return new CStatus();
    }
}
