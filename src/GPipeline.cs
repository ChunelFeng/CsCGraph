namespace src;

using System.Collections.Generic;

public class GPipeline
{
    private readonly GParamManager _paramManager = new ();
    private readonly GExecutor _executor = new ();

    public CStatus Process(int times = 1)
    {
        var status = Init();
        while (times-- > 0 && status.IsOk())
        {
            status += Run();
        }
        status += Destroy();
        return status;
    }

    public CStatus RegisterGElement<T>(out GElement element, 
                                       IEnumerable<GElement> depends,
                                       string name,
                                       int loop = 1) where T : GElement, new()
    {
        element = new T();
        element.AddElementInfo(depends, name, loop);
        element.SetManager(_paramManager);
        return _executor.AddElement(element);
    }

    public CStatus Init()
    {
        return _executor.Init();
    }

    public CStatus Run()
    {
        var status =_paramManager.Setup();
        if (status.IsErr()) { return status; }

        status += _executor.Run();
        _paramManager.Reset(status);
        return status;
    }

    public CStatus Destroy()
    {
        return _executor.Destroy();
    }
}
