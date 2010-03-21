
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;

#if USEFMRSIM
using TaskBarLibSim;
#else
using TaskBarLib;
#endif

namespace JQuant
{

    partial class Program
    {

        #region Oper Callbacks
        // Operations are intended to run in the real TaskBar environment - no simulated API

        protected void operLoginCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            //check if there is connection already open:
            if (this.fmrConection != null)
            {
                iWrite.WriteLine(
                    Environment.NewLine
                    + "WARNING !!! You're already logged in with SessionId="
                    + this.fmrConection.GetSessionId()
                    + Environment.NewLine
                    + "Login Status: "
                    + this.fmrConection.loginStatus
                    + Environment.NewLine
                    );
            }
            //if no open connection - perform login process:
            else
            {
                // Define where the xml with connection params is.
                // To define JQUANT_ROOT - see howto in Main(...) in Main.cs
                string ConnFile = Environment.GetEnvironmentVariable("JQUANT_ROOT");
                ConnFile += "ConnectionParameters.xml";

                this.fmrConection = new FMRShell.Connection(ConnFile);

                bool openResult;
                int errResult;
                openResult = this.fmrConection.Open(iWrite, out errResult, true);

                iWrite.WriteLine("");
                if (openResult)
                {
                    iWrite.WriteLine("Connection opened for " + this.fmrConection.GetUserName());
                    iWrite.WriteLine("sessionId=" + errResult);
                }
                else
                {
                    iWrite.WriteLine("Connection failed errResult=" + errResult);
                    iWrite.WriteLine("Error description: " + this.fmrConection.LoginErrorDesc());
                }

                iWrite.WriteLine("Login status is " + this.fmrConection.loginStatus.ToString());
            }
        }

        protected void operLogoutCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            if (fmrConection != null)
            {
                int s = this.fmrConection.GetSessionId();
                this.fmrConection.Dispose();
                this.fmrConection = null;  //set connection to null
                Console.WriteLine("Session with id " + s + " was terminated.");
            }
            else
            {
                Console.WriteLine("There is no active connection - you're not logged in.");
            }
        }


        protected void operLogCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            //first check if logged in:
            if (this.fmrConection == null)
            {
                iWrite.WriteLine(Environment.NewLine
                    + "WARNING! You're not logged in. Please log in first!");
            }

            //then check if connection is OK. TaksBar takes care of keeping it alive
            //in all times. If loginStatus != LoginSessionActive - there probably a big trouble
            else if (this.fmrConection.loginStatus != LoginStatus.LoginSessionActive)
            {
                iWrite.WriteLine(Environment.NewLine
                    + "WARNING! Your login status is "
                    + this.fmrConection.loginStatus.ToString()
                    + "Please check your connection!");
            }

            // check the entered arguments:
            else if (cmdArguments.Length > 3)
            {
                iWrite.WriteLine(Environment.NewLine + "Too many arguments. Try again");
            }

            //Check whether log directory exists
            else if (Resources.GetLogsFolder() == null)
            {
                iWrite.WriteLine("Can't perform the commmand. Define the logs directory.");
            }

            //this one with no args initializes all the three logs
            else if (cmdArguments.Length == 1)
            {
                LogMaof(iWrite);
                LogRezef(iWrite);
                LogMadad(iWrite);
            }
            else if (cmdArguments.Length == 2)   // start one specified log
            {
                switch (cmdArguments[1].ToString().ToLower())
                {
                    case "mf":
                        LogMaof(iWrite);
                        break;
                    case "rz":
                        LogRezef(iWrite);
                        break;
                    case "mdd":
                        LogMadad(iWrite);
                        break;
                    default:
                        iWrite.WriteLine(Environment.NewLine
                            + "Invalid data type argument '"
                            + cmdArguments[1].ToString()
                            + "'. Type 'startlog + (optional) MF|RZ|MDD'");
                        break;
                }
            }
            else if (cmdArguments.Length == 3)   // start a specified log
            {                                     // using playback data generator in simulation mode
                switch (cmdArguments[1].ToString().ToLower())
                {
                    case "mf":
                        LogMaof(iWrite, cmdArguments[2].ToString());
                        break;
                    default:
                        iWrite.WriteLine(Environment.NewLine
                            + "Invalid data type argument '"
                            + cmdArguments[1].ToString()
                            + "'. Type 'startlog + (optional) MF' + playbackLogName");
                        break;
                }
            }
        }

        protected void LogMaof(IWrite iWrite)
        {
            LogMaof(iWrite, null);
        }

        protected void LogMaof(IWrite iWrite, string backlog)
        {
            // generate filename and display it
            string filename = Resources.CreateLogFileName("MaofLog_", LogType.CSV);
            iWrite.WriteLine("Maof log file " + filename);

            OpenStreamAndLog(iWrite, false, FMRShell.DataType.Maof, filename, "MaofLogger", backlog);
        }

        protected void LogMadad(IWrite iWrite)
        {
            string filename = Resources.CreateLogFileName("MadadLog_", LogType.CSV);
            iWrite.WriteLine("Madad log file " + filename);

            OpenStreamAndLog(iWrite, false, FMRShell.DataType.Madad, filename, "MadadLogger");
        }

        protected void LogRezef(IWrite iWrite)
        {
            string filename = Resources.CreateLogFileName("RezefLog_", LogType.CSV);
            iWrite.WriteLine("Rezef log file " + filename);

            OpenStreamAndLog(iWrite, false, FMRShell.DataType.Rezef, filename, "RezefLogger");
        }

        protected void operStopLogCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            bool _stopStream = false;
            if (cmdArguments.Length == 1) iWrite.WriteLine("Please supply data type: MF | RZ | MDD");
            else if (cmdArguments.Length > 3) iWrite.WriteLine("Too many arguments");
            else
            {
                if (cmdArguments.Length == 3)
                {
                    _stopStream = (cmdArguments[2].ToString().ToLower() == "y");
                    Console.WriteLine(_stopStream);
                }
                switch (cmdArguments[1].ToString().ToLower())
                {
                    case "mf":
                        CloseLog(iWrite, FMRShell.DataType.Maof, _stopStream);
                        break;
                    case "rz":
                        CloseLog(iWrite, FMRShell.DataType.Rezef, _stopStream);
                        break;
                    case "mdd":
                        CloseLog(iWrite, FMRShell.DataType.Madad, _stopStream);
                        break;
                    default:
                        iWrite.WriteLine("Invalid data type parameter: " + cmdArguments[1].ToString());
                        break;
                }
            }
        }

        protected void StopStreamCallBack(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            if (cmdArguments.Length == 1) iWrite.WriteLine("Please supply data type: MF | RZ | MDD");
            else if (cmdArguments.Length > 2) iWrite.WriteLine("Too many arguments");
            else
            {
                switch (cmdArguments[1].ToString().ToLower())
                {
                    case "mf":
                        StopStream(iWrite, FMRShell.DataType.Maof);
                        break;
                    case "rz":
                        StopStream(iWrite, FMRShell.DataType.Rezef);
                        break;
                    case "mdd":
                        StopStream(iWrite, FMRShell.DataType.Madad);
                        break;
                    default:
                        iWrite.WriteLine("Invalid data type parameter: " + cmdArguments[1].ToString());
                        break;
                }
            }

        }

        protected void StopStream(IWrite iWrite, FMRShell.DataType dt)
        {
            DataCollector.Stop(dt);
        }

        protected void OpenStreamAndLog(IWrite iWrite, bool test, FMRShell.DataType dt, string filename, string loggerName)
        {
            // use NULL for backlogfile - in simulation mode data generator is random
            // 
            OpenStreamAndLog(iWrite, test, dt, filename, loggerName, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iWrite">
        /// A <see cref="IWrite"/>
        /// </param>
        /// <param name="test">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <param name="dt">
        /// A <see cref="FMRShell.DataType"/>
        /// </param>
        /// <param name="filename">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="loggerName">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="backlogfile">
        /// A <see cref="System.String"/>
        /// Applicable only in the simulation mode
        /// If the file is specified (not null) - play back the specified pre-recorded log file
        /// If null use randomly generated data
        /// </param>
        protected void OpenStreamAndLog(IWrite iWrite, bool test, FMRShell.DataType dt, string filename, string loggerName, string backlogfile)
        {
#if USEFMRSIM
            if (dt == FMRShell.DataType.Maof)
            {
                TaskBarLibSim.EventGenerator<K300MaofType> dataMaofGenerator;
                if (backlogfile == null)
                {
                    // create Maof data generator
                    dataMaofGenerator = new TaskBarLibSim.MaofDataGeneratorRandom();
                }
                else
                {
                    // create Maof data generator
                    dataMaofGenerator = new TaskBarLibSim.MaofDataGeneratorLogFile(backlogfile, 1, 0);
                }
                // setup the data generator(s) in the K300Class
                TaskBarLibSim.K300Class.InitStreamSimulation(dataMaofGenerator);
            }
            else if (dt == FMRShell.DataType.Rezef)
            {
                //create Rezef data generator
                TaskBarLibSim.RezefDataGeneratorRandom dataRzfGenerator = new TaskBarLibSim.RezefDataGeneratorRandom();
                TaskBarLibSim.K300Class.InitStreamSimulation(dataRzfGenerator);
            }
            else if (dt == FMRShell.DataType.Madad)
            {
                //create Madad data generator
                TaskBarLibSim.MadadDataGeneratorRandom dataMddGenerator = new TaskBarLibSim.MadadDataGeneratorRandom();
                TaskBarLibSim.K300Class.InitStreamSimulation(dataMddGenerator);
            }

            else
            {
                iWrite.WriteLine(Environment.NewLine + "Warning data type not supported in FMR simulation: " + dt.ToString() + Environment.NewLine);
            }
#endif

            // Check that there is no data collector created already
            Console.WriteLine("DT= " + dt.ToString() + " (" + (int)dt + ")");
            if (DataCollector == null)
            {
                // create Collector (producer) - will do it only once
                DataCollector = new FMRShell.Collector(this.fmrConection.GetSessionId());
            }

            // create logger which will register itself (AddSink) in the collector
            FMRShell.TradingDataLogger dataLogger = default(FMRShell.TradingDataLogger);
            if (dt == FMRShell.DataType.Maof)
            {
                dataLogger = new FMRShell.TradingDataLogger(loggerName, filename, false,
                DataCollector.maofProducer, new FMRShell.MarketDataMaof().Legend);
            }
            else if (dt == FMRShell.DataType.Rezef)
            {
                dataLogger = new FMRShell.TradingDataLogger(loggerName, filename, false,
                DataCollector.rezefProducer, new FMRShell.MarketDataRezef().Legend);
            }
            else if (dt == FMRShell.DataType.Madad)
            {
                dataLogger = new FMRShell.TradingDataLogger(loggerName, filename, false,
                DataCollector.madadProducer, new FMRShell.MarketDataMadad().Legend);
            }
            else System.Console.WriteLine("No handling for data type " + dt + "(" + (int)dt + ")");

            DataLogger[(int)dt] = dataLogger;

            // start logger
            dataLogger.Start();

            // start collector, which will start the stream in K300Class
            DataCollector.Start(dt);

            debugLoggerShowCallback(iWrite, "", null);

            if (test)
            {
                Thread.Sleep(1000);
                CloseLog(iWrite, dt, true);
            }
        }

        #endregion;

        #region Debug Callbacks

        static MarketSimulationMaof marketSimulationMaof;
        MaofDataGeneratorLogFile dataMaofGenerator;

		protected static void GetMatchGroups(string pattern, string text, out System.Text.RegularExpressions.GroupCollection groups, out int matchesCount)
		{
			System.Text.RegularExpressions.Regex regex;
			groups = null;
			matchesCount = 0;
            text = text.ToUpper();
			
			regex = new System.Text.RegularExpressions.Regex(pattern);
			System.Text.RegularExpressions.MatchCollection matches = regex.Matches(text);

			do
			{
				matchesCount = matches.Count;
				if (matchesCount < 1)
				{
					break;
				}
				
				// get groups
				System.Text.RegularExpressions.Match match = matches[0];
				groups = match.Groups;
			}
			while (false);
		}
		
		protected static bool FindSecurity(System.Collections.Generic.Dictionary<string, int> names, string putcall, string strike, string month, out int id)
		{
			id = 0;
			bool res = false;

			// generate MAOF style name - something like "P01080DEC"
			strike = JQuant.OutputUtils.FormatField(strike, 5, '0');
			month = month.ToUpper();
            putcall = putcall.Substring(0, 1);
            putcall = putcall.ToUpper();
			string name = putcall+strike+month;
			
			if (names.ContainsKey(name))
			{
				id = names[name];
				res = true;
			}
			
			return res;
		}
		
		/// <summary>
		/// This is what I get: 'T25 P01080 DEC9'
		/// This is what I return: 'P01080DEC' or null
		/// </summary>
		protected static string convertBnoName(string BNO_NAME_E)
		{
			const string patternOption = MarketSimulationMaof.BNO_NAME_PATTERN_OPTION;
            
			System.Text.RegularExpressions.GroupCollection groups;
			int matchesCount;
			GetMatchGroups(patternOption, BNO_NAME_E, out groups,  out matchesCount);
			string res = null;
			
			if (matchesCount == 1)
			{
				string putcall = groups[1].Captures[0].ToString();
				string strike = groups[2].Captures[0].ToString();
				string month = groups[3].Captures[0].ToString();
				
				res = putcall+strike+month;
				// System.Console.WriteLine("convertBnoName "+BNO_NAME_E+" to "+res);
			}
			else
			{
				// System.Console.WriteLine("Failed to parse BNO_NAME_E '"+BNO_NAME_E+"'");
			}
			
			return res;
		}
		
		/// <summary>
		/// This is a magic (aka Trust the force) method which goes through the 
		/// CLI command arguments and look for a ticker IDs. The emthod recognize 
		/// different formats of tickers. For example
		/// - the unique integer 80613003
		/// - partial integer 13003 (if unique)
		/// - description 'Call 1800 Nov'
		/// - description 'C1800Nov'
		/// - description 'Put1800 Nov'
		/// - full name 'TA9Z00960C'
		/// </summary>
		protected bool FindSecurity(string text, out int id)
		{
			id = 0;
			bool res = false;
			
			// get the list of securities
            int[] ids = marketSimulationMaof.GetSecurities();   
			// my key is name of the option and my value is unique option Id (integer)
			// On TASE ID is an integer
			System.Collections.Generic.Dictionary<string, int> names = new System.Collections.Generic.Dictionary<string, int>(ids.Length);
			// i need an array (string) of IDs to look for patial integer IDs
			System.Text.StringBuilder idNames = new System.Text.StringBuilder(ids.Length*10);			
			// fill the dictionary and string of all IDs
			foreach (int i in ids)
			{
				MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(i);
				string name = convertBnoName(option.GetName());
	            if (name != null) 
				{
					names.Add(name, i);
				}
				idNames.Append(i);idNames.Append(" ");
			}
			string idNamesStr = idNames.ToString();
			
			// look in the command for regexp jan|feb)($| +)' first
			// Other possibilities are: ' +([0-9]+) *([c,p]) *(jan|feb)($| +)'
			// the final case is any set of digits ' +([0-9]+)($| +)'
            const string monthPattern = "JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC";            
            const string putcallPattern = "c|p|C|P|call|put|CALL|PUT|Call|Put]";
			const string pattern1 = " +("+putcallPattern+") *([0-9]+) *("+monthPattern+")($| +)";
			const string pattern2 = " +([0-9]+) *("+putcallPattern+") *("+monthPattern+")($| +)";
			const string pattern3 = " +([0-9]+)($| +)";
			System.Text.RegularExpressions.GroupCollection groups;
			int matchesCount;
			
			do
			{
                GetMatchGroups(pattern1, text, out groups, out matchesCount);
				if (matchesCount > 1)
				{
					System.Console.WriteLine("I expect one and only one match for '"+pattern1+"' instead of "+matchesCount);
					break;
				}
				
				if (matchesCount == 1)
				{
					string putcall = groups[1].Captures[0].ToString(); // group[0] is reserved for the whole match
					string strike = groups[2].Captures[0].ToString();
					string month = groups[3].Captures[0].ToString();					
					res = FindSecurity(names, putcall, strike, month, out id);
					break;
				}
				
                GetMatchGroups(pattern2, text, out groups, out matchesCount);
				if (matchesCount > 1)
				{
					System.Console.WriteLine("I expect one and only one match for '"+pattern2+"' instead of "+matchesCount);
					break;
				}
				if (matchesCount == 1)
				{
					string strike = groups[2].Captures[0].ToString();  // group[0] is reserved for the whole match
					string putcall = groups[1].Captures[0].ToString();
					string month = groups[3].Captures[0].ToString();					
					res = FindSecurity(names, putcall, strike, month, out id);
					break;
				}

				// finally i look just for any sequence of digits
                GetMatchGroups(pattern3, text, out groups, out matchesCount);
				if (matchesCount > 0)
				{
					string digits = groups[0].Captures[0].ToString(); // group[0] is reserved for the whole match
					int idxFirst = idNamesStr.IndexOf(digits);        // idNamesStr is a string containing all existing Ids followed by blank
					int idxSecond = idNamesStr.LastIndexOf(digits);
					string firstMatch = idNamesStr.Substring(idxFirst, idNamesStr.IndexOf(" ", idxFirst+1)-idxFirst);
					string secondMatch = idNamesStr.Substring(idxSecond, idNamesStr.IndexOf(" ", idxSecond+1)-idxSecond);
					if (idxFirst != idxSecond)
					{
						System.Console.WriteLine("I have at least two matches '"+firstMatch+"' and '"+secondMatch+"'");
						break;
					}
                    // System.Console.WriteLine("firstMatch={0},secondMatch={1},digits={2}",firstMatch,secondMatch,digits);
					// i got a single match - convert to ID
					id = Int32.Parse(firstMatch);
					res = true;
					break;
				}
				
				res = false;
			}
			while (false);
			
			
			return res;
		}

        protected void debugMarketSimulationMaofCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            string cmd = "", arg1 = "", arg2 = "";
            switch (cmdArguments.Length)
            {
                case 0:
                case 1:
                    iWrite.WriteLine("Usage: maof create <backlogfile> [speedup] | stop | start");
                    break;

                case 2:
                    cmd = cmdArguments[1].ToString().ToLower();
                    break;

                case 3:
                    cmd = cmdArguments[1].ToString().ToLower();
                    arg1 = cmdArguments[2].ToString();
                    break;

                case 4:
                    cmd = cmdArguments[1].ToString().ToLower();
                    arg1 = cmdArguments[2].ToString();
                    arg2 = cmdArguments[3].ToString();
                    break;
            }
            
            if (cmd == "stop")
            {
                if (dataMaofGenerator!=default(MaofDataGeneratorLogFile))
                {
                    dataMaofGenerator.Stop();
                    dataMaofGenerator.RemoveConsumer(marketSimulationMaof);
				    marketSimulationMaof.Dispose();
                    marketSimulationMaof = default(MarketSimulationMaof);
                    dataMaofGenerator = default(MaofDataGeneratorLogFile);

                    iWrite.WriteLine("maof stop called");
                }
                else
                {
                    iWrite.WriteLine("No active simulation to stop.");
                }
            }        
            else if (cmd == "start") // log file name
            {
                if (this.dataMaofGenerator != default(MaofDataGeneratorLogFile))
                {
                    // call EventGenerator.Start() - start the data stream
                    dataMaofGenerator.Start();
				}
				else
				{
                    iWrite.WriteLine("Use 'create' first to create the market simulation");
				}
			}
            else if (cmd == "create") // log file name
            {
                string logfile = arg1;
                double speedup = JQuant.Convert.StrToDouble(arg2, 1.0);

                //if K300Class instance is not already initilazed, do it now
                if (this.dataMaofGenerator == default(MaofDataGeneratorLogFile))
                {
                    this.dataMaofGenerator =
                        new MaofDataGeneratorLogFile(logfile, speedup, 0);

                    //I need a cast here, because MarketSimulationMaof expects parameter of type IProducer
                    marketSimulationMaof = new MarketSimulationMaof();
                    dataMaofGenerator.AddConsumer(marketSimulationMaof);
					
//					marketSimulationMaof.EnableTrace(80608128, true);
//					marketSimulationMaof.EnableTrace(80616808, true);
                    iWrite.WriteLine("Use 'start' to start the market simulation");
                }
                else    //for the moment I don't want the mess of running multiple simulations simultaneously.
                {
                    iWrite.WriteLine("Maof simulation " + dataMaofGenerator.Name + "is already running.");
                    iWrite.WriteLine("Only a single simulation at a time is possible.");
                }
            }
        }

        protected void debugMarketSimulationMaofStatMaof(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
        }
        
        protected void debugMarketSimulationMaofStatCore(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int columnSize = 8;
            System.Collections.ArrayList names;
            System.Collections.ArrayList values;
            marketSimulationMaof.GetEventCounters(out names, out values);

            CommandLineInterface.printTableHeader(iWrite, names, columnSize);
            CommandLineInterface.printValues(iWrite, values, columnSize);
        }
        
        protected void debugMarketSimulationMaofStatBook(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int[] ids = marketSimulationMaof.GetSecurities();   //get the list of securities
			int[] columns = new int[0];
			bool firstLoop = true;

            System.Collections.ArrayList names = new System.Collections.ArrayList();
            names.Add("Name");

            System.Array.Sort(ids);

            foreach (int id in ids)
            {
				MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);
                System.Collections.ArrayList values = new System.Collections.ArrayList();

				
                JQuant.IResourceStatistics bids = marketSimulationMaof.GetOrderBook(id, JQuant.TransactionType.BUY);
                System.Collections.ArrayList bidValues;
                System.Collections.ArrayList bidNames;
                bids.GetEventCounters(out bidNames, out bidValues);

                JQuant.IResourceStatistics asks = marketSimulationMaof.GetOrderBook(id, JQuant.TransactionType.SELL);
                System.Collections.ArrayList askValues;
                System.Collections.ArrayList askNames;
                asks.GetEventCounters(out askNames, out askValues);

				// print table header if this is first loop
				if (firstLoop)
				{
					firstLoop = false;
					for (int i = 0;i < bidNames.Count;i++)
					{
						names.Add(bidNames[i]);
					}
					for (int i = 0;i < askNames.Count;i++)
					{
						names.Add(askNames[i]);
					}
		            columns = JQuant.ArrayUtils.CreateInitializedArray(6, names.Count);
		            columns[0] = 16;
		            columns[1] = 10;
		            columns[2] = 6;
		            columns[3] = 6;
		            columns[4] = 6;
		            columns[5] = 10;
		            columns[6] = 6;
		            columns[7] = 6;
		            columns[8] = 6;
		            CommandLineInterface.printTableHeader(iWrite, names, columns);
				}
				
                values.Add(option.GetName());
				for (int i = 0;i < bidValues.Count;i++)
				{
					values.Add(bidValues[i]);
				}
				for (int i = 0;i < askValues.Count;i++)
				{
					values.Add(askValues[i]);
				}

                CommandLineInterface.printValues(iWrite, values, columns);
            }
        }
        
        protected void debugMarketSimulationMaofStatQueue(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            iWrite.WriteLine("Not supported");
        }
        
        protected void debugMarketSimulationMaofStatCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            string cmd = "core";

            if (dataMaofGenerator == default(MaofDataGeneratorLogFile)) // check if there active simulation to get data from 
            {                                                           // to prevent System.NullReferenceException
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            
            if (cmdArguments.Length > 1)
            {
                cmd = (string)cmdArguments[1];
            }

            if (cmd == "maof")
            {
                debugMarketSimulationMaofStatMaof(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "core")
            {
                debugMarketSimulationMaofStatCore(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "book")
            {
                debugMarketSimulationMaofStatBook(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "queue")
            {
                debugMarketSimulationMaofStatQueue(iWrite, cmdName, cmdArguments);
            }
            else
            {
                iWrite.WriteLine("Only arguments maof, core, book, queue are supported");
            }
        }

        protected void debugMarketSimulationMaofSecsBook(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int[] ids = marketSimulationMaof.GetSecurities();   //get the list of securities
			
            System.Collections.ArrayList names = new System.Collections.ArrayList();
            names.Add("Id");
            names.Add("Name");
            names.Add("Bid:PriceVolume");
            names.Add("Ask:PriceVolume");

            int[] columns = JQuant.ArrayUtils.CreateInitializedArray(6, names.Count);
            columns[0] = 10;
            columns[1] = 16;
            columns[2] = 30;
            columns[3] = 30;
            
            CommandLineInterface.printTableHeader(iWrite, names, columns);

            System.Array.Sort(ids);

            foreach (int id in ids)
            {
				MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);
                System.Collections.ArrayList values = new System.Collections.ArrayList();
				
                MarketSimulation.OrderPair[] bids = marketSimulationMaof.GetOrderQueue(id, JQuant.TransactionType.BUY);
                MarketSimulation.OrderPair[] asks = marketSimulationMaof.GetOrderQueue(id, JQuant.TransactionType.SELL);

                values.Add(id);
                values.Add(option.GetName());
                values.Add(OrderBook2String(bids, 9));
                values.Add(OrderBook2String(asks, 9));

                CommandLineInterface.printValues(iWrite, values, columns);
            }
        }
        
		protected static string OrderPair2String(MarketSimulation.OrderPair op, int columnSize)
		{
		    string res = "" + op.price + ":" + op.size + " ";
			res = OutputUtils.FormatField(res, columnSize);
			return res;
		}
        
		protected static string OrderBook2String(MarketSimulation.OrderPair[] book, int columnSize)
		{
			string res = "";
			
			for (int i = 0;i < book.Length;i++)
			{
				MarketSimulation.OrderPair op = book[i];
				res = res + OrderPair2String(op, columnSize);
			}
			
			return res;
		}
		
		
        protected void debugMarketSimulationMaofSecsMaof(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int[] ids = marketSimulationMaof.GetSecurities();   //get the list of securities

            System.Collections.ArrayList names = new System.Collections.ArrayList();
            names.Add("Id");
            names.Add("Name");
            names.Add("Bid:PriceVolume");
            names.Add("Ask:PriceVolume");

            int[] columns = JQuant.ArrayUtils.CreateInitializedArray(6, names.Count);
            columns[0] = 10;
            columns[1] = 16;
            columns[2] = 30;
            columns[3] = 30;
            
            CommandLineInterface.printTableHeader(iWrite, names, columns);

            System.Array.Sort(ids);

            foreach (int id in ids)
            {
				MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);
                System.Collections.ArrayList values = new System.Collections.ArrayList();

                values.Add(id);
                values.Add(option.GetName());
                values.Add(OrderBook2String(option.GetBookBid(), 9));
                values.Add(OrderBook2String(option.GetBookAsk(), 9));

                CommandLineInterface.printValues(iWrite, values, columns);
            }
        }
        
		/// <summary>
		/// This method do two things 
		/// - get list of securities from the MarketSimulationMaof
		/// For every security ask MarketSimulation.Core what the Core thinks about it. 
		/// </summary>
        protected void debugMarketSimulationMaofSecsCore(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int[] ids = marketSimulationMaof.GetSecurities();   //get the list of securities

            System.Collections.ArrayList names = new System.Collections.ArrayList();
            names.Add("Id");
            names.Add("Name");
            names.Add("CoreId");
            names.Add("Bid:PriceVolume");
            names.Add("Ask:PriceVolume");
            names.Add("LastTrade");
            names.Add("LastTradeSize");
            names.Add("DayVolume");

            int[] columns = JQuant.ArrayUtils.CreateInitializedArray(6, names.Count);
            columns[0] = 9;
            columns[1] = 12;
            columns[2] = 9;
            columns[3] = 30;
            columns[4] = 30;
            
            CommandLineInterface.printTableHeader(iWrite, names, columns);

            System.Array.Sort(ids);

            foreach (int id in ids)
            {
				// i need MarketSimulationMaof.Option to show the name of the option
				// currently I take care only of options
				MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);			
				// get information kept in the MrketSimulation.Core
				MarketSimulation.MarketData md = marketSimulationMaof.GetSecurity(id);
                System.Collections.ArrayList values = new System.Collections.ArrayList();

                values.Add(id);
                values.Add(option.GetName());
                values.Add(md.id);
                values.Add(OrderBook2String(md.bid, 9));
                values.Add(OrderBook2String(md.ask, 9));
                values.Add(md.lastTrade);
                values.Add(md.lastTradeSize);
                values.Add(md.dayVolume);

                CommandLineInterface.printValues(iWrite, values, columns);
            }
        }
		
        protected void debugMarketSimulationMaofSecsQueue(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            if (marketSimulationMaof == default(MarketSimulationMaof)) // check if there active simulation to get data from 
            {                                                           
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            
            iWrite.WriteLine("Not supported");
        }
        
        protected void debugMarketSimulationMaofSecsCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            string cmd = "maof";

            if (dataMaofGenerator == default(MaofDataGeneratorLogFile)) // check if there active simulation to get data from 
            {                                                           // to prevent System.NullReferenceException
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            
            if (cmdArguments.Length > 1)
            {
                cmd = (string)cmdArguments[1];
            }

            if (cmd == "maof")
            {
                debugMarketSimulationMaofSecsMaof(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "core")
            {
                debugMarketSimulationMaofSecsCore(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "book")
            {
                debugMarketSimulationMaofSecsBook(iWrite, cmdName, cmdArguments);
            }
            else if (cmd == "queue")
            {
                debugMarketSimulationMaofSecsQueue(iWrite, cmdName, cmdArguments);
            }
            else
            {
                iWrite.WriteLine("Only arguments maof, core, book, queue are supported");
            }
        }

        protected void debugMarketSimulationMaofTraceCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            if (cmdArguments.Length < 2)
            {
                iWrite.WriteLine("Security ID is required");
				return;
            }
            if (marketSimulationMaof == default(MarketSimulationMaof))
            {
                iWrite.WriteLine("Create market simulation first");
				return;
			}
			
			int securityId = Convert.StrToInt((string)(cmdArguments[1]), 0);
			bool enable = false;
			
			if (cmdArguments.Length > 2)
			{
				enable = Boolean.Parse((string)(cmdArguments[2]));
			}
			else
			{
				enable = !(marketSimulationMaof.GetEnableTrace(securityId));
			}
			marketSimulationMaof.EnableTrace(securityId, enable);
		}


        public class WatchlistCallback
        {
            public WatchlistCallback(IWrite iWrite)
            {
                this.iWrite = iWrite;
            }
            
            /// <summary>
            /// called by MarketSimulation when a change is in the status of the security 
            /// </summary>
            public void callback(MarketSimulation.MarketData md)
            {
                int id = md.id;
                MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);
                string optionName = option.GetName();
    
                // print everything out - name, bids, asks
                MarketSimulation.OrderPair[] bids = md.bid;
                MarketSimulation.OrderPair[] asks = md.ask;
    
                System.Collections.ArrayList values = new System.Collections.ArrayList();
    
                values.Add(md.tick);
                values.Add(id);
                values.Add(optionName);
                values.Add(md.lastTrade);
                values.Add(md.dayVolume);
                values.Add(OrderBook2String(bids, 9));
                values.Add(OrderBook2String(asks, 9));
    
                CommandLineInterface.printValues(iWrite, values, this.columns);
            }

            public void printLegend()
            {
                System.Collections.ArrayList values = new System.Collections.ArrayList();
    
                values.Add("Tick");
                values.Add("Id");
                values.Add("Name");
                values.Add("LastTradeSize");
                values.Add("DayVolume");
                values.Add("Bids");
                values.Add("Asks");
                
                CommandLineInterface.printValues(iWrite, values, this.columns);
            }

            public void printList()
            {
                int[] list = marketSimulationMaof.WatchList();
                for (int i = 0;i < list.Length;i++)
                {
                    int id = list[i];
                    MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(id);
                    string optionName = option.GetName();
                    System.Collections.ArrayList values = new System.Collections.ArrayList();
                    values.Add(id);
                    values.Add(optionName);
                    CommandLineInterface.printValues(iWrite, values, this.columns);
                }
            }
            protected IWrite iWrite;
            protected int[] columns = {8, 10, 12, 6, 6, 30, 30};
        }

        protected WatchlistCallback watchlistCallback;
        protected void debugMarketSimulationMaofWatchCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
            if (marketSimulationMaof == default(MarketSimulationMaof)) // check if there active simulation to get data from 
            {                                                           
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            if (watchlistCallback == null) // create a watchlist callback in the first call
            {
                watchlistCallback = new WatchlistCallback(iWrite);
            }
            if (cmdArguments.Length == 2)
            {
                // argument is legend or list
                string legendList = cmdArguments[1].ToString();
                legendList = legendList.ToUpper();
                if (legendList.CompareTo("LEGEND") == 0)
                {
                    watchlistCallback.printLegend();
                }
                else if (legendList.CompareTo("LIST") == 0)
                {
                    watchlistCallback.printList();
                }
                else
                {
                    iWrite.WriteLine("Use commands legend or list");
                }
                return ;
            }
            if (cmdArguments.Length < 3)
            {
                iWrite.WriteLine("At least two arguments are required");
                return;
            }
            int id;
            bool res = FindSecurity(cmdName, out id);
            if (!res)
            {
                iWrite.WriteLine("Unknown security in the command "+cmdName);
                return;
            }

            // first argument is add or rmv
            string addRmv = cmdArguments[1].ToString();
            addRmv = addRmv.ToUpper();
            if (addRmv.CompareTo("ADD") == 0)
            {
                marketSimulationMaof.AddWatch(id, watchlistCallback.callback);
            }
            else
            {
                marketSimulationMaof.RemoveWatch(id);
            }
		}
		
        protected void placeOrderCallback(MarketSimulation.ReturnCode errorCode, MarketSimulation.ISystemLimitOrder lo, int quantity)
        {
            MarketSimulationMaof.Option option = marketSimulationMaof.GetOption(lo.SecurityId);
            string optionName = option.GetName();
            if (errorCode == MarketSimulation.ReturnCode.Fill)
            {
                System.Console.WriteLine("Tick {6} Order {0} {5} id {1} quantity {3} price {4} got fill at price {2}", 
                                         optionName, lo.Id, lo.FillPrice, quantity, lo.Price, lo.SecurityId, lo.FillTick);
            }
            else 
            {
                System.Console.WriteLine("Tick {5} Order {0} id {1} price {2} quantity {3} failed on {4}", 
                                         optionName, lo.Id, lo.Price, quantity, errorCode.ToString(), lo.FillTick);
            }
        }
        
        protected void debugMarketSimulationMaofPlaceOrderCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
            if (marketSimulationMaof == default(MarketSimulationMaof)) // check if there active simulation to get data from 
            {                                                           
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            
			int id;
			bool res = FindSecurity(cmdName, out id);
			if (!res)
			{
                iWrite.WriteLine("Unknown security in the command "+cmdName);
                return;
			}

            // i got security ID
            bool buyOrder = (cmdArguments[1].ToString().ToUpper().CompareTo("BUY") == 0);
            bool sellOrder = (cmdArguments[1].ToString().ToUpper().CompareTo("SELL") == 0);
            if (!buyOrder && !sellOrder)
            {
                iWrite.WriteLine("Use words buy or sell to specify the order type");
                return;
            }
            if (buyOrder && sellOrder)
            {
                iWrite.WriteLine("Internal error: both buy and sell in "+cmdArguments[1]);
                return;
            }


            // are there three and only three numbers in the command line ?
            const string patternNumbers = ".+[0-9]+.+[0-9]+ +[0-9]+";
            System.Text.RegularExpressions.Regex regexNumbers = new System.Text.RegularExpressions.Regex(patternNumbers);
            System.Text.RegularExpressions.MatchCollection matches = regexNumbers.Matches(cmdName);
            if (matches.Count != 1)
            {
                iWrite.WriteLine("Three and only three numbers - security ID, limit price and quantiy are allowed. I got '"+cmdName+"'");
                return;
            }

            // last arguments are price and quantity
            string limitPriceStr = cmdArguments[cmdArguments.Length-2].ToString();
            string quantintyStr = cmdArguments[cmdArguments.Length-1].ToString();
            int limitPrice = Int32.Parse(limitPriceStr);
            int quantity = Int32.Parse(quantintyStr);
            if (limitPrice == 0)
            {
                iWrite.WriteLine("Failed to parse limit price "+limitPriceStr);
                return;
            }
            if (quantity == 0)
            {
                iWrite.WriteLine("Failed to parse quantinty "+quantintyStr);
                return;
            }

            TransactionType transaction;
            if (buyOrder)
            {
                transaction = TransactionType.BUY;
            }
            else 
            {
                transaction = TransactionType.SELL;
            }
            MarketSimulation.ReturnCode errorCode;
            MarketSimulation.ISystemLimitOrder order = marketSimulationMaof.CreateOrder(id, limitPrice, quantity, transaction, placeOrderCallback);
            res = marketSimulationMaof.PlaceOrder(order, out errorCode);
            if (!res)
            {
                iWrite.WriteLine("Failed to place order error="+errorCode);
            }
		}
		
        protected void debugMarketSimulationMaofCancelOrderCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
		{
            if (marketSimulationMaof == default(MarketSimulationMaof)) // check if there active simulation to get data from 
            {                                                           
                iWrite.WriteLine("No active market simulations.");
                return;
            }
            
            iWrite.WriteLine("Not supported");
		}
		
		

        protected void GetSH161Data(IWrite iWrite, string filename)
        {
            SH161DataLogger dl = new SH161DataLogger(filename);
            dl.GetAndLogSH161Data(fmrConection.GetSessionId());
        }

        protected void debugOperationsLogMaofCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            // generate filename
            string filename = Resources.CreateLogFileName("MaofLog_", LogType.CSV);
            iWrite.WriteLine("Log file " + filename);
            OpenStreamAndLog(iWrite, false, FMRShell.DataType.Maof, filename, "MaofLogger");
        }

        /// <summary>
        /// i can support multiple data collectors and loggers. Because this is CLI i am going to assume
        /// that there is exactly one logger for one data collector. 
        /// </summary>
        protected FMRShell.Collector DataCollector;


        /// <summary>
        /// i can support multiple data collectors and loggers. Because this is CLI i am going to assume
        /// that there is exactly one logger for one data collector. 
        /// </summary>
        protected FMRShell.TradingDataLogger[] DataLogger = new FMRShell.TradingDataLogger[(int)FMRShell.DataType.Last];


        protected void debugOperatonsStopLogCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            //CloseDataStreamAndLog(iWrite, (FMRShell.DataType)cmdArguments[0]);    //an error here - invalid cast exception
            CloseLog(iWrite, FMRShell.DataType.Maof, false);
        }

        protected void CloseLog(IWrite iWrite, FMRShell.DataType dt, bool stopStream)
        {
            FMRShell.TradingDataLogger dataLogger = DataLogger[(int)dt];
            dataLogger.Stop();
            dataLogger.Dispose();
            dataLogger = null;
            if (stopStream)
            {
                StopStream(iWrite, dt);
            }
        }


        protected void feedGetToFileCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            feedGetSeriesCallback(iWrite, cmdName, cmdArguments, true);
        }

        protected void feedGetSeriesCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            feedGetSeriesCallback(iWrite, cmdName, cmdArguments, false);
        }

        protected void printIntStatisticsHeader(IWrite iWrite)
        {
            iWrite.WriteLine(OutputUtils.FormatField("Name", 8) +
                         OutputUtils.FormatField("Mean", 8) +
                         OutputUtils.FormatField("Ready", 8) +
                         OutputUtils.FormatField("Size", 8) +
                         OutputUtils.FormatField("Count", 8)
                        );
            iWrite.WriteLine("----------------------------------------------------------------");
        }

        protected void printIntStatistics(IWrite iWrite, IntStatistics statistics)
        {
            iWrite.WriteLine(OutputUtils.FormatField(statistics.Name, 8) +
                         OutputUtils.FormatField(statistics.Mean, 8) +
                         OutputUtils.FormatField(statistics.Full().ToString(), 8) +
                         OutputUtils.FormatField(statistics.Size, 8) +
                         OutputUtils.FormatField(statistics.Count, 8)
                        );
        }

        protected void printIntMaxMinHeader(IWrite iWrite)
        {
            iWrite.WriteLine(OutputUtils.FormatField("Name", 8) +
                         OutputUtils.FormatField("Max", 8) +
                         OutputUtils.FormatField("Min", 8) +
                         OutputUtils.FormatField("Ready", 8) +
                         OutputUtils.FormatField("Size", 8) +
                         OutputUtils.FormatField("Count", 8)
                        );
            iWrite.WriteLine("----------------------------------------------------------------");
        }

        protected void printIntMaxMin(IWrite iWrite, IntMaxMin maxMin)
        {
            iWrite.WriteLine(OutputUtils.FormatField(maxMin.Name, 8) +
                         OutputUtils.FormatField(maxMin.Max, 8) +
                         OutputUtils.FormatField(maxMin.Min, 8) +
                         OutputUtils.FormatField(maxMin.Full().ToString(), 8) +
                         OutputUtils.FormatField(maxMin.Size, 8) +
                         OutputUtils.FormatField(maxMin.Count, 8)
                        );
        }

        protected TA.PriceVolumeSeries FeedSeries;
        protected void feedGetSeriesCallback(IWrite iWrite, string cmdName, object[] cmdArguments, bool outputToFile)
        {
            int argsNum = cmdArguments.Length;
            string symbol = null;
            DateTime from = DateTime.Today - TimeSpan.FromDays(30);
            DateTime to = DateTime.Now;
            DateTime tmp;
            bool result = true;
            string[] args = (string[])cmdArguments;
            string filename = null;

            switch (argsNum)
            {
                case 1:
                    result = false;
                    break;
                case 2:
                    if (outputToFile)
                    {
                        result = false;
                    }
                    else
                    {
                        symbol = args[1];
                    }
                    break;
                case 3:
                    if (outputToFile)
                    {
                        symbol = args[1];
                        filename = args[2];
                    }
                    else
                    {
                        symbol = args[1];
                        result = DateTime.TryParse(args[2], out tmp);
                        if (result) from = tmp;
                    }
                    break;
                case 4:
                    if (outputToFile)
                    {
                        symbol = args[1];
                        filename = args[2];
                        result = DateTime.TryParse(args[3], out tmp);
                        if (result) from = tmp;
                    }
                    else
                    {
                        symbol = args[1];
                        result = DateTime.TryParse(args[2], out tmp);
                        if (result) from = tmp;
                        result = DateTime.TryParse(args[3], out tmp);
                        if (result) to = tmp;
                    }
                    break;
                case 5:
                default:
                    if (outputToFile)
                    {
                        symbol = args[1];
                        filename = args[2];
                        result = DateTime.TryParse(args[3], out tmp);
                        if (result) from = tmp;
                        result = DateTime.TryParse(args[4], out tmp);
                        if (result) to = tmp;
                    }
                    else
                    {
                        symbol = args[1];
                        result = DateTime.TryParse(args[2], out tmp);
                        if (result) from = tmp;
                        result = DateTime.TryParse(args[3], out tmp);
                        if (result) to = tmp;
                    }
                    break;
            }

            if (!result)
            {
                iWrite.WriteLine("Please, specify symbol, from and to date");
                return;
            }

            IDataFeed dataFeed = new FeedYahoo();
            result = dataFeed.GetSeries(from, to, new Equity(symbol), DataFeed.DataType.Daily, out FeedSeries);
            TA.PriceVolumeSeries series = FeedSeries;
            if (result)
            {
                System.IO.FileStream fileStream = null;
                iWrite.WriteLine("Parsed " + series.Data.Count + " entries");
                if (outputToFile)
                {
                    bool shouldClose = false;
                    try
                    {
                        fileStream = new System.IO.FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                        shouldClose = true;
                        StreamWriter streamWriter = new StreamWriter(fileStream);
                        streamWriter.Write(series.ToString(TA.PriceVolumeSeries.Format.Table));
                        streamWriter.Flush();
                        fileStream.Close();
                        shouldClose = false;
                    }
                    catch (IOException e)
                    {
                        iWrite.WriteLine(e.ToString());
                    }
                    if (shouldClose)
                    {
                        fileStream.Close();
                    }
                }
                else
                {
                    iWrite.WriteLine(series.ToString(TA.PriceVolumeSeries.Format.Table));
                }
            }
            else
            {
                iWrite.WriteLine("Failed to read data from server");
            }

        }

        protected void feedGetSeriesFromFileCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int argsNum = cmdArguments.Length;
            string[] args = (string[])cmdArguments;
//            string filename = "yahoo_feed_data.csv";
            string filename = "yahoo_feed_data_5y.csv";
            switch (argsNum)
            {
                case 2:
                    filename = args[1];
                    break;
                default:
                    break;
            }

            if (filename == null)
            {
                iWrite.WriteLine("Filename is not specified");
            }
            else
            {
                IDataFeed dataFeed = new FeedYahoo();
                bool result = dataFeed.GetSeries(filename, out FeedSeries);
                TA.PriceVolumeSeries series = FeedSeries;
                if (result)
                {
                    iWrite.WriteLine("Parsed " + series.Data.Count + " entries");
                }
                else
                {
                    iWrite.WriteLine("Failed to read data from server");
                }
            }
        }

        protected void feedFisherTransformCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            TA.PriceVolumeSeries series = FeedSeries;

            // first step is to calculate max, min, average etc.
            series.CalculateParams();
            iWrite.WriteLine("Series: count="+series.Data.Count+", max="+series.Max+", min="+series.Min+", average="+series.Average+", sd="+series.StdDeviation);

            double average, max, min;
            
            double[] data = TA.PriceVolumeSeries.GetClose(series);
            TA.PriceVolumeSeries.CalculateAverage(data, 0, data.Length, out average, out max, out min);
            iWrite.WriteLine("Data: count="+data.Length+",max="+series.Max+", min="+series.Min+", average="+series.Average+", sd="+series.StdDeviation);

            int windowSize = 10;
            // now normalize the data
            data = TA.PriceVolumeSeries.Normalize(data, windowSize);
            TA.PriceVolumeSeries.CalculateAverage(data, 0, data.Length, out average, out max, out min);
            iWrite.WriteLine("Normalize(data): count="+data.Length+",max="+max+", min="+min+", average="+average);
            
            TA.PriceVolumeSeries.EMA(data, 0.5);
            TA.PriceVolumeSeries.CalculateAverage(data, 0, data.Length, out average, out max, out min);
            iWrite.WriteLine("EMA(Normalize(data)): count="+data.Length+",max="+max+", min="+min+", average="+average);
            
            TA.PriceVolumeSeries.Fisher(data);
            TA.PriceVolumeSeries.CalculateAverage(data, 0, data.Length, out average, out max, out min);
            iWrite.WriteLine("Fisher(EMA(Normalize(data))): count="+data.Length+",max="+max+", min="+min+", average="+average);

            TA.PriceVolumeSeries.EMA(data, 0.5);
            TA.PriceVolumeSeries.CalculateAverage(data, 0, data.Length, out average, out max, out min);
            iWrite.WriteLine("EMA(Fisher(EMA(Normalize(data)))): count="+data.Length+",max="+max+", min="+min+", average="+average);

            int i;
            for (i = 0;i < data.Length;i++)
            {
//                iWrite.WriteLine(""+data[i]);
            }

            signalPerformanceOptimization(series, data, windowSize);
        }

        protected class Trade
        {
            public double entry;
            public TA.Candle candleEntry;
            public double exit;
            public TA.Candle candleExit;
            public bool isBuy;
            public double p;
            public int days;
            public int idx;
        }
        protected class TradeSession
        {
            public double p;
            public int days;
            public int hits;
            public double maxDrawDown;
            public double stopLoss;
            public double sellSignal;
            public double buySignal;
            public int maxDays;

            public System.Collections.Generic.List<Trade> trades;
        }
        
        protected void signalPerformanceOptimization(TA.PriceVolumeSeries series, double[] data, int windowSize)
        {
            double stopLossFrom = 0.02;
            double stopLossTo = 0.04;
            double stopLossStep = 0.001;
            double buySignalFrom = -4;;
            double buySignalTo = -1.4;
            double sellSignalFrom = 4;
            double sellSignalTo = 1.4;
            double signalStep = 0.01;
            int maxDays = 1000;
            long loopsTotal = (long)(((buySignalTo-buySignalFrom)/signalStep) * ((sellSignalFrom-sellSignalTo)/signalStep) * ((stopLossTo-stopLossFrom)/stopLossStep)  );
            
            System.Collections.Generic.List<TradeSession> bestBlocks = new System.Collections.Generic.List<TradeSession>(40);
            
            double buySignal = buySignalFrom;
            while (buySignal < buySignalTo)
            {
                double sellSignal = sellSignalFrom;
                while (sellSignal > sellSignalTo)
                {
                    double stopLoss = stopLossFrom;
                    while (stopLoss < stopLossTo)
                    {
                        signalPerformanceOptimization(series, data, windowSize, stopLoss, buySignal, sellSignal, maxDays, bestBlocks);
                        stopLoss += stopLossStep;
                        loopsTotal--;
                        if ((loopsTotal & 0xFFFF) == 0) System.Console.Write("."+loopsTotal);
                    }
                    sellSignal -= signalStep;
                }
                buySignal += signalStep;               
            }

            TradeSession bs = findBest(bestBlocks);
            System.Console.WriteLine("pTotal="+(int)(100*bs.p)+", days="+bs.days+
                                      ", hits="+bs.hits+", maxDrawDown="+(int)(100*bs.maxDrawDown)+""+
                                         ", trades="+bs.trades.Count+", stopLoss="+bs.stopLoss+", maxDays="+bs.maxDays+
                                         ", sellSig="+bs.sellSignal+", buySig="+bs.buySignal+", wind="+windowSize);
            signalPerformancePrintTrades(bs.trades);
                
        }
        protected void _signalPerformanceOptimization(TA.PriceVolumeSeries series, double[] data, int windowSize)
        {
            double stopLossStep = 0.001;
            
            System.Collections.Generic.List<TradeSession> bestBlocks = new System.Collections.Generic.List<TradeSession>(40);

            double buySignal = -1.94;
            double sellSignal = 1.7;
            double stopLoss = 0.026;
            signalPerformanceOptimization(series, data, windowSize, stopLoss, buySignal, sellSignal, 1000, bestBlocks);

            TradeSession bs = findBest(bestBlocks);
            System.Console.WriteLine("pTotal="+(int)(100*bs.p)+", days="+bs.days+
                                      ", hits="+bs.hits+", maxDrawDown="+(int)(100*bs.maxDrawDown)+""+
                                         ", trades="+bs.trades.Count+", stopLoss="+bs.stopLoss+", maxDays="+bs.maxDays+
                                         ", sellSig="+bs.sellSignal+", buySig="+bs.buySignal+", wind="+windowSize);
            signalPerformancePrintTrades(bs.trades);
                
        }

        protected TradeSession findBest(System.Collections.Generic.List<TradeSession> sessions)
        {
            TradeSession bestSession = null;
            double p = Double.MinValue;
            double maxDrawDown = Double.MaxValue;
            int hits = Int32.MinValue;
            int trades = Int32.MinValue;
            foreach (TradeSession s in sessions)
            {
                if (s.p > p)
                {
                    p = s.p;
                    maxDrawDown = s.maxDrawDown;
                    bestSession = s;
                    trades = s.trades.Count;
                }
            }

            return bestSession;
        }

        protected static void signalPerformancePrintTrades(System.Collections.Generic.List<Trade> trades)
        {
            foreach (Trade t in trades)
            {
                string buy = "Buy:";
                if (!t.isBuy) buy = "Sell:";
                System.Console.WriteLine(buy+" entry="+t.entry+" exit="+t.exit+" days="+t.days+
                                         " p="+(int)(100*t.p)+" idx="+t.idx);
                System.Console.WriteLine("\tEntry:"+t.candleEntry.ToString());
                System.Console.WriteLine("\tExit:"+t.candleExit.ToString());
                System.Console.WriteLine(" ");
            }
        }
        
        protected static void signalPerformanceGetTrades(System.Collections.Generic.List<Trade> trades, out int days, out int hits, out double p, out double maxDrawDown)
        {
            p = 1.0;
            days = 0;
            hits = 0;
            maxDrawDown = Double.MaxValue;
            
            foreach (Trade t in trades)
            {
                days += t.days;
                p = p * (1+t.p);
                if (t.p > 0) hits++;
                if (t.p < maxDrawDown) maxDrawDown = t.p;
            }
        }
        
        protected void signalPerformanceOptimization(TA.PriceVolumeSeries series, double[] data, int windowSize, 
                                                     double stopLoss,
                                                     double buySignal, double sellSignal, int maxDays,
                                                     System.Collections.Generic.List<TradeSession> bestBlocks)
        {
            Trade trade;
            System.Collections.Generic.List<Trade> trades = new System.Collections.Generic.List<Trade>(10);
            double diff = (1+0.002);
            int i = 0;
            while (i < (data.Length-1))
            {
                int idx = i + windowSize;
                TA.Candle candle = (TA.Candle)series.Data[idx];
                double p;
                int days;
                if ((data[i] > sellSignal) && (data[i] > data[i+1]*diff))  // sell condition and trigger
                {
                    signalPerformance(series, stopLoss, maxDays, idx, false, out trade);
                    if (trades.Count < 200) trades.Add(trade);
                    else break;
                    i += Math.Max(1, trade.days);
//                        System.Console.Write("\tSell at "+idx+" entry="+trade.entry+" exit="+trade.exit+" "+candle.ToString());
//                        System.Console.WriteLine(" p="+trade.p+", days="+trade.days+", exit at "+(idx+trade.days));
                }
                else if ((data[i] < buySignal) && (data[i]*diff < data[i+1]))  // buy condition and trigger
                {
                    signalPerformance(series, stopLoss, maxDays, idx, true, out trade);
                    if (trades.Count < 200) trades.Add(trade);
                    else break;
                    i += Math.Max(1, trade.days);
//                        System.Console.Write("\tBuy at "+idx+" entry="+trade.entry+" exit="+trade.exit+" "+candle.ToString());
//                        System.Console.WriteLine(" p="+trade.p+", days="+trade.days+", exit at "+(idx+trade.days));
                }
                else i++;
            }

            double pTotal;
            int daysTotal;
            int hits;
            double maxDrawDown;
            signalPerformanceGetTrades(trades, out daysTotal, out hits, out pTotal, out maxDrawDown);
            int misses = trades.Count-hits;
            if ((pTotal > 1.1) && (trades.Count > 5) && (bestBlocks.Count < 1000))
            {
                TradeSession ts = new TradeSession();
                ts.trades = trades;
                ts.maxDays = maxDays;
                ts.p = pTotal;
                ts.days = daysTotal;
                ts.hits = hits;
                ts.stopLoss = stopLoss;
                ts.sellSignal = sellSignal;
                ts.buySignal = buySignal;
                ts.maxDays = maxDays;
                ts.maxDrawDown = maxDrawDown;
                bestBlocks.Add(ts);
                System.Console.Write("+");
//                System.Console.WriteLine("p="+pTotal);
//                signalPerformancePrintTrades(trades);
            }
            if ((pTotal > 2) && (trades.Count > 5) && (bestBlocks.Count > 1000))
               System.Console.Write("-");
//            System.Console.Write("1");
        }

        protected bool signalStop(bool isBuy, double stopLoss, int maxDays, double entry, double last, double current, int days, double bestClose)
        {
            double deltaTrade = (bestClose-current)/bestClose;
            double deltaCandle = (last-current)/last;

            if (isBuy)
            {
                deltaTrade = -deltaTrade;
                deltaCandle = -deltaCandle;
            }

            bool res = false;
            double stopLoss_1 = -stopLoss;
            double stopLoss_2 = -2*stopLoss;
            res = res || (deltaTrade < stopLoss_2);
            res = res || (deltaCandle < stopLoss_1);
//            res = res || (days > maxDays);

            return res;            
        }
        
        protected void signalPerformance(TA.PriceVolumeSeries series, double stopLoss, int maxDays, int idx, bool isBuy, out Trade trade)
        {
            trade = new Trade();
            trade.isBuy = isBuy;
            trade.idx = idx;
            
            int count = series.Data.Count;
            TA.Candle candle = (TA.Candle)series.Data[idx];
            trade.candleEntry = candle;
            double entryPoint = candle.close;
            double close = entryPoint;
            double bestClose = close;
            bool isSell = !isBuy;
            int i;
            int days = 0;
            for (i = idx+1;i < count;i++)
            {
                candle = (TA.Candle)series.Data[i];
                days++;
                if (signalStop(isBuy, stopLoss, maxDays, entryPoint, close, candle.close, days, bestClose))
                {
                    break;
                }
                close = candle.close;
                if (isBuy)
                    if (bestClose < close) bestClose = close;
                else
                    if (bestClose > close) bestClose = close;
            }
            double delta;
            delta = entryPoint - candle.close;
            if (isBuy) delta = -delta;

            double p = delta/entryPoint;
            trade.candleExit = candle;
            trade.entry = entryPoint;
            trade.exit = candle.close;
            trade.p = p;
            trade.days = days;            
        }
        




        #endregion;

        #region Load Commands

        protected void LoadCommandLineInterface()
        {
            #region Main CLI menu

            cli.SystemMenu.AddCommand("exit", "Exit from the program",
                "Cleanup and exit", this.CleanupAndExit);
            // Menu menuFMRLib = 
            cli.RootMenu.AddMenu("FMRLib", "Access to  FMRLib API",
                          " Allows to access the FMRLib API directly");
            // Menu menuFMRLibSim = 
            cli.RootMenu.AddMenu("FMRLibSim", "Configure FMR simulation",
                           " Condiguration and debug of the FMR simulatoion");

            #endregion;

            LoadCommandLineInterface_oper();
            LoadCommandLineInterface_dbg();
            LoadCommandLineInterface_test();
            LoadCommandLineInterface_ms();
            LoadCommandLineInterface_feed();
        }

        #endregion


    }//partial class Program
}//namespace JQuant
