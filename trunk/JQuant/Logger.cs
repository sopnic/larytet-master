
using System;

namespace JQuant
{
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
	
	public class Logger<DataType> :ISink<DataType>, ILogger, IDisposable
	{
	}
	
}
