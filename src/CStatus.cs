namespace src;

public class CStatus
{
    private int ErrorCode { get; set; } = 0;
    private string ErrorInfo { get; set; } = string.Empty;

    public CStatus()
    {
    }

    public CStatus(string errorInfo)
    {
        ErrorCode = -1;
        ErrorInfo = errorInfo;
    }

    public bool IsOk()
    {
        return ErrorCode == 0;
    }

    public bool IsErr()
    {
        return ErrorCode < 0;
    }

    private CStatus AddAssign(CStatus cur)
    {
        if (!IsOk() || cur.IsOk())
        {
           return this;
        }

        ErrorCode = cur.ErrorCode;
        ErrorInfo = cur.ErrorInfo;
        return this;
    }

    public static CStatus operator +(CStatus a, CStatus b)
    {
        return a.AddAssign(b);
    }
}