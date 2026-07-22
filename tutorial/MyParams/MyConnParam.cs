using CsCGraph;

public sealed class MyConnParam : GPassedParam
{
    public const string Key = "conn";
    public string Ip { get; set; } = "0.0.0.0";
    public short Port { get; set; }

    public override void Clone(GPassedParam param)
    {
        if (param is not MyConnParam source)
        {
            throw new ArgumentException("param must be MyConnParam", nameof(param));
        }
        Ip = source.Ip;
        Port = source.Port;
    }
}
