namespace CsCGraph;

public enum GEventType
{
    Sync,
    Async,
}

public enum GEventAsyncStrategy
{
    WaitByPipeline,
    FireAndTrack,
}
