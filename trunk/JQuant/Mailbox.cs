
using System;
using System.Threading;
using System.Collections.Generic;

namespace JQuant
{	

	/// <summary>
	/// this guy is a simple Queue base implemenation of mailbox - queue of messages with
	/// send and receive methods
	/// </summary>
	public class Mailbox<Message> :Queue<Message>, IMailbox, IDisposable
	{
		public Mailbox(string name, int capacity) : base(capacity)
		{
			_name = name;
			_capacity = capacity;
			_maxCount = 0;

			_dropped = 0;
			_sent = 0;
			_received = 0;
			_timeouts = 0;

			// add myself to the list of created mailboxes
			Resources.Mailboxes.Add(this);
		}
		
		public void Dispose()
		{
			Clear();
			
			// remove myself from the list of created mailboxes
			Resources.Mailboxes.Remove(this);			
		}
		
		~Mailbox() 
		{
			Console.WriteLine("Mailbox "+GetName()+" destroyed");
		}
		
		/// <summary>
		/// add a message to the queue
		/// </summary>
		/// <param name="message">
		/// A <see cref="Message"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// returns True is there is enough place in the message queue
		/// </returns>
		public bool Send(Message message)
		{
			bool result = false;
			
			lock(this) {
				if (Count < _capacity) {
					Monitor.Pulse(this);
					Enqueue(message);
					result = true;
					_sent++;
				}
				else {
					Monitor.Pulse(this);
					_dropped++;
				}
			}
			if (_maxCount < Count) _maxCount = Count;
			
			return result;
		}

		/// <summary>
		/// Send pulse and unblock the thread waiting for the message
		/// 
		/// </summary>
		public void Pulse()
		{
			lock (this) {
				Monitor.Pulse(this);
			}
		}
		
		/// <summary>
		/// blocking call - wait for a message 
		/// </summary>
		/// <param name="message">
		/// A <see cref="Message"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// True - if a new message arrived
		/// </returns>
		public bool Receive(out Message message)
		{
			bool result = false;
			
			lock(this) {
				if (Count == 0) Monitor.Wait(this);
				if (Count != 0) {
					_received++;
					message = Dequeue();
					result = true;
				}
			}
			
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message">
		/// A <see cref="Message"/>
		/// </param>
		/// <param name="timeout">
		/// A <see cref="System.Int32"/>
		/// Block the calling thread until a new message or timeout expires 
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// True - if a new message arrived
		/// </returns>
		public bool Receive(Message message, int timeout)
		{
			bool result = false;
			
			lock(this) {
				while (Count == 0) result = Monitor.Wait(this, timeout);
				if (result) {
					_received++;
					message = Dequeue();
					result = true;
				}
				else {
					_timeouts++;
				}
			}
			
			return result;
		}
		
		public string GetName()
		{
			return _name;
		}
		
		public int GetCount()
		{
			return Count;
		}

		public int GetMaxCount()
		{
			return _maxCount;
		}

		public int GetCapacity()
		{
			return _capacity;
		}
		
		public int GetDropped()
		{
			return _dropped;
		}

		public int GetSent()
		{
			return _sent;
		}
		
		public int GetReceived()
		{
			return _received;
		}
		
		public int GetTimeouts()
		{
			return _timeouts;
		}
		
	    private string _name;
		private int _capacity;
		private int _maxCount;
		private int _dropped;
		private int _sent;
		private int _received;
		private int _timeouts;
		
	}
}
