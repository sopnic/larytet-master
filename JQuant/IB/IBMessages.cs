/// <summary>
/// Contains class handling all received messages from the IB
/// </summary>


using System;

// file system
using System.IO;

// text stream
using System.Text;

namespace IB
{
	/// <summary>
	/// RxHandler will call the method for all incoming messages
	/// </summary>
	public delegate void RxHandlerCallback();

	/// <summary>
	/// This class handles a parser of messages from the IB. The parser is a simple state machine. Application
	/// calls method HandleData() with the data received. Method HandleData() pulls the message(s) and calls 
	/// application callback. Applicaiton will need  one instance of this class for every active connection.
	/// </summary>
	public class RxHandler
	{
		protected enum State
		{
			Idle,
			Processing
		};
	
		public RxHandler(RxHandlerCallback rxHandlerCallback)
		{
			this.rxHandlerCallback = rxHandlerCallback;
			state = RxHandler.State.Idle;
		}
	
		public void HandleData (byte[] data, int size)
		{
		}
		
		protected RxHandlerCallback rxHandlerCallback;
		protected RxHandler.State state;
	}

} // namespace IB