namespace CsCGraph;

using System.Threading.Channels;

public abstract class GMessageParam
{
}

public sealed class GMessage<T> where T : GMessageParam
{
    private readonly Channel<T> _channel;
    private readonly int _capacity;
    private readonly int _connId;

    public GMessage(int size = 1024, int connectionId = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _capacity = size;
        _channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions(size)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
        _connId = connectionId;
    }

    public void Send(T value, GMessagePushStrategy strategy = GMessagePushStrategy.Wait)
    {
        if (strategy == GMessagePushStrategy.Wait)
        {
            _channel.Writer.WriteAsync(value).AsTask().GetAwaiter().GetResult();
            return;
        }

        if (_channel.Writer.TryWrite(value) || strategy == GMessagePushStrategy.DropLatest) return;
        _ = _channel.Reader.TryRead(out _);
        if (!_channel.Writer.TryWrite(value))
            throw new InvalidOperationException("message queue write failed");
    }

    public CStatus Recv(out T value, long timeout)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));
        try
        {
            value = _channel.Reader.ReadAsync(cancellation.Token).AsTask().GetAwaiter().GetResult();
            return new CStatus();
        }
        catch (OperationCanceledException)
        {
            value = null!;
            return new CStatus("receive message timeout");
        }
        catch (ChannelClosedException)
        {
            value = null!;
            return new CStatus("message topic closed");
        }
    }

    public int GetCapacity() => _capacity;
    public int GetConnId() => _connId;
}

public readonly record struct GMessageResult<T>(CStatus Status, T? Value)
    where T : GMessageParam;
