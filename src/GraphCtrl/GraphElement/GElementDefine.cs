namespace CsCGraph;

public static class GElementDefaults
{
    public const int CGraphDefaultLoopTimes = 1;
    public const int CGraphDefaultElementLevel = 0;
    public const long CGraphDefaultElementTimeout = 0;
    public const int CGraphDefaultBindingIndex = -1;
}

public enum GElementState
{
    Normal = 0x0000,
    Cancel = 0x1001,
    Suspend = 0x1002,
    Timeout = 0x1010,
}

public enum GElementType
{
    Element = 0x00000000,
    Node = 0x00010000,
    Group = 0x00020000,
    Cluster = 0x00020001,
    Region = 0x00020002,
    Condition = 0x00020004,
    Some = 0x00020008,
    Mutable = 0x0002000A,
    MultiCondition = 0x00020014,
    Adapter = 0x00040000,
    Function = 0x00040001,
    Fence = 0x00040004,
}

public enum GElementTimeoutStrategy
{
    AsError = 0,
    HoldByPipeline = 1,
    NoHold = 2,
}
