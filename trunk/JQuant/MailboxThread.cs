
using System;
using System.Threading;

namespace JQuant
{
	
	/// <summary>
	/// Thread waiting forever for a message
	/// </summary>
	public class MailboxThread<Message> :IThread
	{
		
		public MailboxThread(string name, int mailboxCapacity)
		{
			_mailbox = new Mailbox<Message>(name, mailboxCapacity);
			_isAlive = false;
			
			// add myself to the list of created mailboxes
			Resources.Threads.Add(this);
			
			_state = ThreadState.Initialized;
		}
		
		~ MailboxThread()
		{
			// no point in the mailbox if there is nobody to get message from it
			// may be different application will not need this 
			_mailbox = null;
			
			// remove myself from the list of created mailboxes
			Resources.Threads.Remove(this);			

			_state = ThreadState.Destroyed;
		}
		
		public void Run()
		{
			_state = ThreadState.Started;
			_isAlive = true;
			while (_isAlive) 
			{
				Message msg;
				bool result = _mailbox.Receive(msg);
				if (result) {
					HandleMessage(msg);
				}
			}
			
			_state = ThreadState.Stoped;
		}
		
		/// <summary>
		/// application will override this method
		/// </summary>
		/// <param name="message">
		/// A <see cref="Message"/>
		/// </param>
		protected virtual void HandleMessage(Message message)
		{
			Console.WriteLine("I can't handle message "+message);
		}

		public bool Send(Message message)
		{
			bool result = _mailbox.Send(message);
			return result;
		}
		
		public void Stop()
		{
			_isAlive = false;
			_mailbox.Pulse();
		}
					
		/// <summary>
		/// call this method to start the thread - enters loop forever in the 
		/// method Run()
		/// </summary>
		public void Start()
		{
			_isAlive = true;
			_thread = new Thread(new ThreadStart(this.Run));
			_thread.Start();
		}
		
		public ThreadState GetState()
		{
			return _state;
		}

		public string GetName()
		{
			return _mailbox.GetName();
		}
		
		private Mailbox<Message> _mailbox;
		private bool _isAlive;
		private ThreadState _state;
		private Thread _thread;
	}
}