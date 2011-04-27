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
		/// Return true if success
		/// </summary>
		public delegate bool Parser(byte[] data, System.Collections.Generic.List<Utils.IEIndex> ieMap, out string[] ies);
		
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
		/// Methos assumes that array data contains message ID. The method will call 
		/// appropriate parser and return array of information elements
		/// </summary>
		public static bool Parse(byte[] data, System.Collections.Generic.List<Utils.IEIndex> ieMap, out string[] ies)
		{
			bool res = true;
			ies = null;
			do
			{
				// get first IE - message ID
				int ieMessageId;
				string ieMessageIdStr;
				Utils.IEIndex ieIndex = ieMap[0];
				res = Utils.GetIEValue (data, 0, ieIndex.firstByte, ieIndex.lastByte, out ieMessageId, out ieMessageIdStr);
				if (!res)
				{
					System.Console.Out.WriteLine("Failed to parse message ID ("+ieMessageIdStr+")");
					break;
				}
				
				Message message = Message.Find(ieMessageId);
				res = (message != null);
				if (!res)
				{
					System.Console.Out.WriteLine("Failed to find message ID ("+ieMessageId+")");
					break;
				}
				
				res = message.parser(data, ieMap, out ies);
				if (!res)
				{
					break;
				}
			}
			while (false);
			
			return res;
		}
		
		public static bool Parser_TICK_PRICE (byte[] data, System.Collections.Generic.List<Utils.IEIndex> ieMap, out string[] ies)
		{
			bool res = false;
			ies = null;
			
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
			int copySize = size;
			Array.Copy(data, offset, shiftRegister, 0, copySize);
			size -= copySize;
			shiftRegisterSize = copySize;
			
			System.Collections.Generic.List<Utils.IEIndex> ieMap;
			int ieMapLength;
			do
			{
				bool res = Utils.SplitArray(shiftRegister, 0, size, out ieMap, out ieMapLength);
				if (!res)
				{
					break;
				}
				string[] ies;
				res = Message.Parse(shiftRegister, ieMap, out ies);
				if (!res)
				{
					break;
				}
				
				// remove parsed elements
				Utils.RemoveLeadingBytes(shiftRegister, shiftRegisterSize, ieMapLength);
			}
			while (false);
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
		protected byte[] shiftRegister;
		protected int shiftRegisterSize;
	}

	public class Utils
	{
		/// <summary>
		/// Returns first and last byte of the information element started at the specified offset
		/// </summary>
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
		public static bool GetIEValue (byte[] data, int offset, int firstByte, int lastByte, out int ieValue, out string str)
		{
			bool res = true;
			ieValue = 0;
			
			str = Encoding.ASCII.GetString (data, offset, lastByte - firstByte + 1);
			try {
				ieValue = Int32.Parse (str);
			} catch (Exception e) {
				res = false;
			}
			
			return res;
		}
		
		public class IEIndex
		{
			public IEIndex(int firstByte, int lastByte)
			{
				this.firstByte = firstByte;
				this.lastByte = lastByte;
			}
			
			public int firstByte;
			public int lastByte;
		};
		
		/// <summary>
		/// Splits zero delimitered array of bytes into map of information elements. 
		/// An information element is described by first and last byte in the array
		/// </summary>
		public static bool SplitArray(byte[] data, int offset, int size, out System.Collections.Generic.List<Utils.IEIndex> list, out int length)
		{
			list = new System.Collections.Generic.List<Utils.IEIndex>(10);
			length = 0;
			
			if (data[offset] == 0)
			{
				offset++;
			}
			int firstByte = 0;
			int lastByte;
			while (offset < size)
			{
				if (data[offset] == 0)
				{
					lastByte = offset;
					length = offset+1;
					IEIndex ieIndex = new IEIndex(firstByte, lastByte);
					list.Add(ieIndex);
					firstByte = lastByte + 1;
				}
				offset++;
			}
			
			
			return (length != 0);
		}
	
		public static void RemoveIEValue(byte[] data, int size, int firstByte, int lastByte)
		{
			int ieSize = lastByte - firstByte + 1;
			size -= ieSize;
			Array.Copy(data, lastByte+1, data, 0, size);
		}
		
		public static void RemoveLeadingBytes(byte[] data, int size, int firstByte)
		{
			Array.Copy(data, firstByte, data, 0, size-firstByte);
		}
	}

} // namespace IB