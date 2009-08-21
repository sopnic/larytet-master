
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
	/// </summary>
	public class Resources
	{
		protected Resources()
		{
		}
		
		/// <summary>
		/// created in the system mailboxes
		/// </summary>
		public static List<IMailbox> Mailboxes;
	}
}
