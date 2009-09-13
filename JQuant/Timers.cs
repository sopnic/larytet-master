
using System;
using System.Threading;
using System.Timers;
using System.Collections.Generic;

/// <summary>
/// The idea is taken from http://larytet.sourceforge.net/aos.shtml  (AOS Timer)
/// In the CSharp there is System.Threading.Timer. The service comes at cost - a separate thread
/// for every timer. This is proprietary implemenation for the application timers
///   Terminology:
///   Timer list - queue of the running timers with the SAME timeout. For example list of 1s timers
///   Set - one or more lists of timers and a task handling the lists.
///       For example set A containing 1s timers, 2s timers and 5s timers
///       and set B containing 100 ms timers, 50 ms timers and 200 ms timers
///   Timer task - task that handles one and only one set of lists of timers
/// 
///                      -----------   Design   ---------------
/// 
///   In the system run one or more timer tasks handling different timer sets. Every timer
///   set contains one or more timer lists.
///   Function start timer allocates free entry from the stack of free timers and places
///   the timer in the end of the timer list (FIFO). Time ~O(1)
///   Timer task waits for the expiration of the nearest timer, calls application handler
///   TimerExpired, find the next timer to be expired using sequential search in the
///   set (always the first entry in a timer list). Time ~ O(size of set)
///   Function stop timer marks a running timer as stopped. Time ~O(1)
/// 
///                      -----------   Reasons  ---------------
/// 
///   1. It is possible that every subsystem will have own timer tasks running in
///      different priorities
///   2. Set of long timers and set of short timers can be created and handled by tasks with
///      different priorities
///   3. "Timer expired"  application handlers can be called from different tasks. For high
///      priority short timers such handler should be short - release semaphore for example,
///      for low priority long timers handler can make long processing like audit in data-base
///   4. In the system can coexist 1 or 2 short timers - 50 ms - used in call process
///      and 10 long timers  - 10 s, 1 min, 1 h, etc. - used in the application
///      sanity checking or management
///   5. In the system can coexist short - 10 ms - timer that always expired and 10 long
///      protocol timers that ususally stopped by the application before expiration
///                      -----------   Usage examples  ---------------
/// </summary>
namespace JQuant
{

    public delegate void TimerExpiredCallback(object o);
    
    public class Timer
    {
        /// <summary>
        /// this is expiration tick
        /// </summary>
        long ExpirationTime;

        /// <summary>
        /// system tick when the timer was started
        /// </summary>
        long StartTick;
    }

    /// <summary>
    /// lits of timers - keep all timers with the same timeout
    /// </summary>
    public class TimerList : List<Timer>, ITimerList
    {
        /// <summary>
        /// Create a timer list
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String"/>
        /// Name of the timer list
        /// </param>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// Maximum number of pending timers
        /// </param>
        /// <param name="timerCallback">
        /// A <see cref="TimerExpiredCallback"/>
        /// This method will be called for all expired timers
        /// There is no a callback per timer. Only a callback per timer list
        /// </param>
        public TimerList(string name, int size, TimerExpiredCallback timerCallback) :base(size)
        {
            // add myself to the list of created timer lists
            Resources.TimerLists.Add(this);

            this.name = name;
            this.timerCallback = timerCallback;

            // create pool of free timers
            InitTimers(size);
        }

        protected void Dispose()
        {
            // remove myself from the list of created timer lists
            Resources.TimerLists.Remove(this);
        }

        /// <summary>
        /// Allocate a free timer from the stack of free timers, set expiration
        /// add the timer to end of the list of running timers
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="System.Int64"/>
        /// Reference to the started timer. Can be used in call to Stop()
        /// </param>
        /// <param name="timer">
        /// A <see cref="Timer"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// returns true of Ok, false is failed to allocate a new timer - no free
        /// timers are available
        /// </returns>
        public bool StartTimer(long timeout, out Timer timer, out long timerId)
        {
            timer = null;
            timerId = 0;
            
            return true;
        }

        /// <summary>
        /// use this method if no need to call Stop() will ever arise for the timer
        /// </summary>
        public bool StartTimer(long timeout)
        {
            Timer timer;
            long timerId;
            
            bool result = StartTimer(timeout, out timer, out timerId);
            
            return result;
        }

        /// <summary>
        /// stop previously started timer
        /// </summary>
        /// <param name="timer">
        /// A <see cref="Timer"/>
        /// </param>
        /// <param name="timerId">
        /// A <see cref="System.Int64"/>
        /// Value returned by StartTimer()
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// Returns true if the timer was running and now stopped
        /// Call to this method for already stoped timer will return false
        /// and error message will be printed on the console
        /// </returns>
        public bool StopTimer(Timer timer, long timerId)
        {
            return true;
        }

        /// <summary>
        /// free timers are stored in the stack
        /// </summary>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// </param>
        protected void InitTimers(int size)
        {
            freeTimers = new Stack<Timer>(size);
            for (int i = 0; i < size; i++)
            {
                Timer timer = new Timer();
                freeTimers.Push(timer);
            }
        }

        ~TimerList()
        {
            Console.WriteLine("TimerList " + name + " destroyed");
        }
        
        public int GetPendingTimers()
        {
            return 0;
        }
        
        public int GetCountStart()
        {
            return 0;
        }
        
        public int GetCountStop()
        {
            return 0;
        }
        
        public int GetCountExpired()
        {
            return 0;
        }
        
        /// <summary>
        /// stack of free timers
        /// </summary>
        protected Stack<Timer> freeTimers;

        
        protected string name;

        protected TimerExpiredCallback timerCallback;
    }
    
    /// <summary>
    /// Timer task (timer set) implementation
    /// </summary>
    public class TimerTask
    {
        
        public TimerTask(string name)
        {
            this.name = name;
            this.isAlive = false;
            
        }

        protected void Dispose()
        {
            thread.Abort();
            thread = null;
        }


        /// <summary>
        /// call this method to start the thread - enters loop forever in the 
        /// method Run()
        /// </summary>
        public void Start()
        {
            this.thread = new Thread(this.Run);
            this.thread.Start();
        }

        public void Stop()
        {
            isAlive = false;
            Monitor.Pulse(this);
        }
        
        
        ~TimerTask()
        {
            Console.WriteLine("TimerTask " + name + " destroyed");
        }


        protected void Run()
        {
            isAlive = true;

            // Check all timer lists, find minimum delay (timeout before the nearest timer expires)
            // call Thread.Sleep()
            while (isAlive)
            {
            }
        }
        

        protected Thread thread;
        protected bool isAlive;
        protected string name;

    }
}
