
using System;

namespace JQuant
{
	/// <summary>
	/// sink is a data consumer. the interface assumes asynchronous processing
	/// usually Notify() will send add the data object to the internal queue for 
	/// the future processing and get out
	/// </summary>
    public interface ISink<DataType>
	{
		/// <summary>
		/// Producer calls the method to notfy consumer that there is new data available
		/// Notify() should be non-blocking - there are more than one sink to be served 
		/// by the producer
		/// </summary>
		/// <param name="count">
		/// A <see cref="System.Int32"/>
		/// Number of objects available
		/// </param>
		void Notify(int count, DataType data);
	}
	
    public interface IProducer<DataType>
    {
        bool AddSink(ISink<DataType> sink);
        bool RemoveSink(ISink<DataType> sink);
    }
	
	/// <summary>
	/// base class for all system logs
	/// </summary>
	public abstract class Logger<DataType> :ISink<DataType>, ILogger, IDisposable
	{
        public Logger(string name)
        {
            _name = name;

            // add myself to the list of created mailboxes
            Resources.Loggers.Add(this);
        }

        public void Dispose()
        {
            // remove myself from the list of created mailboxes
            Resources.Loggers.Remove(this);
        }

        ~Logger()
        {
            Console.WriteLine("Mailbox " + GetName() + " destroyed");
        }
		
		public abstract void Notify(int count, DataType data);
		
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
}
