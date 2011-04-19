/// <summary>
/// Contains wrapper for the InteractiveBrokers API and simulation 
/// This is a rather large file
/// </summary>


using System;

// file system
using System.IO;

// text stream
using System.Text;

namespace IB
{
	/// <summary>
	/// Message types to be sent to Processor
	/// </summary>
	public enum MessageType
	{
		Connect
	}

	/// <summary>
	/// Possible states of the Processor
	/// </summary>
	public enum State
	{
		Idle,
		Connecting,
		Connected
	}
	
	/// <summary>
	/// Objects of this type can be sent to the Processot thread
	/// </summary>
	public class Message
	{
		public Message(MessageType id, object data, object data1)
		{
			this.data = data;
			this.data1 = data1;
			this.id = id;
		}
		
		public Message(MessageType id, object data)
		{
			this.data = data;
			this.data1 = null;
			this.id = id;
		}

		public MessageType id;
		public object data;
		public object data1;
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
			state = State.Idle;
		
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
		/// <param name="host">
		/// </summary>
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
			this.Send(new Message(MessageType.Connect, host, port));
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
			return (state == State.Connected);
		}
		
		protected override void HandleMessage (Message message)
		{
			switch (state)
			{
			case State.Idle:
				HandleMessage_Idle (message);
				break;
			case State.Connecting:
				HandleMessage_Connecting (message);
				break;
			case State.Connected:
				HandleMessage_Connected (message);
				break;
			}
		}

		protected void HandleMessage_Idle (Message message)
		{
			switch (message.id)
			{
				case MessageType.Connect:
				HandleMessage_IdleConnect (message);
				break;
			}
		
		}
		
		protected void HandleMessage_Connected (Message message)
		{
			
		}

		protected void HandleMessage_Connecting (Message message)
		{
			
		}
		
		protected void HandleMessage_IdleConnect (Message message)
		{
			string host = (string)message.data;
			int port = (int)(message.data1);
			clientSocket = null;
			try
			{
				clientSocket = new System.Net.Sockets.TcpClient (host, port);
			}
			catch (Exception e)
			{
				System.Console.WriteLine ("Failed to connect " + host + ", port "+port);
				System.Console.WriteLine (e.ToString ());
			}
			if (clientSocket != null)
			{
				bool res = writeToSocket(clientSocket, CLIENT_VERSION);
				if (res)
				{
					this.state = State.Connecting;
				}
				else
				{
				}
			}
		
		}

		protected static bool writeToSocket(System.Net.Sockets.TcpClient clientSocket, int data)
		{
			return false;
		}

		protected State state;
		protected System.Net.Sockets.TcpClient clientSocket;
		private static int CLIENT_VERSION = 46;
		private static int SERVER_VERSION = 38;
		
	}

} // namespace IB