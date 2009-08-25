using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;
 
using System.Reflection;
using System.ComponentModel;


/// <summary>
/// thin shell around TaskLib 
/// or around FMRSym if compiler directive USEFMRSIM is defined 
/// </summary>
namespace FMRShell
{
	public enum ConnectionState
	{
        [Description("Idle")]
        Idle,                              // never opened yet
        [Description("Established")]       
        Established, 
        [Description("Trying")]
        Trying,
        [Description("Closed")]
        Closed,
        [Description("Disposed")]           
        Disposed                           // Dispose() called
	}
	
	/// <summary>
	/// this guy logins to the remote server
	/// and eventually will run state machine keeping the connection alive and kicking 
	/// and will notify other subsystems if and when the connection goes down
	/// this little guy is one of the most important. there are two sources of information
	/// related to the connection status
	/// - periodic attempts to read data stream
	/// - TaskLib.UserClass 
	/// This class handles all included in the TaskLib.UserClass and login related
	/// When there is no real TaskLib the class calls TaskLibSim
	/// 
	/// Normally application will do something like
	/// FMRShell.Connection connection = new FMRShell.Connection("xmlfilename")
	/// bool openResult = connection.Open(errCode)
	/// do work with connection.userClass  of type TaskLib.UserClass
	/// connection.Dispose();
	/// </summary>
	public class Connection : IDisposable
	{
		/// <summary>
		/// use default hard coded user name and password
		/// </summary>
		public Connection()
		{
			//  set defaults
			parameters = new ConnectionParameters("aryeh", // user name
                                                "abc123", // password
                                                ""       // app passsword
		                                                );
			Init();
		}
		
		/// <summary>
		/// open connection based on the login information storedin the specified XML file
		/// See example of the XML file in the ConnectionParameters.xml
		/// </summary>
		/// <param name="filename">
		/// A <see cref="System.String"/>
		/// Name of the XML file where the user login credentials can be found
		/// </param>
		public Connection(string filename)
		{
			xmlFileName = filename;
			useXmlFile = true;
			Init();
		}
		
		/// <summary>
		/// do some general initialization common for all constructors
		/// </summary>
		private void Init()
		{
			stateListeners = new List<JQuant.ISink<ConnectionState>> (5);
			state = ConnectionState.Idle;
			userClass = new TaskBarLibSim.UserClass();
		}
		
		/// <summary>
		/// application have to call this method to get rid of the 
		/// connection (close sockets, etc.)
		/// </summary>
		public void Dispose()
		{
			// call userClass.Logout here
			
			// set userClass to null
			
			state = ConnectionState.Disposed;
		}
		
		~Connection()
		{
		}
			
		public int GetSessionId()
		{
			return sessionId;
		}
		
		public string GetErrorMsg()
		{
			return errMsg;
		}
		
		/// <summary>
		/// not supposed to be called by application (protected)
		/// read login credentials from the XML file
		/// </summary>
		/// <param name="xmlFileName">
		/// A <see cref="System.String"/>
		/// File name of the XML file 
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// True - if XML parse is Ok
		/// </returns>
        protected bool ReadXml(string xmlFileName, out ConnectionParameters parameters)
        {
			bool result = false;
			do{
				ReadConnectionParameters reader = new ReadConnectionParameters(xmlFileName);
				
				// let's try to read the file 
				result = reader.Parse(out parameters);
				if (!result) break;
	
				result = true;
			}	
			while (false);
			
			return result;
		}
		
		/// <summary>
		/// return fals if the open connection fails 
		/// normally application will call Open() without arguments - blocking Open
		/// or Keep() - which runs a thread and attempts to keep the connection
		/// alive. 
		/// </summary>
		/// <param name="returnCode">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// True if Ok and returnCode is set to somethign meaningful
		/// Application will check that the method retruned true and only 
		/// after that analyze returnCode
		/// </returns>
        public bool Open(out int returnCode)
        {
			bool result = true;
			returnCode = 0;
			
			// should I read login credentials from XML file ?
			if (useXmlFile) {
				result = ReadXml(xmlFileName, out parameters);
			}
			
			if (result)
			{
	            returnCode = userClass.Login(parameters.userName, parameters.userPassword, parameters.appPassword, 
				                             out  errMsg, out  sessionId);
				if (returnCode >= 0)
				{
					// according to the documentation returnCode == sessionId
					if (sessionId != returnCode)
					{
						Console.WriteLine("Session Id="+sessionId+",returnCode="+returnCode+" are not equal after Login");
					}
					// at this point userClass object is completely initialized and 
					// ready for work
					state = ConnectionState.Established;
				}
				else  // Login failed - application will try again later
				{
					state = ConnectionState.Trying;			
					result = false;
				}
			}
			
            return result;
        }			
		
		/// <summary>
		/// the calling thread will be blocked by this method until Login to the
		/// remote server succeeds 
		/// </summary>
        public void Open()
        {
			state = ConnectionState.Trying;
			// loop until login succeeds
			do {
				int returnCode;
				bool openResult = Open(out returnCode);
				if (openResult)
				{
					state = ConnectionState.Established;
					break;
				}
				// i need a short delay here before next attempt - 5s
				Thread.Sleep(5*1000);
			}
			while (true);
		}

		
		/// <summary>
		/// this method spawns a thread which will attemp to establish the connection
		/// and keep the connection alive
		/// the call to Keep() is non-blocking. 
		/// Before calling to Keep() application should call AddStateListener() and register 
		/// interface allowing to listen to changes in the connection state
		/// </summary>
        public void Keep()
        {
		}
		
		/// <summary>
		/// Application will call this method to install listeners - callback functions 
		/// which should be called when the state of the connection changed
		/// Class Connection will call Notify() for all registered sinks
		/// </summary>
		public void AddStateListener(JQuant.ISink<ConnectionState> sink)
		{
			stateListeners.Add(sink);
		}
		
		/// <summary>
		/// this is remove from the list and if the list is large the method can be quite 
		/// expensive
		/// </summary>
		public void RemoveStateListener(JQuant.ISink<ConnectionState> sink)
		{
			stateListeners.Remove(sink);
		}
		
		public string GetUserName()
		{
			return parameters.userName;
		}
		
		public ConnectionState state
		{
			get;
			protected set;
		}
		
        protected int sessionId;
        protected string errMsg;
		protected string xmlFileName;
		protected bool useXmlFile;
		protected ConnectionParameters parameters;
        protected List<JQuant.ISink<ConnectionState>> stateListeners;

	
		/// <value>
		/// in the real code TaskBar instead of TaskBarSim will be used
		/// </value>
#if USEFMRSIM
        public TaskBarLibSim.UserClass userClass
		{
			get;
			protected set;
		}
#else			
        public TaskBarLib.UserClass userClass
		{
			get;
			protected set;
		}
#endif			
	}
	
	/// <summary>
	/// handle XML file containing connection parameters
	/// </summary>
	class ReadConnectionParameters :XmlTextReader
	{
		public ReadConnectionParameters(string filename) :base(filename)
		{
		}
		
		private enum xmlState {
	        [Description("BEGIN")]
			BEGIN,
	        [Description("PARAMS")]
			PARAMS,
	        [Description("USERNAME")]
			USERNAME,
	        [Description("PASSWORD")]
			PASSWORD,
	        [Description("APP_PASSWORD")]
			APP_PASSWORD
		}
		
		public bool Parse(out ConnectionParameters parameters)
		{
			xmlState state = xmlState.BEGIN;
			string username = "";
			string appPassword = "";
			string password = "";
			bool result = true;
			string val;
			parameters = null;
			
			while (base.Read())
			{
				switch (base.NodeType)
				{
				case XmlNodeType.Element:
					val = base.Name; 
					if ((val.Equals("connectionparameters")) && (state == xmlState.BEGIN))
					{
						state = xmlState.PARAMS;
					}
					else if ((val.Equals("username")) && (state == xmlState.PARAMS))
					{
						state = xmlState.USERNAME;
					}
					else if ((val.Equals("password")) && (state == xmlState.USERNAME))
					{
						state = xmlState.PASSWORD;
					}
					else if ((val.Equals("apppassword")) && (state == xmlState.PASSWORD))
					{
						state = xmlState.APP_PASSWORD;
					}
					else 
					{
						Console.WriteLine("Failed at element "+val+ " in state "+JQuant.EnumUtils.GetDescription(state));
						result = false;
					}
					break;
					
				case XmlNodeType.Text:
					val = base.Value; 
					if (state == xmlState.USERNAME) {
						username = val;
					}
					else if (state == xmlState.PASSWORD)
					{
						password = val;
					}
					else if (state == xmlState.APP_PASSWORD)
					{
						appPassword = val;
					}
					else
					{
						Console.WriteLine("Failed at text "+val+ " in state "+JQuant.EnumUtils.GetDescription(state));
						result = false;
					}
					break;
					
				case XmlNodeType.EndElement:
					// I will not check that endelement Name is Ok 
					val = base.Name; 
					if ((val.Equals("connectionparameters")) && (state == xmlState.APP_PASSWORD))
					{
					}
					else if ((val.Equals("username")) && (state == xmlState.USERNAME))
					{
					}
					else if ((val.Equals("password")) && (state == xmlState.PASSWORD))
					{
					}
					else if ((val.Equals("apppassword")) && (state == xmlState.APP_PASSWORD))
					{
					}
					else 
					{
						Console.WriteLine("Failed at EndElement "+val+ " in state "+JQuant.EnumUtils.GetDescription(state));
						result = false;
					}					
					break;						
				}
				
				// somethign is broken in the XML file
				if (result)
				{
					parameters = new ConnectionParameters(username, password, appPassword);
				}
				else
				{
					break;
				}
			}
			
			
			return result;
		}
			
	}

	/// <summary>
	/// User login credentials, IP address and everything else required
	/// to establish and keep connection. This is just a data holder
	/// </summary>
	/// <returns>
	/// A <see cref="System.Int32"/>
	/// </returns>
	public class ConnectionParameters
	{
		public ConnectionParameters(string name, string password, string apassw)
		{
			userName = name;
			userPassword = password;
			appPassword = apassw;
		}
		
        public string userName
        {
            get;
            protected set;
        }
		
        public string userPassword
        {
            get;
            protected set;
        }
		
		
        public string appPassword
        {
			get;
			set;
		}				
	}
	
		

	/// <summary>
	/// Example of usage of the class, Main_ should be replaced by Main in the real application
	/// </summary>
    class FMRShellTest
    {
        public static void Main_(string[] args)
        {
			// use default hard coded settings
            Connection newConn = new FMRShell.Connection();
			int returnCode;
            bool result  = newConn.Open(out returnCode);
			if (! result) {
	            Console.WriteLine("Connection start failed: return code=" + returnCode +
				                  ", errorMsg=" + newConn.GetErrorMsg());
	            Console.WriteLine();
	            Console.ReadLine();
			}
        }
    }

}
	

