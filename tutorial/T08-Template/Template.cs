using CsCGraph;

internal static class Program
{
    private static int TutorialTemplate()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElementWithArgs<MyTemplateNode<int, float>>(
            out var a, [], 3, 3.5f);
        status += pipeline.RegisterGElementWithArgs<MyTemplateNode<int, float>>(
            out var b, [a], 5, 3.75f);
        status += pipeline.RegisterGElementWithArgs<MyTemplateNode<int>>(
            out var c, [b], 8);
        status += pipeline.RegisterGElement<MyTemplateV2Node<TemplateId4>>(
            out _, [c]);

        status += pipeline.Process();

        return status.GetCode();
    }

    public static int Main()
    {
        return TutorialTemplate();
    }
}
