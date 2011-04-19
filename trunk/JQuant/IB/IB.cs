/// <summary>
/// Contains wrapper for the InteractiveBrokers API and simulation 
/// This is a rather large file
/// </summary>


namespace IB
{
	public enum MessageType
	{
		Connect
	}
	
	/// <summary>
	/// Objects of this type can be sent to the Processot thread
	/// </summary>
	public class Message
	{
		MessageType id;
	
	}
	
	public class Processor : JQuant.MailboxThread<Message>
	{
		/// <summary>
		/// Initialize the class. This is a simpleton - only one object of this 
		/// type exists in the system
		/// </summary>
		protected Processor() 
			:base("Processor", 100)
		{
		
		}
		
		/// <summary>
		/// Create an instance of the processor
		/// </summary>
		/// <returns>
		/// A <see cref="Processor"/>
		/// </returns>
		public Processor CreateProcessor ()
		{
			return null;
		}
		
		/// <summary>
		/// Connect to the server. This method will open a client socket and 
		/// start the FSM which attempts to connect to the specified server. 
		/// The actual connection is goingt to be established in the context of the 
		/// Processor FSM. Notification onConnect can be received asynchronously 
		/// by the application
		/// </summary>
		/// <param name="host">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="port">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool Connect(string host, int port)
		{
			return true;
		}
		
		/// <summary>
		/// Returns true if connection is established with the server and 
		/// the initial handshake is alright
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool isConnected()
		{
			return false;
		}
		
	}

} // namespace IB