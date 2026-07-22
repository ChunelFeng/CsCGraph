using CsCGraph;

public interface ITemplateId
{
    static abstract int Value { get; }
}

public readonly struct TemplateId4 : ITemplateId
{
    public static int Value => 4;
}

public sealed class MyTemplateV2Node<TId> : GNode
    where TId : ITemplateId
{
    protected override CStatus Run()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MyTemplateV2Node] template id = [{TId.Value}]");
        return new CStatus();
    }
}
