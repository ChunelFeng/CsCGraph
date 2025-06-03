<h1 align="center"> CsCGraph 说明文档 </h1>

> CsCGraph is a C# native, CGraph-API-liked project, with simple DAG executor and param transfer function.

# 一. 简介

CsCGraph 是一个基于原生 C# `net9.0/version10` 实现的的调度框架，是 [CGraph](https://github.com/ChunelFeng/CGraph) 多语言简化版本之一。主要包含 <b>DAG调度</b> 和 <b>跨算子数据传递</b> 功能。

# 二. 入门Demo

* DAG调度

```c#
class Program
{
    private class MyNode1 : GNode
    {
        protected override CStatus Run()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleep for 1s.");
            Thread.Sleep(1000);
            return new CStatus();
        }
    }

    private class MyNode2 : GNode
    {
        protected override CStatus Run()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{GetName()}], sleep for 2s.");
            Thread.Sleep(2000);
            return new CStatus();
        }
    }

    private static void Main(string[] args)
    {
        var pipeline = new GPipeline();
        pipeline.RegisterGElement<MyNode1>(out var a, Array.Empty<GElement>(), "nodeA");
        pipeline.RegisterGElement<MyNode2>(out var b, new [] {a}, "nodeB");
        pipeline.RegisterGElement<MyNode1>(out var c, new [] {a}, "nodeC");
        pipeline.RegisterGElement<MyNode2>(out var d, new [] {b, c}, "nodeD");

        pipeline.Process();
    }
}
```

* 参数传递

```c#
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
```

---
<details>
<summary><b>附录-1. 版本信息</b></summary>

[2025.06.01 - v1.0.0 - Chunel]
* 提供图化执行功能，支持非依赖节点并行计算
* 提供参数传递功能

</details>
