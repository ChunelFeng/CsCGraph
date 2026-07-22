namespace CsCGraph;

using System.Collections.Concurrent;
using System.Threading.Channels;

internal sealed class GMessageManager
{
    private interface ITopic
    {
        Type MessageType { get; }
        int Capacity { get; }
        void Complete();
    }

    private sealed class Topic<T> : ITopic where T : GMessageParam
    {
        internal Topic(int capacity)
        {
            Capacity = capacity;
            Channel = System.Threading.Channels.Channel.CreateBounded<T>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false,
                });
        }
        public Type MessageType => typeof(T);
        public int Capacity { get; }
        internal Channel<T> Channel { get; }
        public void Complete() => Channel.Writer.TryComplete();
    }

    private readonly object _mutationLock = new();
    private readonly Dictionary<string, ITopic> _sendRecv = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<int, ITopic>> _pubSub = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, ITopic> _connections = new();
    private int _nextConnectionId;

    internal CStatus CreateTopic<T>(string topic, int capacity) where T : GMessageParam
    {
        if (capacity <= 0) return new CStatus("message capacity must be positive");
        lock (_mutationLock)
        {
            if (_sendRecv.TryGetValue(topic, out var current))
                return current.MessageType == typeof(T) && current.Capacity == capacity
                    ? new CStatus() : new CStatus($"create topic [{topic}] duplicate");
            _sendRecv.Add(topic, new Topic<T>(capacity));
            return new CStatus();
        }
    }

    internal async ValueTask<CStatus> Send<T>(string topic, T value, GMessagePushStrategy strategy)
        where T : GMessageParam
    {
        if (!TryGetSendTopic<T>(topic, out var typed)) return new CStatus($"topic [{topic}] not found or type mismatch");
        if (strategy == GMessagePushStrategy.Wait)
        {
            await typed.Channel.Writer.WriteAsync(value).ConfigureAwait(false);
            return new CStatus();
        }
        if (typed.Channel.Writer.TryWrite(value)) return new CStatus();
        if (strategy == GMessagePushStrategy.DropLatest) return new CStatus();
        _ = typed.Channel.Reader.TryRead(out _);
        return typed.Channel.Writer.TryWrite(value) ? new CStatus() : new CStatus("message queue write failed");
    }

    internal async ValueTask<GMessageResult<T>> Receive<T>(string topic, TimeSpan timeout)
        where T : GMessageParam
    {
        if (!TryGetSendTopic<T>(topic, out var typed))
            return new(new CStatus($"topic [{topic}] not found or type mismatch"), null);
        return await ReadAsync(typed, timeout).ConfigureAwait(false);
    }

    internal int Bind<T>(string topic, int capacity) where T : GMessageParam
    {
        if (capacity <= 0) return -1;
        var id = Interlocked.Increment(ref _nextConnectionId);
        var queue = new Topic<T>(capacity);
        lock (_mutationLock)
        {
            if (!_pubSub.TryGetValue(topic, out var subscribers))
            {
                subscribers = [];
                _pubSub.Add(topic, subscribers);
            }
            subscribers.Add(id, queue);
            _connections[id] = queue;
        }
        return id;
    }

    internal async ValueTask<CStatus> Publish<T>(string topic, T value, GMessagePushStrategy strategy)
        where T : GMessageParam
    {
        ITopic[] subscribers;
        lock (_mutationLock)
        {
            if (!_pubSub.TryGetValue(topic, out var current)) return new CStatus($"topic [{topic}] not found");
            subscribers = current.Values.ToArray();
        }
        var status = new CStatus();
        foreach (var subscriber in subscribers)
        {
            if (subscriber is not Topic<T> typed) return new CStatus($"topic [{topic}] type mismatch");
            status += await WriteTopic(typed, value, strategy).ConfigureAwait(false);
        }
        return status;
    }

    internal async ValueTask<GMessageResult<T>> Subscribe<T>(int connectionId, TimeSpan timeout)
        where T : GMessageParam
    {
        if (!_connections.TryGetValue(connectionId, out var queue) || queue is not Topic<T> typed)
            return new(new CStatus($"connection [{connectionId}] not found or type mismatch"), null);
        return await ReadAsync(typed, timeout).ConfigureAwait(false);
    }

    internal CStatus Detach(string topic, int connectionId)
    {
        lock (_mutationLock)
        {
            if (!_pubSub.TryGetValue(topic, out var subscribers) || !subscribers.Remove(connectionId, out var queue))
                return new CStatus($"connection [{connectionId}] not found");
            _connections.TryRemove(connectionId, out _);
            queue.Complete();
            return new CStatus();
        }
    }

    internal CStatus RemoveTopic(string topic)
    {
        lock (_mutationLock)
        {
            if (!_sendRecv.Remove(topic, out var removed))
                return new CStatus($"topic [{topic}] not found");
            removed.Complete();
            return new CStatus();
        }
    }

    internal CStatus DropTopic(string topic)
    {
        lock (_mutationLock)
        {
            if (!_pubSub.Remove(topic, out var subscribers))
                return new CStatus($"topic [{topic}] not found");
            foreach (var item in subscribers)
            {
                _connections.TryRemove(item.Key, out _);
                item.Value.Complete();
            }
            return new CStatus();
        }
    }

    internal CStatus Clear()
    {
        lock (_mutationLock)
        {
            foreach (var topic in _sendRecv.Values) topic.Complete();
            foreach (var subscribers in _pubSub.Values)
                foreach (var topic in subscribers.Values) topic.Complete();
            _sendRecv.Clear();
            _pubSub.Clear();
            _connections.Clear();
            _nextConnectionId = 0;
        }
        return new CStatus();
    }

    private bool TryGetSendTopic<T>(string topic, out Topic<T> typed) where T : GMessageParam
    {
        lock (_mutationLock)
        {
            if (_sendRecv.TryGetValue(topic, out var queue) && queue is Topic<T> found)
            {
                typed = found;
                return true;
            }
            typed = null!;
            return false;
        }
    }

    private static async ValueTask<CStatus> WriteTopic<T>(Topic<T> topic, T value, GMessagePushStrategy strategy)
        where T : GMessageParam
    {
        if (strategy == GMessagePushStrategy.Wait)
        {
            await topic.Channel.Writer.WriteAsync(value).ConfigureAwait(false);
            return new CStatus();
        }
        if (topic.Channel.Writer.TryWrite(value) || strategy == GMessagePushStrategy.DropLatest) return new CStatus();
        _ = topic.Channel.Reader.TryRead(out _);
        return topic.Channel.Writer.TryWrite(value) ? new CStatus() : new CStatus("message queue write failed");
    }

    private static async ValueTask<GMessageResult<T>> ReadAsync<T>(Topic<T> topic, TimeSpan timeout)
        where T : GMessageParam
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            var value = await topic.Channel.Reader.ReadAsync(cancellation.Token).ConfigureAwait(false);
            return new(new CStatus(), value);
        }
        catch (OperationCanceledException)
        {
            return new(new CStatus("message receive timeout"), null);
        }
        catch (ChannelClosedException)
        {
            return new(new CStatus("message topic closed"), null);
        }
    }
}
