
using System;
using System.Collections.Generic;

namespace JQuant
{	

	/// <summary>
	/// objects implement Mailbox
	/// </summary>
	public interface IMailbox {
		/// <summary>
		/// return name of the mailbox
		/// </summary>
		string GetName();
		
		/// <summary>
		/// number of messages in the queue
		/// </summary>
		int GetSize();
		
		/// <summary>
		/// maximum size recorded
		/// </summary>
		int GetMaxSize();
		
		/// <summary>
		/// mailbox capacity
		/// </summary>
		int GetCapacity();
	}
	
	
	/// <summary>
	/// this guy is a simple Queue base implemenation of mailbox - queue of messages with
	/// send and receive methods
	/// </summary>
	public class Mailbox<Message> :Queue<Message>, IMailbox
	{
		public Mailbox(string name, int capacity) : base(capacity)
		{
			_name = name;
			_capacity = capacity;
			_maxSize = 0;
			
			// add myself to the list of created mailboxes
			Resources.Mailboxes.Add(this);
		}
		
		~Mailbox() 
		{
			// clean up - remove all object from the queue
			Clear();
			
			// remove myself from the list of created mailboxes
			Resources.Mailboxes.Remove(this);			
		}

		
		public string GetName()
		{
			return _name;
		}
		
		public int GetSize()
		{
			return Count;
		}

		public int GetMaxSize()
		{
			return _maxSize;
		}

		public int GetCapacity()
		{
			return _capacity;
		}
		
	    private string _name;
		private int _capacity;
		private int _maxSize;
		
	}
}
