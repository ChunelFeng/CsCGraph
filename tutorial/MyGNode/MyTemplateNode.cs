using CsCGraph;

public sealed class MyTemplateNode<T>(int number) : GTemplateNode<T>
{
    private readonly float _score = 7.0f;

    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MyTemplateNode] num = [{number}], score = [{_score:F2}]");
        return new CStatus();
    }
}

public sealed class MyTemplateNode<T1, T2>(
    int number,
    float score) : GTemplateNode<(T1, T2)>
{
    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MyTemplateNode] num = [{number}], score = [{score:F2}]");
        return new CStatus();
    }
}
