
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace JQuant
{
	/// <summary>
	/// base class for all system loggers
	/// </summary>
	public abstract class Logger :IDisposable, ILogger
	{
        public Logger(string name)
        {
            _name = name;

            // add myself to the list of created mailboxes
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
            Console.WriteLine("Mailbox " + GetName() + " destroyed");
        }
		
        public string GetName()
		{
			return _name;
		}
		
        public int GetCount()
		{
			return _count;
		}
		
        public LogType GetLogType()
		{
			return _type;
		}
		
		public bool TimeStamped()
		{
			return _timeStamped;
		}
		
		public System.DateTime GetLatest()
		{
			return _stampLatest;
		}
		
		public System.DateTime GetOldest()
		{
			return _stampOldest;
		}
		

		protected string _name;
		protected LogType _type;
		protected int _count;
		protected bool _timeStamped;
		protected System.DateTime _stampLatest; 
		protected System.DateTime _stampOldest; 
	}
	
	/// <summary>
	/// write log to file
	/// base class for all loggers working with output streams
	/// the class implments asynchronous (postponned) writing and will be
	/// used when there large amounts of data to write or when the data should be
	/// processed before writing and the thread generating the data is 
	/// sensitive to the performance 
	/// </summary>
	public abstract class AsyncLogger :Logger
	{
		public AsyncLogger(string name): base(name)
		{
			notStoped = false;
			writer = new Thread(this.Writer);
			writer.Priority = ThreadPriority.Lowest;
		}
			
		/// <summary>
		/// start write file
		/// </summary>
		public virtual void Start()
		{
			// start Writer (writing thread)
			writer.Start();
		}

		/// <summary>
		/// application will call this method for clean up
		/// remove registration from the producer
		/// close the file, remove registration of the data sync from the producer
		/// </summary>
		public virtual void Stop()
		{
			notStoped = false;
            lock (this)
			{
				incomingData.Clear();
			}			
		}
		
		/// <summary>
		/// FMRShell collector calls this method to notify about incoming events
		/// push the evet into FIFO and let low priority background thread to write 
		/// the data into the file
		/// </summary>
		public override void AddEntry(object data)
		{
            lock (this)  // protect access to FIFO
			{
				// push the data to the FIFO
				incomingData.Enqueue(data);
				Monitor.Pulse(this);
			}
		}
		
		/// <summary>
		/// low priority thread writing the data to the file
		/// </summary>
		protected void Writer()
		{
			bool dataSet = false;
			object data = null;
			
			while (notStoped)
			{
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
						Monitor.Wait(this, 1*1000);
					}
				}
				if (dataSet) 
				{
					WriteData(data);
				}
			}
		}
		
		protected abstract void WriteData(object data);
		
		protected bool notStoped;
		
		/// <summary>
		/// Notify() pushes the incoming data here
		/// Thread FileWriter() pulls objects from the list and writes them to the file
		/// Under normal conditions the list is going to be empty most of the time
		/// </summary>
		protected Queue incomingData;
		protected Thread writer;
	}

	/// <summary>
	/// this class will get the data from specified data producer and write the data to the 
	/// specified file.
	/// </summary>
	public class MarketDataLoggerAscii :AsyncLogger, ISink<FMRShell.MarketData>
	{
		public MarketDataLoggerAscii(string name, string filename, 
		                             IProducer<FMRShell.MarketData> producer): base(name)
		{
			FileName = filename;
			_producer = producer;
			notStoped = false;
		}
			
		/// <summary>
		/// register notifier in the producer, start write file
		/// </summary>
		public override void Start()
		{
			// open file for writing 
			
			// register myself in the data producer
			_producer.AddSink(this);
			
			base.Start();			
		}

		/// <summary>
		/// application will call this method for clean up
		/// remove registration from the producer
		/// close the file, remove registration of the data sync from the producer
		/// </summary>
		public override void Stop()
		{
            lock (this)
			{
				_producer.RemoveSink(this);
				base.Stop();
			}			
		}
		
		/// <summary>
		/// FMRShell collector calls this method to notify about incoming events
		/// push the evet into FIFO and let low priority background thread to write 
		/// the data into the file
		/// </summary>
		public void Notify(int count, FMRShell.MarketData data)
		{
			base.AddEntry(data);
		}
		
		protected override void WriteData(object data)
		{
			// write data to the file
		}
		
		public string FileName
		{
			get;
			protected set;
		}
		
		protected IProducer<FMRShell.MarketData> _producer;
	}
	
}
