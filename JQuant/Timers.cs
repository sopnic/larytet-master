
using System;

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
    /// <summary>
    /// Timer task (or timer set) implementation
    /// </summary>
    public class TimerTask : ITimerTask
    {
        
        public TimerTask()
        {
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
        
        
    }
}
