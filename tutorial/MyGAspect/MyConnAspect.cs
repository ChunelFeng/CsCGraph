using CsCGraph;

public sealed class MyConnAspect : GAspect
{
    private bool _connected;
    public override CStatus BeginInit()
    {
        if (GetAParam<MyConnParam>() is { } param)
        {
            _connected = true;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyConnAspect] [{param.Ip} : {param.Port}] connected");
        }
        return new CStatus();
    }

    public override void FinishDestroy(CStatus status)
    {
        if (_connected && GetAParam<MyConnParam>() is { } param)
        {
            _connected = false;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ----> [MyConnAspect] [{param.Ip} : {param.Port}] disconnected");
        }
    }
}
