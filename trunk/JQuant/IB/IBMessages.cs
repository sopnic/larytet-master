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
	
		/// <summary>
		/// A message from IB is always starst from message identifier, for example REQ_MKT_DATA (1), followed by 
		/// version (always 1?) and message specific data (tickerId). The information elemnets are zero delimited 
		/// ASCII strings
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void HandleData (byte[] data, int size)
		{
			switch (state)
			{
				case State.Idle:
				HandleData_Idle(data, size);
				break;
				
				case State.Processing:
				HandleData_Processing(data, size);
				break;
				
			}
		}

		/// <summary>
		/// This is a new data - I expect to find a known message ID in the first information element
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void HandleData_Idle (byte[] data, int size)
		{
		
		}
		
		/// <summary>
		/// I am in the middle of processing of a message and this is new chunk of data. I 
		/// shall start the processing from the message ID I discovered in the previous chunk
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void HandleData_Processing (byte[] data, int size)
		{
		
		}
				
		protected RxHandlerCallback rxHandlerCallback;
		protected RxHandler.State state;
	}

} // namespace IB