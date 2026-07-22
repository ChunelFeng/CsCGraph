namespace CsCGraph;

public sealed class GPerfInfo
{
    public uint Loop { get; internal set; }
    public double FirstStartTs { get; internal set; }
    public double LastFinishTs { get; internal set; }
    public double AccuCostTs { get; internal set; }
    public bool InLongestPath { get; internal set; }
}
