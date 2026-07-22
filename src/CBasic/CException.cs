namespace CsCGraph;

public sealed class CException : Exception
{
    public CException(string message)
        : this(new CStatus(CStatus.StatusCrash, message))
    {
    }

    public CException(CStatus status)
        : base(status.GetInfo())
    {
        Status = status.IsErr()
            ? status
            : new CStatus(CStatus.StatusCrash, status.GetInfo());
    }

    public CException(string message, Exception innerException)
        : base(message, innerException)
    {
        Status = new CStatus(CStatus.StatusCrash, message);
    }

    public CStatus Status { get; }

    public static CStatus FromException(Exception exception, string context)
        => new(CStatus.StatusCrash,
            $"{context}: {exception.GetType().Name}: {exception.Message}");
}
