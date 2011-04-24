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
	public class Message
	{
		public const int TICK_PRICE = 1;
		public const int TICK_SIZE = 2;
		public const int ORDER_STATUS = 3;
		public const int ERR_MSG = 4;
		public const int OPEN_ORDER = 5;
		public const int ACCT_VALUE = 6;
		public const int PORTFOLIO_VALUE = 7;
		public const int ACCT_UPDATE_TIME = 8;
		public const int NEXT_VALID_ID = 9;
		public const int CONTRACT_DATA = 10;
		public const int EXECUTION_DATA = 11;
		public const int MARKET_DEPTH = 12;
		public const int MARKET_DEPTH_L2 = 13;
		public const int NEWS_BULLETINS = 14;
		public const int MANAGED_ACCTS = 15;
		public const int RECEIVE_FA = 16;
		public const int HISTORICAL_DATA = 17;
		public const int BOND_CONTRACT_DATA = 18;
		public const int SCANNER_PARAMETERS = 19;
		public const int SCANNER_DATA = 20;
		public const int TICK_OPTION_COMPUTATION = 21;
		public const int TICK_GENERIC = 45;
		public const int TICK_STRING = 46;
		public const int TICK_EFP = 47;
		public const int CURRENT_TIME = 49;
		public const int REAL_TIME_BARS = 50;
		public const int FUNDAMENTAL_DATA = 51;
		public const int CONTRACT_DATA_END = 52;
		public const int OPEN_ORDER_END = 53;
		public const int ACCT_DOWNLOAD_END = 54;
		public const int EXECUTION_DATA_END = 55;
		public const int DELTA_NEUTRAL_VALIDATION = 56;
		public const int TICK_SNAPSHOT_END = 57;
		
		/// <summary>
		/// Method will parse the array of bytes and generate an array of information elements
		/// Return trut if success
		/// </summary>
		public delegate bool Parser(byte[] data, int size, int offset, out string[] ies, out int length);
		
		public static readonly Message[] list = new[] {
			new Message(Message.TICK_PRICE, 1, Parser_TICK_PRICE),
			new Message(Message.TICK_SIZE, 1, null),
			new Message(Message.ORDER_STATUS, 1, null),
			new Message(Message.ERR_MSG, 1, null),
			new Message(Message.OPEN_ORDER, 1, null),
			new Message(Message.ACCT_VALUE, 1, null),
			new Message(Message.PORTFOLIO_VALUE, 1, null),
			new Message(Message.ACCT_UPDATE_TIME, 1, null),
			new Message(Message.NEXT_VALID_ID, 1, null),
			new Message(Message.CONTRACT_DATA, 1, null),
			new Message(Message.EXECUTION_DATA, 1, null),
			new Message(Message.MARKET_DEPTH, 1, null),
			new Message(Message.MARKET_DEPTH_L2, 1, null),
			new Message(Message.NEWS_BULLETINS, 1, null),
			new Message(Message.MANAGED_ACCTS, 1, null),
			new Message(Message.RECEIVE_FA, 1, null),
			new Message(Message.HISTORICAL_DATA, 1, null),
			new Message(Message.BOND_CONTRACT_DATA, 1, null),
			new Message(Message.SCANNER_PARAMETERS, 1, null),
			new Message(Message.SCANNER_DATA, 1, null),
			new Message(Message.TICK_OPTION_COMPUTATION, 1, null),
			new Message(Message.TICK_GENERIC, 1, null),
			new Message(Message.TICK_STRING, 1, null),
			new Message(Message.TICK_EFP, 1, null),
			new Message(Message.CURRENT_TIME, 1, null),
			new Message(Message.REAL_TIME_BARS, 1, null),
			new Message(Message.FUNDAMENTAL_DATA, 1, null),
			new Message(Message.CONTRACT_DATA_END, 1, null),
			new Message(Message.OPEN_ORDER_END, 1, null),
			new Message(Message.ACCT_DOWNLOAD_END, 1, null),
			new Message(Message.EXECUTION_DATA_END, 1, null),
			new Message(Message.DELTA_NEUTRAL_VALIDATION, 1, null),
			new Message(Message.TICK_SNAPSHOT_END, 1, null)
		};
				
		public Message(int id, int fields, Parser parser)
		{
			this.id = id;
			this.fields = fields;
			this.parser = parser;
		}
		
		public static Message Find(int id)
		{
			foreach (Message msg in list)
			{
				if (msg.id == id)
				{
					return msg;
				}
			}
			return null;
		}

		/// <summary>
		/// Methos assumes that at offset array data contains message ID. The method will call 
		/// appropriate parser and return array of information elements
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="offset">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="messageId">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="ies">
		/// A <see cref="System.String[]"/>
		/// </param>
		/// <param name="length">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool Parse(byte[] data, int size, int offset, out string[] ies, out int length)
		{
			int firstByte;
			int lastByte;
			bool res = false;
			do
			{
				Message message = Find(0);
				res = message.parser(data, size, offset, out ies, out length);
			}
			while (false);
			
			
			return res;
		}
		
		public static bool Parser_TICK_PRICE (byte[] data, int size, int offset, out string[] ies, out int length)
		{
			bool res = false;
			ies = null;
			length = 0;
			
			return res;
		}
		
		public readonly int id;
		public readonly int fields;		
		public readonly Parser parser;
	}
    
  
  
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
				if (shiftRegisterSize <= 0)
				{
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
		

		protected void RemoveIEValue(int firstByte, int lastByte)
		{
			int ieSize = lastByte - firstByte + 1;
			shiftRegisterSize -= ieSize;
			Array.Copy(shiftRegister, lastByte+1, shiftRegister, 0, shiftRegisterSize);
		}
								
		protected RxHandlerCallback rxHandlerCallback;
		protected RxHandler.State state;
		protected byte[] shiftRegister;
		protected int shiftRegisterSize;
	}

	public class Utils
	{
		/// <summary>
		/// Returns first and last byte of the information element started at the specified offset
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
		public static bool GetIE (byte[] data, int size, int offset, out int firstByte, out int lastByte)
		{
			bool res;
			
			// this is a delimiter at offset. I am going to find position of the next delimiter
			if (data[offset] == 0) {
				firstByte = offset + 1;
			} else {
				firstByte = offset;
			}
			
			lastByte = 0;
			while (offset < size) {
				if (data[offset] == 0) {
					lastByte = offset - 1;
					break;
				}
				offset++;
			}
			
			res = (firstByte < size) && (lastByte > 0);
			
			return res;
		}

		/// <summary>
		/// Return integer value of the information element
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="offset">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="firstByte">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="lastByte">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="ieValue">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool GetIEValue (byte[] data, int offset, int firstByte, int lastByte, out int ieValue)
		{
			bool res = true;
			ieValue = 0;
			
			string ieStr = Encoding.ASCII.GetString (data, offset, lastByte - firstByte + 1);
			try {
				ieValue = Int32.Parse (ieStr);
			} catch (Exception e) {
				res = false;
			}
			
			return res;
		}
		
		public struct IEIndex
		{
			public int firstByte;
			public int lastByte;
		};
		
		/// <summary>
		/// Splits zero delimitered array of bytes into map of information elements
		/// </summary>
		public static bool SplitArray(byte[] data, int offset, int size, out System.Collections.Generic.List<IEIndex> list, out int length)
		{
			list = null;
			length = 0;
			
			if (data[offset] == 0)
			{
				offset++;
			}
			while (offset < size)
			{
				
			}
			
			return false;
		}
	
	}

} // namespace IB