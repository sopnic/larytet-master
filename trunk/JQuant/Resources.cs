
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

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
	
	
	public enum ThreadState {
		[Description("Initialized")] Initialized,
		[Description("Satrted")] Started,
		[Description("Stoped")] Stoped,
		[Description("Destroyed")] Destroyed
		
	};
	
	/// <summary>
	/// objects implement MailboxThread
	/// </summary>
	public interface IThread {
		ThreadState GetState();
		string GetName();
	}
	
	/// <summary>
	/// objects implementing Pool interface
	/// </summary>
	public interface IPool {
		string GetName();
		int GetCapacity();
		int GetCount();
		int GetMinCount();
		int GetAllocOk();
		int GetAllocFailed();
		int GetFreeOk();
	}
	
	/// <summary>
	/// a storage of all created objects
	/// an object central
	/// this guy is singleton - application calls Init() only once to initialize the class
	/// </summary>
	public class Resources
	{
		protected Resources()
		{
			Mailboxes = new List<IMailbox>(10);
			Threads = new List<IThread>(10);
			Pools = new List<IPool>(10);
		}
		
		static public void Init() {
			if (r == null) {
				r = new Resources();
			}
		}
		
		/// <summary>
		/// created in the system mailboxes
		/// </summary>
		public static List<IMailbox> Mailboxes;
		public static List<IThread> Threads;
		public static List<IPool> Pools;
		
		static protected Resources r;
	}
}
