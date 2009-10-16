
using System;

namespace JQuant
{
    public interface ICriticalSection
    {
        void Enter();
        void Exit();   
    }

    /// <summary>
    /// do nothing to protect a critical section
    /// debug only
    /// </summary>
    public class DummyCriticalSection : ICriticalSection
    {
        public void Enter()
        {
        }
        
        public void Exit()
        {
        }       
    }

    /// <summary>
    /// use System.Threading.Monitor API (similar to lock)
    /// </summary>
    public class LockCriticalSection : ICriticalSection
    {
        public LockCriticalSection(object lockObject)
        {
            this.lockObject = lockObject;
        }
        
        public void Enter()
        {
            System.Threading.Monitor.Enter(lockObject);
        }
        
        public void Exit()
        {
            System.Threading.Monitor.Exit(lockObject);
        }

        protected object lockObject;
    }

    /// <summary>
    /// use System.Threading.Monitor API (similar to lock)
    /// The object itself is a synchronization object
    /// </summary>
    public class SyncObject : ICriticalSection
    {
        public void Enter()
        {
            System.Threading.Monitor.Enter(this);
        }
        
        public void Exit()
        {
            System.Threading.Monitor.Exit(this);
        }
    }
    
}
