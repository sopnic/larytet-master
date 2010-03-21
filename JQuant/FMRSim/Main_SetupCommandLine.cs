
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
