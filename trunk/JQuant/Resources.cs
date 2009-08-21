
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
		int GetCount();
		
		/// <summary>
		/// maximum size recorded
		/// </summary>
		int GetMaxCount();
		
		/// <summary>
		/// mailbox capacity
		/// </summary>
		int GetCapacity();
		
		int GetDropped();
		int GetSent();
		int GetReceived();
		int GetTimeouts();
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
		
		static protected Resources r;
	}
}
