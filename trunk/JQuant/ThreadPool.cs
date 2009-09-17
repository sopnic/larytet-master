
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
            : this(name, size, System.Threading.ThreadPriority.Lowest)
        {
        }

        /// <summary>
        /// Create a new thread pool. number of threads in the pool can be smaller
        /// than maximum number of simultaneously placed job. Burst of new jobs can be
        /// served by small number of threads
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="threads">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="jobs">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="priority">
        /// A <see cref="Thread.Priority"/>
        /// </param>
        public ThreadPool(string name, int threads, System.Threading.ThreadPriority priority)
        {
            this.Name = name;
            this.Threads = threads;
            this.Jobs = threads;
            MinThreadsFree = threads;
            countStart = 0;
            countDone = 0;
            countMaxJobs = 0;
            
            jobThreads = new Stack<JobThread>(threads);
            runningThreads = new List<JobThread>(threads);
            for (int i = 0;i < threads;i++)
            {
                JobThread jobThread = new JobThread(this, priority);
                jobThreads.Push(jobThread);
            }

            pendingJobs = new Queue<JobParams>(this.Jobs);
            freeJobs = new Stack<JobParams>(this.Jobs);
            for (int i = 0;i < this.Jobs;i++)
            {
                JobParams jobParams = new JobParams();
                freeJobs.Push(jobParams);
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
        public bool PlaceJob(Job job, JobDone jobDone, object jobArgument)
        {
            bool result = false;
            JobThread jobThread = default(JobThread);
            JobParams jobParams = default(JobParams);
            
            do
            {
                // allocate job params blocks
                lock (freeJobs)
                {
                    if (freeJobs.Count > 0)
                    {
                        jobParams = freeJobs.Pop();
                        jobParams.Init(job, jobDone, jobArgument);
                        pendingJobs.Enqueue(jobParams);
                        if (countMaxJobs < pendingJobs.Count)
                        {
                            countMaxJobs = pendingJobs.Count;
                        }
                        result = true;
                    }
                    else // no room for a new job - get out
                    {
                        System.Console.WriteLine("Failed to place a job");
                        break;
                    }                        
                }
                
                // just to be sure that there is a thread to serve the new 
                // job allocate a free thread (if there is any)
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

                jobThread.Start();

                    
                result = true;
            }
            while (false);
            
            return result;
        }

        protected void JobDone(JobThread jobThread, JobParams jobParams)
        {
            lock (jobThreads)
            {
                countDone++;
                jobThreads.Push(jobThread);
                runningThreads.Remove(jobThread);
            }

            lock (freeJobs)
            {
                jobParams.Init();
                freeJobs.Push(jobParams);
            }
        }

        public int GetThreads()
        {
            return Threads;
        }
        
        public int GetJobs()
        {
            return Jobs;
        }
        
        public string GetName()
        {
            return Name;
        }
        
        public int GetMaxCount()
        {
            return (Threads-MinThreadsFree);
        }
        
        public int GetCountStart()
        {
            return countStart;
        }
        
        public int GetCountDone()
        {
            return countDone;
        }
        
        public int GetCountMaxJobs()
        {
            return countMaxJobs;
        }
        
        protected string Name;
        protected int Threads;
        protected int Jobs;
        protected int MinThreadsFree;
        protected int countMaxJobs;
        protected int countStart;
        protected int countDone;

        Stack<JobThread> jobThreads;
        List<JobThread> runningThreads;
        Queue<JobParams> pendingJobs;
        Stack<JobParams> freeJobs;

        protected class JobParams
        {
            public JobParams()
            {
                Init();
            }
            
            public void Init(Job job, JobDone jobDone, object jobArgument)
            {
                this.job = job;
                this.jobDone = jobDone;
                this.jobArgument = jobArgument;
            }
            
            public void Init()
            {
                this.job = null;
                this.jobDone = null;
                this.jobArgument = null;
            }
            
            public Job job;
            public JobDone jobDone;
            public object jobArgument;
        }
        
        protected class JobThread: IDisposable
        {
            public JobThread(ThreadPool threadPool, System.Threading.ThreadPriority priority)
            {
                thread = new Thread(Run);
                thread.IsBackground = true;
                thread.Priority = priority;
                IsRunning = false;
                this.threadPool = threadPool;
                isStoped = false;

                // run the job loop
                thread.Start();
            }

            public void Start()
            {
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

                    // try to serve all pending jobs
                    do
                    {
                        JobParams jobParams = default(JobParams);
                        lock (threadPool.pendingJobs)
                        {
                            if (threadPool.pendingJobs.Count > 0)
                            {
                                jobParams = threadPool.pendingJobs.Dequeue();
                            }
                        }
    
                        if (jobParams != default(JobParams))
                        {
                            ServeJob(jobParams);
                        }
                        else // no more pending jobs in the queue
                        {
                            break;
                        }
                    }
                    while (true);
                }
            }

            protected void ServeJob(JobParams jobParams)
            {
                IsRunning = true;

                object jobArgument = jobParams.jobArgument;
                
                // execute the job and notify the application
                jobParams.job(jobArgument);
                jobParams.jobDone(jobArgument);

                IsRunning = false;
                
                // back to the ThreadPool
                threadPool.JobDone(this, jobParams);
            }

            public bool IsRunning
            {
                get;
                protected set;
            }

            protected ThreadPool threadPool;
            protected Thread thread;
            protected bool isStoped;
        }

    }
}
