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
	
		/// <summary>
		/// Create a parser. 
		/// </summary>
		/// <param name="rxHandlerCallback">
		/// A <see cref="RxHandlerCallback"/>
		/// </param>
		/// <param name="bufferSize">
		/// A <see cref="System.Int32"/>
		/// Maximum buffer size in the call to HandleData()
		/// </param>
		public RxHandler(RxHandlerCallback rxHandlerCallback, int bufferSize)
		{
			this.rxHandlerCallback = rxHandlerCallback;
			state = RxHandler.State.Idle;
			shiftRegister = new byte[bufferSize];
			shiftRegisterSize = 0;
		}
	
		/// <summary>
		/// A message from IB is always starst from message identifier, for example REQ_MKT_DATA (1), followed by 
		/// version (always 1?) and message specific data (tickerId). The information elemnets are zero delimited 
		/// ASCII strings. Method copies the data locally and array data[] can be reused by the calling method
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
			int offset = 0;
			int firstByte;
			int lastByte;
			
			// copy the data to the shift register
			int copySize = Math.Min(size, shiftRegister.Length);
			Array.Copy(data, offset, shiftRegister, 0, copySize);
			size -= copySize;
			shiftRegisterSize = copySize;
			
			while (true)
			{
				// try to fetch first IE - message ID
				bool getIEResult = GetIE(shiftRegister, size, offset, out firstByte, out lastByte);
				if (!getIEResult)
				{
					state = RxHandler.State.Processing;
					break;
				}
				int ieValue;
				// I have got first IE - message ID. Try to convert to string and after that to integer
				getIEResult = GetIEValue(data, firstByte, lastByte, out ieValue);
				if (!getIEResult)
				{
					state = RxHandler.State.Idle;
					// I failed to parse the IE, drop the data from the shift register
					int ieSize = lastByte - firstByte + 1;
					shiftRegisterSize -= ieSize;
					Array.Copy(shiftRegister, lastByte+1, shiftRegister, 0, shiftRegisterSize);
					break;
				}
			}
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
		
		/// <summary>
		/// Returns first and last byte of the word started at the specified offset
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="offset">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		protected bool GetIE(byte[] data, int size, int offset, out int firstByte, out int lastByte)
		{
			bool res;
			
			// this is a delimiter at offset. I am going to find position of the next delimiter
			if (data[offset] == 0)
			{
				firstByte = offset+1;
			}
			else
			{
				firstByte = offset;			
			}
			
			lastByte = 0;
			while (offset < size)
			{
				if (data[offset] == 0)
				{
					lastByte = offset - 1;
					break;
				}
				offset++;
			}
			
			res = (firstByte < size) && (lastByte > 0);
			
			return res;
		}
		
		protected bool GetIEValue(byte[] data, int firstByte, int lastByte, out int ieValue)
		{
			bool res = true;
			ieValue = 0;
			
			string ieStr = Encoding.ASCII.GetString(shiftRegister, 0, lastByte - firstByte + 1);
			try
			{
				ieValue = Int32.Parse(ieStr);
			}
			catch (Exception e)
			{
				res = false;
			}
		
			return res;
		}
				
		protected RxHandlerCallback rxHandlerCallback;
		protected RxHandler.State state;
		protected byte[] shiftRegister;
		protected int shiftRegisterSize;
	}

} // namespace IB