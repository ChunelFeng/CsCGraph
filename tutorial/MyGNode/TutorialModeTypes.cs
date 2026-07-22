public readonly struct FastThenSlow : IStageTiming
{
    public static int BeforeSeconds => 1;
    public static int AfterSeconds => 2;
}

public readonly struct SlowThenFast : IStageTiming
{
    public static int BeforeSeconds => 3;
    public static int AfterSeconds => 1;
}
