
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

namespace JQuant
{
    /// <summary>
    /// objects implementing Mailbox
    /// </summary>
    public interface IMailbox
    {
        /// <summary>
        /// return name of the mailbox
        /// </summary>
        string GetName();

        /// <summary>
        /// number of messages in the queue
        /// </summary>
        int GetCount();

        /// <summary>
        /// maximum size recorded
        /// </summary>
        int GetMaxCount();

        /// <summary>
        /// mailbox capacity
        /// </summary>

        int GetCapacity();
        int GetSent();
        int GetDropped();
        int GetReceived();
        int GetTimeouts();
    }


    public enum ThreadState
    {
        [Description("Initialized")]
        Initialized,
        [Description("Satrted")]
        Started,
        [Description("Stoped")]
        Stoped,
        [Description("Destroyed")]
        Destroyed

    };

    /// <summary>
    /// objects implementing MailboxThread
    /// </summary>
    public interface IThread
    {
        ThreadState GetState();
        string GetName();
    }

    /// <summary>
    /// objects implementing Pool interface
    /// </summary>
    public interface IPool
    {
        string GetName();
        int GetCapacity();
        int GetCount();
        int GetMinCount();
        int GetAllocOk();
        int GetAllocFailed();
        int GetFreeOk();
    }

    public enum LogType
    {
        [Description("Dynamic memory")]
        RAM,
        [Description("Serialization")]
        BinarySerialization,
        [Description("Binary")]
        Binary,
        [Description("ASCII")]
        Ascii,
        [Description("HTML")]
        HTML,
        [Description("XML")]
        XML,
        [Description("SQL")]
        SQL
    };


    /// <summary>
    /// System logs will register in the central data base
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// returns name of the logger 
        /// </summary>
        string GetName();

        /// <summary>
        ///returns number of records 
        /// </summary>
        /// <returns>
        /// A <see cref="System.Int32"/>
        /// number of records in the log
        /// </returns>
        int GetCountLog();

        /// <summary>
        /// number of times Logger was invoke
        /// normallu equal or close to what GetCountLog() returns
        /// difference between two is number of pending entries (wait in queue
        /// for processing)
        /// </summary>
        /// <returns>
        /// A <see cref="System.Int32"/>
        /// </returns>
        int GetCountTrigger();

        int GetCountDropped();

        LogType GetLogType();

        /// <summary>
        /// returns true if the log entries are time stamped by the producer. 
        /// Logs can and will time stamp the entries. So in some cases there are going to be two
        /// time stamps - by the producer and by the logger.
        /// </summary>
        bool TimeStamped();

        /// <summary>
        /// returns time of the most recent log entry
        /// </summary>
        System.DateTime GetLatest();

        /// <summary>
        /// returns time of the oldest log entry
        /// </summary>
        System.DateTime GetOldest();
    }


    public interface IDataGenerator
    {
        int GetCount();
        string GetName();
    }

    public interface IResourceTimerList
    {
        int GetPendingTimers();
        int GetCountStart();
        int GetCountStop();
        int GetCountExpired();
        int GetCountStartAttempt();
        int GetCountStopAttempt();
        int GetSize();
        int GetMaxCount();
        string GetName();
        string GetTaskName();
    }

    public interface IResourceStatistics
    {
        /// <summary>
        /// returns array of debug counters
        /// </summary>
        void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values);
    }
    
    public interface INamedResource
    {
        /// <summary>
        /// returns name of the resource
        /// </summary>
        string GetName();
    }
    
    public interface IResourceThreadPool :IResourceStatistics, INamedResource
    {
    }
    
    /// <summary>
    /// a storage of all created objects
    /// an object central
    /// this guy is singleton - application calls Init() only once to initialize the class
    /// </summary>
    public class Resources
    {
        /// <summary>
        /// call Init() to create a single instance of this class 
        /// </summary>
        protected Resources()
        {
            Mailboxes = new List<IMailbox>(10);
            Threads = new List<IThread>(10);
            Pools = new List<IPool>(10);
            Loggers = new List<ILogger>(10);
            DataGenerators = new List<IDataGenerator>(10);
            TimerLists = new  List<IResourceTimerList>(5);
            ThreadPools = new List<IResourceThreadPool>(2);
        }

        static public void Init()
        {
            if (r == null)
            {
                r = new Resources();
            }
            else
            {
                Console.WriteLine("Class Resources is a singletone. Resources.Init() can be called only once");
            }
        }

        /// <summary>
        /// created in the system mailboxes
        /// </summary>
        public static List<IMailbox> Mailboxes;

        /// <summary>
        /// i expect that creation of threads is not an often operation
        /// if this is not the case one can construct threads which do not register at all
        /// or a thread pool where all threads are registered after boot.  
        /// Adding a thread will not take too much time, but thread deletion will 
        /// consume some CPU cycles.
        /// </summary>
        public static List<IThread> Threads;

        public static List<IPool> Pools;

        public static List<ILogger> Loggers;

        public static List<IDataGenerator> DataGenerators;

        public static List<IResourceTimerList> TimerLists;

        public static List<IResourceThreadPool> ThreadPools;

        static protected Resources r;
    }
}
