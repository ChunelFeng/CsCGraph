using CsCGraph;

internal static class Program
{
    private static ValueTask<CStatus> Send()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode1>(out var a, [], "send-prefix");
        status += pipeline.RegisterGElement<MySendMessageNode>(out _, [a]);

        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(3);
    }

    private static ValueTask<CStatus> Receive()
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElement<MyNode2>(out var a, [], "recv-prefix");
        status += pipeline.RegisterGElement<MyRecvMessageNode>(out _, [a]);

        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(3);
    }

    private static async Task<int> TutorialMessageSendRecv()
    {
        var status = GMessageUtils.CreateTopic<MyMessageParam>("send-recv", 48);
        var results = await Task.WhenAll(Send().AsTask(), Receive().AsTask());
        status += results[0] + results[1];

        status += GMessageUtils.Clear();

        return status.GetCode();
    }

    public static Task<int> Main()
    {
        return TutorialMessageSendRecv();
    }
}
