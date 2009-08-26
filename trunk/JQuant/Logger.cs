
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace JQuant
{
	/// <summary>
	/// base class for all system loggers
	/// </summary>
	public abstract class Logger<DataType> : IDisposable, ILogger
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
	/// </summary>
	public abstract class FileLogger<DataType> :Logger<DataType>
	{
		public FileLogger(string name, string filename): base(name)
		{
			FileName = filename;
			notStoped = false;
		}
			
		/// <summary>
		/// start write file
		/// </summary>
		public void Start()
		{
			// open file for writing 
		}

		/// <summary>
		/// application will call this method for clean up
		/// remove registration from the producer
		/// close the file, remove registration of the data sync from the producer
		/// </summary>
		public void Stop()
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
		public void Notify(object data)
		{
            lock (this)  // protect access to FIFO
			{
				// push the data to the FIFO
				incomingData.Enqueue(data);
			}
		}
		
		/// <summary>
		/// low priority thread writing the data to the file
		/// </summary>
		protected void FileWriter()
		{
			object data = null;
			while (notStoped)
			{
				bool dataSet = false;
				if (incomingData.Count == 0)
				{
					Thread.Sleep(1000);
				}
				lock (this)  // protect access to FIFO
				{
					if (incomingData.Count != 0)
					{
						data = incomingData.Dequeue();
						dataSet = true;
					}
				}
				if (dataSet) 
				{
					// TODO write data the file 
					Console.WriteLine(" "+data);
				}
			}
		}
		
		public string FileName
		{
			get;
			protected set;
		}
		
		protected bool notStoped;
		
		/// <summary>
		/// Notify() pushes the incoming data here
		/// Thread FileWriter() pulls objects from the list and writes them to the file
		/// Under normal conditions the list is going to be empty most of the time
		/// </summary>
		protected Queue incomingData;
	}

	/// <summary>
	/// this class will get the data from specified data producer and write the data to the 
	/// specified file.
	/// </summary>
	public class MarketDataLoggerAscii :Logger<FMRShell.MarketData>, ISink<FMRShell.MarketData>
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
		public void Start()
		{
			// open file for writing 
			_producer.AddSink(this);
		}

		/// <summary>
		/// application will call this method for clean up
		/// remove registration from the producer
		/// close the file, remove registration of the data sync from the producer
		/// </summary>
		public void Stop()
		{
			notStoped = false;
            lock (this)
			{
				_producer.RemoveSink(this);
				incomingData.Clear();
			}			
		}
		
		/// <summary>
		/// FMRShell collector calls this method to notify about incoming events
		/// push the evet into FIFO and let low priority background thread to write 
		/// the data into the file
		/// </summary>
		public void Notify(int count, FMRShell.MarketData data)
		{
			// producer can reuse the same object again and again
			// i have to clone it before pushing to the FIFO
			FMRShell.MarketData dataClone = (FMRShell.MarketData)data.Clone();
            lock (this)  // protect access to FIFO
			{
				// push the data to the FIFO
				incomingData.Enqueue(dataClone);
			}
		}
		
		/// <summary>
		/// low priority thread writing the data to the file
		/// </summary>
		protected void FileWriter()
		{
			FMRShell.MarketData data = new FMRShell.MarketData();
			while (notStoped)
			{
				bool dataSet = false;
				if (incomingData.Count == 0)
				{
					Thread.Sleep(1000);
				}
				lock (this)  // protect access to FIFO
				{
					if (incomingData.Count != 0)
					{
						data = (FMRShell.MarketData)incomingData.Dequeue();
						dataSet = true;
					}
				}
				if (dataSet) 
				{
					// TODO write data the file 
					Console.WriteLine(" "+data);
				}
			}
		}
		
		/// <summary>
		/// Notify() pushes the incoming data here
		/// Thread FileWriter() pulls objects from the list and writes them to the file
		/// Under normal conditions the list is going to be empty most of the time
		/// </summary>
		protected Queue incomingData;
		
		public string FileName
		{
			get;
			protected set;
		}
		
		protected IProducer<FMRShell.MarketData> _producer;
		protected bool notStoped;

	}
	
}
