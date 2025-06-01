using src;

namespace T05_Param;

class Program
{
    private class MyParam : GParam
    {
        protected override void Reset(CStatus curStatus)
        {
            Val = 0;
        }

        public int Val { set; get; } = 0;
        public int Loop { set; get; } = 0;
    }

    private class MyReadParamNode : GNode
    {
        protected override CStatus Init()
        {
            var status = CreateGParam<MyParam>("param1");
            return status;
        }

        protected override CStatus Run()
        {
            var param = GetGParamWithNoEmpty<MyParam>("param1");
            param.Lock();
            Console.WriteLine($"[read] [{GetName()}] loop = {param.Loop}, val = {param.Val}");
            param.Unlock();
            return new CStatus();
        }
    }

    private class MyWriteParamNode : GNode
    {
        protected override CStatus Run()
        {
            var param = GetGParam<MyParam>("param1");
            if (null == param)
            {
                return new CStatus("get param1 failed");
            }

            param.Lock();
            param.Val += 1;
            param.Loop += 1;
            Console.WriteLine($"[write] [{GetName()}] loop = {param.Loop}, val = {param.Val}");
            param.Unlock();
            return new CStatus();
        }
    }

    private static void Main(string[] args)
    {
        var pipeline = new GPipeline();
        pipeline.RegisterGElement<MyReadParamNode>(out var a, [], "readNodeA");
        pipeline.RegisterGElement<MyReadParamNode>(out var b, [a], "readNodeB");
        pipeline.RegisterGElement<MyWriteParamNode>(out var c, [a], "writeNodeC");
        pipeline.RegisterGElement<MyWriteParamNode>(out var d, [a], "writeNodeD", 2);
        pipeline.RegisterGElement<MyReadParamNode>(out var e, [a], "readNodeE");
        pipeline.RegisterGElement<MyWriteParamNode>(out var f, [b, c, d, e], "writeNodeF");

        pipeline.Process(3);
    }
}