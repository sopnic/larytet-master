
using System;
using System.IO;
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
            _countTrigger = 0;
            _countLog = 0;
            _countDropped = 0;

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
			return _name;
		}
		
        public int GetCountLog()
		{
			return _countLog;
		}
        
        public int GetCountTrigger()
        {
            return _countTrigger;
        }
        
        public int GetCountDropped()
        {
            return _countDropped;
        }
        
        public LogType GetLogType()
        {
            return Type;
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
		
        public LogType Type
        {
            get;
            set;
        }

		protected string _name;
        protected int _countTrigger;
        protected int _countLog;
        protected int _countDropped;
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
    ///    
    /// Use property Priority to set the task priority. Default priority is 
    /// ThreadPriority.Lowest
    /// 
	/// </summary>
	public abstract class AsyncLogger :Logger
	{
		public AsyncLogger(string name): base(name)
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
                _countTrigger++;
                if (incomingData.Count < QueueSize)
                {                    
    				// push the data to the FIFO
    				incomingData.Enqueue(data);
                }
                else
                {
                    _countDropped++;
                }
                Monitor.Pulse(this);
			}
		}

        public ThreadPriority Priority
        {
            set {
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
						Monitor.Wait(this, 1*1000);
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
	/// this class will get the data from specified data producer and write the data to the 
	/// specified file.
	/// </summary>
	public class TradingDataLogger :AsyncLogger/*, ISink<FMRShell.MarketDataMaof>*/
	{
        public class MaofSink:ISink<FMRShell.MarketDataMaof>
        {
            public MaofSink(TradingDataLogger TrDtaLogger)
            {
                maofDataToString = new FMRShell.K300MaofTypeToString("\t");
                tdl = TrDtaLogger;
                tdl._collector.maofProducer.AddSink(this);
            }

            public void Stop()
            {
                tdl._collector.maofProducer.RemoveSink(this);
            }

            public void Notify(int count, FMRShell.MarketDataMaof data)
            {
                tdl._stampLatest = System.DateTime.Now;
                FMRShell.MarketDataMaof dataClone = (FMRShell.MarketDataMaof)(data.Clone());
                tdl.AddEntry(dataClone);
            }

            public FMRShell.K300MaofTypeToString maofDataToString;
            protected TradingDataLogger tdl;    //a pointer to the container class
        }

        public class RezefSink : ISink<FMRShell.MarketDataRezef>
        {
            public RezefSink(TradingDataLogger TrDtaLogger)
            {
                rezefDataToString = new FMRShell.K300RzfTypeToString("\t");
                tdl = TrDtaLogger;
                tdl._collector.rezefProducer.AddSink(this);
            }

            public void Stop()
            {
                tdl._collector.rezefProducer.RemoveSink(this);
            }

            public void Notify(int count, FMRShell.MarketDataRezef data)
            {
                tdl._stampLatest = System.DateTime.Now;
                FMRShell.MarketDataRezef dataClone = (FMRShell.MarketDataRezef)(data.Clone());
                tdl.AddEntry(dataClone);
            }

            public FMRShell.K300RzfTypeToString rezefDataToString;
            protected TradingDataLogger tdl;    //a pointer to the container class
        }

        /// <summary>
        /// Create the ASCII logger
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String"/>
        /// Debuh info - name of the logger
        /// </param>
        /// <param name="filename">
        /// A <see cref="System.String"/>
        /// File to read the data
        /// </param>
        /// <param name="append">
        /// A <see cref="System.Boolean"/>
        /// If "append" is true and file exists logger will append the data to the end of the file
        /// </param>
        /// <param name="producer">
        /// A <see cref="IProducer"/>
        /// Object which provides data to log
        /// </param>
		public TradingDataLogger(string name, string filename, bool append, FMRShell.Collector collector, FMRShell.DataType dt): base(name)
		{
			FileName = filename;
            _fileStream = default(FileStream);
            _streamWriter = default(StreamWriter);
            _collector = collector;
            _append = append;
            _timeStamped = false;
            _stampLatest = default(System.DateTime);
            _stampOldest = default(System.DateTime);
            _dt = dt;
            Type = LogType.Ascii;
			notStoped = false;
            
            
            
            // I estimate size of FMRShell.MarketData struct 50 bytes
            QueueSize = (500*1024)/50; 
		}
			
		/// <summary>
		/// register notifier in the producer, start write file
        /// returns True if Ok
        /// application will check LastException if the method
        /// returns False
		/// </summary>
		public override bool Start()
		{
            bool result = false;
            
            // i want a loop here to break from  - i avoid multiple
            // returns this way
            do
            {
                // open file for writing
                try
                {
                    if (_append)
                    {
                        _fileStream = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                        _streamWriter = new StreamWriter(_fileStream);
                    }
                    else
                    {
                        _fileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                        _streamWriter = new StreamWriter(_fileStream);
                    }
                }
                catch (IOException e)
                {
                    // store the exception
                    LastException = e;
                    if (_fileStream != default(FileStream))
                    {
                        _fileStream.Close();
                        // help Garbage collector
                        _streamWriter = default(StreamWriter);
                        _fileStream = default(FileStream);
                    }
                    // and get out
                    break;
                }

                // write legend at the top of the file
                /*try
                {
                    if (_dt == FMRShell.DataType.Maof) _streamWriter.WriteLine(this._maofSink.maofDataToString.Legend);
                    else if (_dt == FMRShell.DataType.Rezef) _streamWriter.WriteLine(this._rezefSink.rezefDataToString.Legend);
                }
                catch (IOException e)
                {
                    // store the exception
                    LastException = e;
                    // close the file
                    _fileStream.Close();
                    // help Garbage collector
                    _streamWriter = default(StreamWriter);
                    _fileStream = default(FileStream);
                    Console.WriteLine(e.ToString());
                    // and get out
                    break;
                }*/
             

                // register myself in the data producer
                if (this._dt == FMRShell.DataType.Maof) this._maofSink = new MaofSink(this);
                else if (this._dt == FMRShell.DataType.Rezef) this._rezefSink = new RezefSink(this);
                
                base.Start();       

                result = true;
            }
            while (false);

            return result;
		}

		/// <summary>
		/// application will call this method for clean up
		/// remove registration from the producer
		/// close the file, remove registration of the data sync from the producer
		/// </summary>
		public override void Stop()
		{
            base.Stop();
            if (_dt == FMRShell.DataType.Maof) _maofSink.Stop();
            else if (_dt == FMRShell.DataType.Rezef) _rezefSink.Stop();

            if (_fileStream != default(FileStream))
            {
                _fileStream.Close();
                // help Garbage collector
                _streamWriter = default(StreamWriter);
                _fileStream = default(FileStream);
            }
		}

        public override void Dispose()
        {
            if (_fileStream != default(FileStream))
            {
                _fileStream.Close();
            }
            base.Dispose();
        }
		
		/// <summary>
		/// FMRShell collector calls this method to notify about incoming events
		/// push the evet into FIFO and let low priority background thread to write 
		/// the data into the file
		/// </summary>
		/*public void Notify(int count, FMRShell.MarketDataMaof data)
		{
            _stampLatest = System.DateTime.Now;
            
            // i have to clone the data first. Producer can use the same
            // reference again and again
            FMRShell.MarketDataMaof dataClone = (FMRShell.MarketDataMaof)(data.Clone());
            // add the object to the queue for further processing
			base.AddEntry(data);
		}*/


        /*public void AddEntry(object data)
        {
            base.AddEntry(data);
        }*/

        /// <summary>
        /// write data to the file. this method is called from a separate
        /// thread
        /// </summary>
        /// <param name="data">
        /// A <see cref="System.Object"/>
        /// </param>
		protected override void WriteData(object data)
		{
			// I have to decide on format of the log - ASCII or binary 
            // should I write any system info like version the data/software ?
            // at this point only ASCII is supported, no system info
            // write all fields of K300MaofType (data.k3Maof) in one line
            // followed by EOL

            // convert the data to string - this is time consuming
            // operation
            if (_dt == FMRShell.DataType.Maof) _maofSink.maofDataToString.Init(((FMRShell.MarketDataMaof)data).k300MaofType);
            else if (_dt == FMRShell.DataType.Rezef) _rezefSink.rezefDataToString.Init(((FMRShell.MarketDataRezef)data).k300RzfType);
            // write the string to the file
            try
            {
                if (_dt == FMRShell.DataType.Maof) _streamWriter.WriteLine(_maofSink.maofDataToString.Values);
                else if (_dt == FMRShell.DataType.Rezef) _streamWriter.WriteLine(_rezefSink.rezefDataToString.Values);
                // i want to make Flush from time to time
                // the question is when ? or let the OS to manage the things ?
                // _streamWriter.Flush();
                lock (this)
                {
                    _countLog++;
                }
            }
            catch (ObjectDisposedException e)
            {
                // store the exception
                LastException = e;
                Console.WriteLine(e.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                // store the exception
                LastException = e;
                // and get out
                Stop();
            }
		}
		
		public string FileName
		{
			get;
			protected set;
		}
        
        public Exception LastException
        {
            get;
            protected set;
        }
		
		protected FMRShell.Collector _collector;    //producer
        protected MaofSink _maofSink;               //and two
        protected RezefSink _rezefSink;             //sinks

        bool _append;
        FileStream _fileStream;
        FMRShell.DataType _dt;
        StreamWriter _streamWriter;
	}
}