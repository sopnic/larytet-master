
using System;
using System.Collections.Generic;

namespace JQuant
{
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
