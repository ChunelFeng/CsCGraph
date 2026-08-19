

<h1 align="center">CsCGraph</h1>

> CsCGraph is a C# native, CGraph-API-liked project

# 一. 简介

CsCGraph 是一个基于原生 C# `net10.0/C# 14` 实现的调度框架，是 [CGraph](https://github.com/ChunelFeng/CGraph) 多语言版本之一

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

---
<details>
<summary><b>附录-1. 版本信息</b></summary>

[2025.06.01 - v1.0.0 - Chunel]
* 提供图化执行功能，支持非依赖节点并行计算
* 提供参数传递功能

[2026.07.22 - v1.1.0 - Chunel]
* 增加全量功能

</details>
