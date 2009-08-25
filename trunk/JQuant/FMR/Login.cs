using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FMRShell
{
	/// <summary>
	/// This is a thin shell above TaskLib/FMRSim
	/// lot of ifdefs are going to be here - depending on the compilation falg TaskLib 
	/// or FMRSim are going to be used
	/// </summary>
    class FMRShell
    {
		
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
		/// this guy keeps connection and eventually will run state machine keeping the connection
		/// alive and kicking
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
				userClass = new TaskBarLibSim.UserClass();
			}
			
			/// <summary>
			/// open connection based on the login information storedin the specified XML file
			/// </summary>
			public Connection(string filename)
			{
			}
			
			/// <summary>
			/// application have to call this emthod to get rid of the 
			/// connection (close sockets, etc.)
			/// </summary>
			public void Dispose()
			{
			}
			
			~Connection()
			{
			}
				
			public ConnectionParameters parameters
			{
				get;
				protected set;
			}
			
			
			public int GetSessionId()
			{
				return sessionId;
			}
			
			public string GetErrorMsg()
			{
				return errMsg;
			}
			
            public TaskBarLibSim.UserClass userClass
			{
				get;
				protected set;
			}
			
			/// <summary>
			/// return fals if the open connection fails 
			/// </summary>
	        public bool Open(out int returnCode)
	        {
	            returnCode = userClass.Login(parameters.userName, 
				                               parameters.userPassword, 
				                               parameters.appPassword, 
				                             out  errMsg,
				                             out  sessionId);
				
				// handle the return code here and return false/true here correctly
				
	            return true;
	        }			
			
			
	        protected int sessionId;
	        protected string errMsg;
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
            FMRShell.Connection newConn = new FMRShell.Connection();
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
	

