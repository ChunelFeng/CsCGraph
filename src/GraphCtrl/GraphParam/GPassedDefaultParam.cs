namespace CsCGraph;

public class GPassedDefaultParam : GPassedParam
{
    public override void Clone(GPassedParam param)
    {
        ArgumentNullException.ThrowIfNull(param);
    }
}

public sealed class GAspectDefaultParam : GPassedDefaultParam;
public sealed class GDaemonDefaultParam : GPassedDefaultParam;
public sealed class GEventDefaultParam : GPassedDefaultParam;
public sealed class GStageDefaultParam : GStageParam;
