namespace CsCGraph;

public readonly struct CStatus
{
    public const int StatusOk = 0;
    public const int StatusErr = -1;
    public const int StatusCrash = -996;

    private readonly int _errorCode;
    private readonly string? _errorInfo;

    public CStatus()
    {
    }

    public CStatus(string errorInfo)
        : this(StatusErr, errorInfo)
    {
    }

    public CStatus(int errorCode, string errorInfo)
    {
        _errorCode = errorCode;
        _errorInfo = errorInfo;
    }

    public int ErrorCode => _errorCode;

    public string ErrorInfo => GetInfo();

    public bool IsOk() => _errorCode == StatusOk;

    public bool IsErr() => _errorCode < StatusOk;

    public int GetCode() => _errorCode;

    public string GetInfo() => _errorInfo ?? string.Empty;

    public static CStatus operator +(CStatus left, CStatus right)
        => left.IsErr() || right.IsOk() ? left : right;

    public override string ToString()
        => IsOk() ? "OK" : $"ERR[{_errorCode}] {GetInfo()}";
}
