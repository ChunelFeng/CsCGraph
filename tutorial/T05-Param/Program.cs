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
            return CreateGParam<MyParam>("param1");
        }

        protected override CStatus Run()
        {
            var param = GetGParamWithNoEmpty<MyParam>("param1");
            Console.WriteLine($"[read] [{GetName()}] loop = {param.Loop}, val = {param.Val}");
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
            
            Console.WriteLine($"[write] [{GetName()}] loop = {param.Loop}, val = {param.Val}");

            param.Val += 1;
            param.Loop += 1;
            return new CStatus();
        }
    }

    private static void Main(string[] args)
    {
        var pipeline = new GPipeline();
        pipeline.RegisterGElement<MyWriteParamNode>(out var a, Array.Empty<GElement>(), "writeNodeA");
        pipeline.RegisterGElement<MyReadParamNode>(out var b, new[] { a }, "readNodeB");

        pipeline.Process(3);
    }
}
