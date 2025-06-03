namespace src;

using System.Threading;

public abstract class GParam
{
    private readonly ReaderWriterLockSlim _paramLock = new ();
    
    protected internal virtual CStatus Setup()
    {
        return new CStatus();
    }

    protected internal virtual void Reset(CStatus curStatus)
    {
    }

    public void Lock()
    {
        _paramLock.EnterWriteLock();
    }

    public void Unlock()
    {
        _paramLock.ExitWriteLock();
    }
}