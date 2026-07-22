namespace CsCGraph;

public static class GMessageUtils
{
    private static readonly GMessageManager s_manager = new();
    public static CStatus CreateTopic<T>(string topic, int capacity) where T : GMessageParam => s_manager.CreateTopic<T>(topic, capacity);
    public static CStatus RemoveTopic(string topic) => s_manager.RemoveTopic(topic);
    public static ValueTask<CStatus> SendTopicValueAsync<T>(string topic, T value, GMessagePushStrategy strategy = GMessagePushStrategy.Wait) where T : GMessageParam => s_manager.Send(topic, value, strategy);
    public static CStatus SendTopicValue<T>(string topic, T value, GMessagePushStrategy strategy = GMessagePushStrategy.Wait) where T : GMessageParam
        => SendTopicValueAsync(topic, value, strategy).AsTask().GetAwaiter().GetResult();
    public static ValueTask<GMessageResult<T>> RecvTopicValueAsync<T>(string topic, TimeSpan timeout) where T : GMessageParam => s_manager.Receive<T>(topic, timeout);
    public static CStatus RecvTopicValue<T>(string topic, out T? value, long timeoutMilliseconds = int.MaxValue) where T : GMessageParam
    {
        var result = RecvTopicValueAsync<T>(topic, TimeSpan.FromMilliseconds(timeoutMilliseconds)).AsTask().GetAwaiter().GetResult();
        value = result.Value;
        return result.Status;
    }
    public static int BindTopic<T>(string topic, int capacity) where T : GMessageParam => s_manager.Bind<T>(topic, capacity);
    public static ValueTask<CStatus> PubTopicValueAsync<T>(string topic, T value, GMessagePushStrategy strategy = GMessagePushStrategy.Wait) where T : GMessageParam => s_manager.Publish(topic, value, strategy);
    public static CStatus PubTopicValue<T>(string topic, T value, GMessagePushStrategy strategy = GMessagePushStrategy.Wait) where T : GMessageParam
        => PubTopicValueAsync(topic, value, strategy).AsTask().GetAwaiter().GetResult();
    public static ValueTask<GMessageResult<T>> SubTopicValueAsync<T>(int connectionId, TimeSpan timeout) where T : GMessageParam => s_manager.Subscribe<T>(connectionId, timeout);
    public static CStatus SubTopicValue<T>(int connectionId, out T? value, long timeoutMilliseconds = int.MaxValue) where T : GMessageParam
    {
        var result = SubTopicValueAsync<T>(connectionId, TimeSpan.FromMilliseconds(timeoutMilliseconds)).AsTask().GetAwaiter().GetResult();
        value = result.Value;
        return result.Status;
    }
    public static CStatus DropTopic(string topic) => s_manager.DropTopic(topic);
    public static CStatus DetachConnId(string topic, int connectionId) => s_manager.Detach(topic, connectionId);
    public static CStatus Clear() => s_manager.Clear();
}
