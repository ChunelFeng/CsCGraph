using CsCGraph;

public sealed class MyHoldNode : GNode
{
    private const string HoldParamName = "hold-param";
    protected override CStatus Init() => CreateGParam<MyParam>(HoldParamName);
    protected override CStatus Run()
    {
        var param = GetGParamWithNoEmpty<MyParam>(HoldParamName);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] hold iValue [{++param.IValue}]");
        return new CStatus();
    }
    protected override bool IsHold()
    {
        var param = GetGParam<MyParam>(HoldParamName);
        return param is not null && param.IValue < 5;
    }
}
