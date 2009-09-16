
using System;
using System.Threading;
using System.Collections.Generic;

namespace JQuant
{

    /// <summary>
    /// this method will be called from a context of the job thread
    /// </summary>
    public delegate void Job(object argument);

    /// <summary>
    /// this method will be called when job is done and just before the job 
    /// thread exists
    /// </summary>
    public delegate void JobDone(object argument);
    
    /// <summary>
    /// Similar to the System.Threading.ThreadPool
    /// Every subsystem can create it's own pool of threads with specified number of
    /// threads. Pay attention, that there can be OS related limitations for the total  
    /// number of threads coexisting in the system.
    /// 
    /// ----------- Usage Example -----------
    /// ThreadPool threadPool = new ThreadPool("Pool1", 5); // pool of 5 threads
    /// threadPool.DoJob(job, ack, fsmData); // call the job, fsmData will be called when job is done
    /// 
    /// </summary>
    public class ThreadPool : IResourceThreadPool, IDisposable
    {
        /// <summary>
        /// Create a thread pool
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String"/>
        /// Name of the pool
        /// </param>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// Number of job threads in the pool
        /// </param>
        public ThreadPool(string name, int size)
        {
            this.Name = name;
            this.Size = size;
            MinThreadsFree = size;
            countStart = 0;
            countDone = 0;
            
            jobThreads = new Stack<JobThread>(size);
            runningThreads = new List<JobThread>(size);
            for (int i = 0;i < size;i++)
            {
                JobThread jobThread = new JobThread(this);                
                jobThreads.Push(jobThread);
            }
            
            // add myself to the list of created thread pools
            Resources.ThreadPools.Add(this);
            
        }

        /// <summary>
        /// Call this method to destroy the object
        /// </summary>
        public void Dispose()
        {
            // remove myself from the list of created thread pools
            Resources.ThreadPools.Remove(this);

            lock (jobThreads)
            {
                // clean the stack of threads
                foreach (JobThread jobThread in jobThreads)
                {
                    jobThread.Dispose();
                }
                
                // clean the list of running threads
                foreach (JobThread jobThread in runningThreads)
                {
                    jobThread.Dispose();
                }
            }
        }
        
        ~ThreadPool()
        {
            Console.WriteLine("ThreadPool " + Name + " destroyed");
        }
        
        /// <summary>
        /// Executes job in a thread, calls jobDone after the job done
        /// </summary>
        /// <param name="job">
        /// A <see cref="Job"/>
        /// </param>
        /// <param name="jobDone">
        /// A <see cref="JobDone"/>
        /// </param>
        /// <param name="jobArgument">
        /// A <see cref="System.Object"/>
        /// Arbitrary data specified by the application. Can be null
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool DoJob(Job job, JobDone jobDone, object jobArgument)
        {
            bool result = false;
            JobThread jobThread = default(JobThread);
            
            do
            {
                // allocate a free thread
                lock (jobThreads)
                {
                    if (jobThreads.Count > 0)
                    {
                        jobThread = jobThreads.Pop();
                        runningThreads.Add(jobThread);
                        if (MinThreadsFree > jobThreads.Count)
                        {
                            MinThreadsFree = jobThreads.Count;
                        }
                        countStart++;
                    }
                    else // no job threads available
                    {
                        break;
                    }
                }

                jobThread.Start(job, jobDone, jobArgument);

                    
                result = true;
            }
            while (false);
            
            return result;
        }

        protected void JobDone(JobThread jobThread)
        {
            lock (jobThreads)
            {
                countDone++;
                jobThreads.Push(jobThread);
                runningThreads.Remove(jobThread);
            }
        }

        public int GetSize()
        {
            return Size;
        }
        
        public string GetName()
        {
            return Name;
        }
        
        public int GetMaxCount()
        {
            return (Size-MinThreadsFree);
        }
        
        public int GetCountStart()
        {
            return countStart;
        }
        
        public int GetCountDone()
        {
            return countDone;
        }
        
        protected string Name;
        protected int Size;
        protected int MinThreadsFree;
        protected int countStart;
        protected int countDone;

        Stack<JobThread> jobThreads;
        List<JobThread> runningThreads;

        protected class JobThread: IDisposable
        {
            public JobThread(ThreadPool threadPool)
            {
                thread = new Thread(Run);
                thread.IsBackground = true;
                IsRunning = false;
                this.threadPool = threadPool;
                isStoped = false;

                // run the job loop
                thread.Start();
            }

            public void Start(Job job, JobDone jobDone, object jobArgument)
            {
                this.job = job;
                this.jobDone = jobDone;
                this.jobArgument = jobArgument;

                lock (this)
                {
                    Monitor.Pulse(this);
                }
            }
            
            /// <summary>
            /// Call this method to destroy the object
            /// </summary>
            public void Dispose()
            {
                // remove myself from the list of created thread pools
                lock (this)
                {
                    isStoped = true;
                    Monitor.Pulse(this);
                }                
            }

            /// <summary>
            /// Wait for start signal
            /// do the job, notify ThreadPool that the job is done
            /// </summary>
            public void Run()
            {
                while (true)
                {
                    lock (this)
                    {
                        Monitor.Wait(this);
                        if (isStoped)
                        {
                            break;
                        }
                    }
                    
                    IsRunning = true;

                    // execute the job and notify the application
                    job(jobArgument);
                    jobDone(jobArgument);

                    IsRunning = false;
                    
                    // back to the ThreadPool
                    threadPool.JobDone(this);
                }
            }

            public bool IsRunning
            {
                get;
                protected set;
            }

            protected Job job;
            protected JobDone jobDone;
            protected object jobArgument;
            protected ThreadPool threadPool;
            protected Thread thread;
            protected bool isStoped;
        }

    }
}