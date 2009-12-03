
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;



#if USEFMRSIM
using TaskBarLibSim;
#else
using TaskBarLib;
#endif

namespace JQuant
{
    /// <summary>
    /// base class for all system loggers
    /// </summary>
    public abstract class Logger : IDisposable, ILogger
    {
        public Logger(string name)
        {
            this.name = name;
            countTrigger = 0;
            countLog = 0;
            countDropped = 0;

            // add myself to the list of created loggers
            Resources.Loggers.Add(this);
        }

        /// <summary>
        /// application will call this method for proper cleanup
        /// </summary>
        public virtual void Dispose()
        {
            // remove myself from the list of created loggers
            Resources.Loggers.Remove(this);
        }

        public abstract void AddEntry(object o);

        ~Logger()
        {
            Console.WriteLine("Logger " + GetName() + " destroyed");
        }

        public string GetName()
        {
            return name;
        }

        public int GetCountLog()
        {
            return countLog;
        }

        public int GetCountTrigger()
        {
            return countTrigger;
        }

        public int GetCountDropped()
        {
            return countDropped;
        }

        public LogType GetLogType()
        {
            return Type;
        }

        public bool TimeStamped()
        {
            return timeStamped;
        }

        public System.DateTime GetLatest()
        {
            return stampLatest;
        }

        public System.DateTime GetOldest()
        {
            return stampOldest;
        }

        public LogType Type
        {
            get;
            set;
        }

        protected string name;
        protected int countTrigger;
        protected int countLog;
        protected int countDropped;
        protected bool timeStamped;
        protected System.DateTime stampLatest;
        protected System.DateTime stampOldest;
    }

    /// <summary>
    /// write log to file
    /// base class for all loggers working with output streams
    /// the class implments asynchronous (postponned) writing and will be
    /// used when there large amounts of data to write or when the data should be
    /// processed before writing and the thread generating the data is 
    /// sensitive to the performance
    ///    
    /// Use property Priority to set the task priority. Default priority is 
    /// ThreadPriority.Lowest
    /// 
    /// </summary>
    public abstract class AsyncLogger : Logger
    {
        public AsyncLogger(string name)
            : base(name)
        {
            notStoped = false;
            writer = new Thread(this.Writer);
            writer.Priority = ThreadPriority.Lowest;
            incomingData = new Queue(100);
        }

        /// <summary>
        /// start write file
        /// </summary>
        public virtual bool Start()
        {
            notStoped = true;

            // start Writer (writing thread)
            writer.Start();

            return true;
        }

        /// <summary>
        /// application will call this method for clean up
        /// remove registration from the producer
        /// close the file, remove registration of the data sync from the producer
        /// </summary>
        public virtual void Stop()
        {
            notStoped = false;
            stopped = false;

            lock (this)
            {
                // let the writer thread know that it is time to get out
                Monitor.Pulse(this);

                // help the garbage collector to clean up
                incomingData.Clear();
            }

            // wait for the write thread exit
            while (!stopped)
            {
                lock (this)
                {
                    Monitor.Pulse(this);
                }
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Application calls this method to add entry to the log
        /// This method is non-blocking - add entry to the queue for 
        /// further processing and get out
        /// </summary>
        public override void AddEntry(object data)
        {
            lock (this)  // protect access to FIFO
            {
                countTrigger++;
                if (incomingData.Count < QueueSize)
                {
                    // push the data to the FIFO
                    incomingData.Enqueue(data);
                }
                else
                {
                    countDropped++;
                }
                Monitor.Pulse(this);
            }
        }

        public ThreadPriority Priority
        {
            set
            {
                writer.Priority = value;
            }

            get
            {
                return writer.Priority;
            }
        }

        /// <value>
        /// Setting this property will limit size of the queue of events
        /// waiting for processing
        /// there is some reasonable default (0.5MB of entries)
        /// </value>
        public int QueueSize
        {
            get;
            set;
        }



        /// <summary>
        /// low priority thread writing the data to the file
        /// </summary>
        protected void Writer()
        {
            object data = null;

            while (notStoped)
            {
                bool dataSet = false;
                lock (this)
                {
                    if (incomingData.Count != 0)
                    {
                        data = incomingData.Dequeue();
                        dataSet = true;
                    }
                    else
                    {
                        // i want to check the notStoped flag from time to time
                        // and make sure that incomingData is empty
                        Monitor.Wait(this, 1 * 1000);
                    }
                }
                if (dataSet)
                {
                    WriteData(data);
                }
            }

            stopped = true;
            Console.WriteLine("Logger " + GetName() + " thread stopped");
        }

        protected abstract void WriteData(object data);

        protected bool notStoped;
        protected bool stopped;

        /// <summary>
        /// Notify() pushes the incoming data here
        /// Thread FileWriter() pulls objects from the list and writes them to the file
        /// Under normal conditions the list is going to be empty most of the time
        /// </summary>
        protected Queue incomingData;
        protected Thread writer;
    }

    /// <summary>
    /// This guy is called once a day in order to keep weights of securities in 
    /// a csv file. 
    /// </summary>
    public class SH161DataLogger
    {
        public SH161DataLogger(string fileName)
        {
            //No append - run it once a day
            _fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            _streamWriter = new StreamWriter(_fileStream);
            _sh161DataToString = new FMRShell.SH161TypeToString(",");
        }

        public void GetAndLogSH161Data(int _sessionId)
        {
            Array x = null;
            K300Class k300Class = new K300Class();
            k300Class.K300SessionId = _sessionId;

            int rc = k300Class.GetSH161(ref x, MadadTypes.TLV25);
            if (rc > 0)
            {
                _streamWriter.WriteLine(_sh161DataToString.Legend);
                for (int i = 0; i < rc; i++)
                {
                    _sh161DataToString.Init((SH161Type)x.GetValue(i));
                    _streamWriter.WriteLine(_sh161DataToString.Values);
                }
                Console.WriteLine(rc + " SH161 records collected");    
            }

            _streamWriter.Close();
            _fileStream.Close();
            _streamWriter.Dispose();
            _fileStream.Dispose();
        }

        FileStream _fileStream;
        StreamWriter _streamWriter;
        FMRShell.SH161TypeToString _sh161DataToString;
    }//SH161DataLogger

    
}//namespace
