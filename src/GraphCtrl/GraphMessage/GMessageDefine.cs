namespace CsCGraph;

public enum GMessagePushStrategy
{
    Wait = 1,
    Replace = 2,
    DropOldest = Replace,
    Drop = 3,
    DropLatest = Drop,
}
