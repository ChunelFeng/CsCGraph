using CsCGraph;

public sealed class MyParamCondition : GCondition
{
    protected override int Choose()
    {
        var param = GetGParamWithNoEmpty<MyParam>("param1");
        param.Lock();
        int count;
        try
        {
            count = param.ICount;
        }
        finally
        {
            param.Unlock();
        }

        var range = GetChildrenCount();
        return range == 0 ? GConditionIndex.Last : count % range;
    }
}
