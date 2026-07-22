using CsCGraph;

public sealed class MyMutable : GMutable
{
    protected override CStatus Reshape(IReadOnlyList<GElement> elements)
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        switch (param.ICount % 4)
        {
            case 0:
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ---- run as a->[b,c]");
                Link(elements[0], elements[1]);
                Link(elements[0], elements[2]);
                break;
            case 1:
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ---- run as c(*3)->b->a");
                Link(elements[2], elements[1], 3);
                Link(elements[1], elements[0]);
                break;
            case 2:
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ---- run as a->c, do not run b");
                Link(elements[0], elements[2]);
                break;
            case 3:
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ---- run as [a,b(*2),c]");
                Activate(elements[0]);
                Activate(elements[1], 2);
                Activate(elements[2]);
                break;
        }

        return new CStatus();
    }
}
