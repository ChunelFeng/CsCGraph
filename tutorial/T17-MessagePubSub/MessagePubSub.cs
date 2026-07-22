using CsCGraph;

internal static class Program
{
    private static ValueTask<CStatus> RunSubscriber(int connectionId)
    {
        var pipeline = new GPipeline();

        var status = pipeline.RegisterGElementWithArgs<MySubMessageNode<int>>(
            out _, [], connectionId);
        return status.IsErr()
            ? ValueTask.FromResult(status)
            : pipeline.AsyncProcess(5);
    }

    private static ValueTask<CStatus> RunPublisher()
    {
        var publisher = new GPipeline();

        var status = publisher.RegisterGElement<MyPubMessageNode>(out _);
        return status.IsErr()
            ? ValueTask.FromResult(status)
            : publisher.AsyncProcess(5);
    }

    private static async Task<int> TutorialMessagePubSub()
    {
        var connections = Enumerable.Range(0, 3)
            .Select(_ => GMessageUtils.BindTopic<MyMessageParam>("pub-sub", 16)).ToArray();
        var subscribers = connections.Select(connection => RunSubscriber(connection).AsTask()).ToArray();
        var publishTask = RunPublisher().AsTask();

        var statuses = await Task.WhenAll(subscribers.Append(publishTask));

        var status = statuses.Aggregate(new CStatus(), (current, item) => current + item);

        status += GMessageUtils.Clear();

        return status.GetCode();
    }

    public static Task<int> Main()
    {
        return TutorialMessagePubSub();
    }
}
