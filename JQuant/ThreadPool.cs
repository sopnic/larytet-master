
using System;
using System.Threading;
using System.Collections.Generic;

namespace JQuant
{

    /// <summary>
    /// this method will be called from a context of dynamically allocated
    /// thread
    /// </summary>
    public delegate void Job(object argument);
    
    /// <summary>
    /// Similar to the System.Threading.ThreadPool
    /// Every subsystem can create it's own pool of threads with specified number of
    /// threads. Pay attention, that there can be OS related limitations for the total  
    /// number of threads coexisting in the system.
    /// </summary>
    public class ThreadPool
    {
        
        public ThreadPool(string name, int size)
        {
            this.Name = name;
            this.Size = size;
            
            jobThreads = new Stack<JobThread>(size);
            for (int i = 0;i < size;i++)
            {
                jobThreads.Push(new JobThread());
            }
        }

        public bool DoJob(Job job, object jobArgument)
        {
            return true;
        }


        protected string Name;
        protected int Size;

        Stack<JobThread> jobThreads;

        protected class JobThread
        {
        }

    }
}
