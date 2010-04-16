
using System;

// file system
using System.IO;

// text stream
using System.Text;


/// <summary>
/// I need an embedded HTTP server and this code should do the trick. This is very simple HTTP server. 
/// No HTTPS. Limited number of simultaneous connections. 
/// </summary>
namespace JQuantHttp
{
	public enum CtrlMsgEvent
	{
		START,
		PAUSE,
		STOP
	}
	
	public class ControlMessage
	{
		public CtrlMsgEvent msgEvent;
	}
	
	public class Http
	{
		/// <summary>
		/// Create an HTTP server. Usually only one HTTP server will be created 
		/// </summary>
		/// <param name="port">
		/// A <see cref="System.Int32"/>
		/// TCP port, for example 80
		/// </param>
		/// <param name="maxConnections">
		/// A <see cref="System.Int32"/>
		/// Maximum number of requests served simultaneously, for example 5
		/// </param>
		/// <param name="rootPath">
		/// A <see cref="System.String"/>
		/// Full path to the files to be served by the server
		/// </param>
		public Http(int port, int maxConnections, string rootPath)
		{
			this.Port = port;
			this.MaxConnections = maxConnections;
			this.RootPath = rootPath;
			
			// i need a thread waiting for incoming connection requests
			// and a pool of threads which serve the requests
			threadPool = new JQuant.ThreadPool("HTTPjob", MaxConnections);
			
			// i need a TCP socket where I wait for incoming connection requests
			tcpListener = default(System.Net.Sockets.TcpListener);
			try 
			{
				tcpListener = new System.Net.Sockets.TcpListener(Port);
			}
			catch (Exception e)
			{
				System.Console.WriteLine("Failed to open port "+Port);
				System.Console.WriteLine(e.ToString());
			}
			if (tcpListener != default(System.Net.Sockets.TcpListener))
			{
			}
			
			mainThread = new MainThread(RootPath, tcpListener, threadPool);
		}
		
		public void Start()
		{
			if (tcpListener != default(System.Net.Sockets.TcpListener))
			{
				try
				{
					tcpListener.Start();
				}
				catch (Exception e)
				{
					System.Console.WriteLine("Failed to bind port "+Port);
					System.Console.WriteLine(e.ToString());
				}
			}
			mainThread.Start();
			System.Console.WriteLine("HTTP server started on port "+Port);
		}

		public void Stop()
		{
			mainThread.Stop();
			if (tcpListener != default(System.Net.Sockets.TcpListener))
			{
				tcpListener.Stop();
			}
		}

		public string RootPath
		{
			protected set;
			get;
		}
		
		public int MaxConnections
		{
			protected set;
			get;
		}
		
		public int Port
		{
			protected set;
			get;
		}
		
		protected enum State
		{
			RUN,
			PAUSE,
			EXIT
		}
		protected class MainThread : JQuant.MailboxThread<ControlMessage>
		{
			public MainThread(string rootPath, System.Net.Sockets.TcpListener tcpListener, JQuant.ThreadPool threadPool) :
				base("HTTP", 10, MBX_TIMEOUT_IDLE)
			{
				this.threadPool = threadPool;
				this.tcpListener = tcpListener;
				this.rootPath = rootPath;
				
	            string patternGet = "^GET /(.*) HTTP/.+";
	            this.regexPatternGet = new System.Text.RegularExpressions.Regex(patternGet);
			}
			
	        protected override void HandleMessage(ControlMessage message)
	        {
				// process incoming messages
				if (message != default(ControlMessage))
				{
					HandleMbxMessage(message);
				}
				
				// check if there is something in the socket
				HandleSocket();
			}
			
	        protected void HandleSocket()
	        {
				// check if there is something in the socket
				bool clientPending = tcpListener.Pending();
				if ((threadPool.FreeThreads() > 0) && clientPending)
				{
					Timeout = MBX_TIMEOUT_FAST;
					System.Net.Sockets.TcpClient tcpClient = default(System.Net.Sockets.TcpClient);
					
					// accept conneciton
					try 
					{
						tcpClient = tcpListener.AcceptTcpClient();
					}
					catch (Exception e)
					{
						System.Console.WriteLine(e.ToString());
					}
					if (tcpClient != default(System.Net.Sockets.TcpClient))
					{
						threadPool.PlaceJob(HandleClient, null, tcpClient);
					}
				}
				
				if (!clientPending)
				{
					Timeout = MBX_TIMEOUT_IDLE;
				}					
			}
					
	        protected void HandleMbxMessage(ControlMessage message)
	        {
				switch (message.msgEvent)
				{
					case CtrlMsgEvent.START:
					state = State.RUN;
					Timeout = MBX_TIMEOUT_FAST;
					break;
						
					case CtrlMsgEvent.STOP:
					state = State.EXIT;
					break;
						
					case CtrlMsgEvent.PAUSE:
					state = State.PAUSE;
					Timeout = MBX_TIMEOUT_IDLE;
					break;			
				}
			}
			
			protected void HandleClient(ref object argument)
			{
				byte[] buffer = new byte[1024];
				System.Net.Sockets.TcpClient tcpClient = (System.Net.Sockets.TcpClient)argument;
				try
				{
					System.Net.Sockets.NetworkStream networkStream = tcpClient.GetStream();
					int result = networkStream.Read(buffer, 0, buffer.Length);
					if (result > 0)
					{
						// convert bytes to string
						string request = Encoding.ASCII.GetString(buffer, 0, result);
						ProcessRequest(rootPath, networkStream, request);
					}
					
					// Byte[] sendBytes = Encoding.ASCII.GetBytes("<b>Hello, world</b>");
					// networkStream.Write(sendBytes, 0, sendBytes.Length);
					tcpClient.Close();
				}
				catch (Exception e)
				{
				}
			}
			
			protected void ProcessRequest(string rootPath, System.Net.Sockets.NetworkStream networkStream, string clientRequest)
			{
	            System.Text.RegularExpressions.MatchCollection matches = regexPatternGet.Matches(clientRequest);
                int matchesCount = matches.Count;

				System.Console.WriteLine("clientRequest="+clientRequest);
				System.Console.WriteLine("matchesCount="+matchesCount);
                if (matchesCount == 1)
                {
	                // get groups
	                System.Text.RegularExpressions.Match match = matches[0];
	                System.Text.RegularExpressions.GroupCollection groups = match.Groups;
					
                    string filename = groups[1].Captures[0].ToString(); // group[0] is reserved for the whole match
					
					// server does not care about subdirectories. Send only files in the root directory 
					filename = System.IO.Path.GetFileName(filename);
					
					// no file means index.html file
					if (filename.Length == 0) filename = "index.html"; 
					
					// add root
					filename = rootPath + filename;
					
					// i have to figure out if this is a file or CGI script (management request)
					// management requests contain '?'
					if (filename.Contains("?"))
					{
						// look in the list of all CGI scripts and if found - call the delegate method
					}
					else
					{
						// send the file
						SendFile(networkStream, filename);
					}
                }
			}
			
			protected static void SendFile(System.Net.Sockets.NetworkStream networkStream, string filename)
			{
				do
				{
					bool fileExists = (System.IO.File.Exists(filename));  // file exists ?
					if (!fileExists)
					{
						SendErrorFileNotExist(networkStream, filename);
						break;
					}
					
					// try to open the file for reading
					System.IO.FileStream fileStream = null;
					try 
					{
		                fileStream = new System.IO.FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
					}
					catch (Exception e)
					{
						SendErrorFileReadError(networkStream, filename);
						break;
					}
					
					FileInfo fi = new FileInfo(filename);
					DateTime fileModified = fi.LastWriteTime;
					long fileSize = fi.Length;
					
					// I can read from the file. Send first part of the server response - header
					SendHeader(networkStream, fileModified, fileSize, "text/html");
				
					// send the data
					SendOctets(networkStream, fileStream, fileSize);
					
					// try to close the file
					try
					{
						fileStream.Close();
					}
					catch (Exception e)
					{
						System.Console.WriteLine(e.ToString());
					}
				}
				while (false);
			}
			
			/// <summary>
			/// send the data - read the file block by block and write to the socket
			/// i should send fileSize bytes in total
			/// </summary>
			protected static bool SendOctets(System.Net.Sockets.NetworkStream networkStream, System.IO.FileStream fileStream, long fileSize)
			{
				byte[] buffer = new byte[1024];
				while (fileSize > 0)
				{
					int readBytes = fileStream.Read(buffer, 0, buffer.Length);
					if (readBytes > 0)
					{
						try 
						{
							networkStream.Write(buffer, 0, readBytes);
						}
						catch (Exception e){break;}
					}
					else break;
					fileSize -= readBytes;
				}
				
				bool result = (fileSize == 0);
				return result;
			}
			
			
			protected static bool SendHeader(System.Net.Sockets.NetworkStream networkStream, DateTime fileModified, long fileSize, string mime)
			{
				bool result = true;
				string header = "HTTP/1.1 200 OK\r\n"+"Date: "+fileModified.ToString()+"\r\n"+
								"Server: JQuant\r\n"+
								"Last-Modified: Wed, 08 Jan 2003 23:11:55 GMT\r\n"+
								"Accept-Ranges: bytes\r\n"+
								"Content-Length: "+fileSize+"\r\n"+
								"Connection: close\r\n"+
								"Content-Type: "+mime+"; charset=UTF-8\r\n\r\n";
				byte[] data = Encoding.ASCII.GetBytes(header);
				try 
				{
					networkStream.Write(data, 0, data.Length);
				}
				catch (Exception e)
				{
					result = false;
				}
				
				return result;
			}
			
			protected static void SendErrorFileNotExist(System.Net.Sockets.NetworkStream networkStream, string filename)
			{
				string s = "File "+filename+" not exists";
				byte[] data = Encoding.ASCII.GetBytes(s);
				try 
				{
					networkStream.Write(data, 0, data.Length);
				}
				catch (Exception e)
				{
				}
			}
			
			protected static void SendErrorFileReadError(System.Net.Sockets.NetworkStream networkStream, string filename)
			{
				string s = "Failed to read file "+filename;
				byte[] data = Encoding.ASCII.GetBytes(s);
				try 
				{
					networkStream.Write(data, 0, data.Length);
				}
				catch (Exception e)
				{
				}
			}
			
			/// <summary>
			/// State of the HTTP main thread
			/// </summary>
			protected State state;
			protected static int MBX_TIMEOUT_IDLE = 1000;
			protected static int MBX_TIMEOUT_FAST = 50;
			protected System.Net.Sockets.TcpListener tcpListener;
			protected JQuant.ThreadPool threadPool;
			protected string rootPath;
			protected System.Text.RegularExpressions.Regex regexPatternGet;
		}
		
		protected JQuant.ThreadPool threadPool;
		protected System.Net.Sockets.TcpListener tcpListener;
		JQuant.MailboxThread<ControlMessage> mainThread;
	}
}
