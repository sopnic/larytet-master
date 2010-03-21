
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

        protected void debugLoginCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            //Tell where the xml file is (see also howto in Main(...) in Main.cs ):
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

        protected void debugLogSH161DataCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            string filename = Resources.CreateLogFileName("sh161_", LogType.CSV);
            iWrite.WriteLine("SH161 Log File: " + filename);
            GetSH161Data(iWrite, filename);
        }

        protected void debugGetAS400DTCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int ltncy;
            DateTime dt;

            //ping every 2 seconds, 60 times, write the output to the console
            //TODO - write to a file instead of console, make it a separate low priority thread
            for (int i = 0; i < 60; i++)
            {
                FMRShell.AS400Synch.Ping(out dt, out ltncy);
                Console.WriteLine(FMRShell.AS400Synch.ToShortCSVString(dt, ltncy));
                Thread.Sleep(2000);
            }
        }

        protected void debugFMRPingCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int argsNum = cmdArguments.Length;
            string[] args = (string[])cmdArguments;
            FMRShell.FMRPing fmrPing = FMRShell.FMRPing.GetInstance();

            switch (argsNum)
            {
                case 1:
                    fmrPing.Start();
                    iWrite.WriteLine("FMRPing started");
                    break;

                default:
                    string arg = args[1];
                    if (arg.Equals("login"))
                    {
                        fmrPing.SendLogin();
                        iWrite.WriteLine("FMRPing Login");
                    }
                    if (arg.Equals("logout"))
                    {
                        fmrPing.SendLogout();
                        iWrite.WriteLine("FMRPing Logout");
                    }
                    if (arg.Equals("stat"))
                    {
                        iWrite.WriteLine("Failed " + fmrPing.CountPingFailed + " from " + (fmrPing.CountPingOk + fmrPing.CountPingFailed));
                        printIntStatisticsHeader(iWrite);
                        printIntStatistics(iWrite, fmrPing.Statistics2min);
                        printIntStatistics(iWrite, fmrPing.Statistics10min);
                        printIntStatistics(iWrite, fmrPing.Statistics1hour);
                        iWrite.WriteLine();
                        printIntMaxMinHeader(iWrite);
                        printIntMaxMin(iWrite, fmrPing.MaxMin2min);
                        printIntMaxMin(iWrite, fmrPing.MaxMin10min);
                        printIntMaxMin(iWrite, fmrPing.MaxMin1hour);
                    }
                    if (arg.Equals("kill"))
                    {
                        fmrPing.Dispose();
                    }
                    break;
            }
        }

        protected void debugThreadPoolTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int maxJobs = 5;
            JQuant.ThreadPool threadPool = new JQuant.ThreadPool("test", 1, maxJobs, ThreadPriority.Lowest);

            threadpoolTestTicks = new long[maxJobs];
            long tick = DateTime.Now.Ticks;
            for (int i = 0; i < maxJobs; i++)
            {
                threadpoolTestTicks[i] = tick;
            }

            for (int i = 0; i < maxJobs; i++)
            {
                threadPool.PlaceJob(ThreadPoolJobEnter, ThreadPoolJobDone, i);
            }
            Thread.Sleep(500);

            debugThreadPoolShowCallback(iWrite, cmdName, cmdArguments);
            threadPool.Dispose();

            for (int i = 0; i < threadpoolTestTicks.Length; i++)
            {
                iWrite.WriteLine("ThreadPoolJob done  idx =" + i + ", time = " + (double)threadpoolTestTicks[i] / (double)(10 * 1) + " micros");
            }

        }

        protected void debugThreadPoolShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugPrintResourcesNameAndStats(iWrite, Resources.ThreadPools);
        }


        protected void debugOrderShowCallback()
        {
            OrderType orderType = (OrderType.LMT | OrderType.FOK | OrderType.IOC);
        }

        protected void debugPrintResourcesNameAndStats(IWrite iWrite, System.Collections.ArrayList list)
        {
            int entry = 0;
            int columnSize = 8;

            bool isEmpty = true;

            iWrite.WriteLine();

            foreach (INamedResource resNamed in list)
            {
                isEmpty = false;

                IResourceStatistics resStat = (IResourceStatistics)resNamed;

                System.Collections.ArrayList names;
                System.Collections.ArrayList values;
                resStat.GetEventCounters(out names, out values);

                if (entry == 0)
                {
                    names.Insert(0, "Name");
                    CommandLineInterface.printTableHeader(iWrite, names, columnSize);
                }
                values.Insert(0, OutputUtils.FormatField(resNamed.Name, columnSize));
                CommandLineInterface.printValues(iWrite, values, columnSize);

                entry++;

            }
            if (isEmpty)
            {
                System.Console.WriteLine("Table is empty - no resources registered");
            }
        }

        protected void debugVerifierShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            System.Collections.ArrayList names;
            System.Collections.ArrayList values;
            int entry = 0;
            int columnSize = 12;

            bool isEmpty = true;

            iWrite.WriteLine();

            foreach (IResourceDataVerifier verifier in Resources.Verifiers)
            {
                verifier.GetEventCounters(out names, out values);
                isEmpty = false;

                if (entry == 0)
                {
                    names.Insert(0, "Name");
                    CommandLineInterface.printTableHeader((JQuant.IWrite)this, names, columnSize);
                }
                values.Insert(0, OutputUtils.FormatField(verifier.Name, columnSize));
                CommandLineInterface.printValues((JQuant.IWrite)this, values, columnSize);

                entry++;

            }
            if (isEmpty)
            {
                iWrite.WriteLine("No verifiers");
            }

        }


        protected void debugPoolShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugPrintResourcesNameAndStats(iWrite, Resources.Pools);
        }


        protected void debugLogoutCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            if (fmrConection != null)
            {
                int s = fmrConection.GetSessionId();
                this.fmrConection.Dispose();
                fmrConection = null;  //set connection to null
                Console.WriteLine("Session with id " + s + " was terminated.");
            }
            else
            {
                Console.WriteLine("There is no active connection - you're not logged in.");
            }
        }


        protected void debugLoggerShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            iWrite.WriteLine(
                OutputUtils.FormatField("Name", 10) +
                OutputUtils.FormatField("Triggered", 10) +
                OutputUtils.FormatField("Logged", 10) +
                OutputUtils.FormatField("Dropped", 10) +
                OutputUtils.FormatField("Log type", 10) +
                OutputUtils.FormatField("Latest", 24) +
                OutputUtils.FormatField("Oldest", 24) +
                OutputUtils.FormatField("Stamped", 10)
            );
            iWrite.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
            bool isEmpty = true;
            foreach (ILogger logger in Resources.Loggers)
            {
                isEmpty = false;
                iWrite.WriteLine(
                    OutputUtils.FormatField(logger.GetName(), 10) +
                    OutputUtils.FormatField(logger.GetCountTrigger(), 10) +
                    OutputUtils.FormatField(logger.GetCountLog(), 10) +
                    OutputUtils.FormatField(logger.GetCountDropped(), 10) +
                    OutputUtils.FormatField(logger.GetLogType().ToString(), 10) +
                    OutputUtils.FormatField(logger.GetLatest().ToString(), 24) +
                    OutputUtils.FormatField(logger.GetOldest().ToString(), 24) +
                    OutputUtils.FormatField(logger.TimeStamped().ToString(), 10)
                );

            }
            if (isEmpty)
            {
                iWrite.WriteLine("No loggers");
            }
        }


        protected void debugProducerShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            System.Collections.ArrayList names;
            System.Collections.ArrayList values;
            int entry = 0;
            int columnSize = 12;

            bool isEmpty = true;

            iWrite.WriteLine();

            foreach (IResourceProducer producer in Resources.Producers)
            {
                producer.GetEventCounters(out names, out values);
                isEmpty = false;

                if (entry == 0)
                {
                    names.Insert(0, "Name");
                    CommandLineInterface.printTableHeader((JQuant.IWrite)this, names, columnSize);
                }
                values.Insert(0, OutputUtils.FormatField(producer.Name, columnSize));
                CommandLineInterface.printValues((JQuant.IWrite)this, values, columnSize);

                entry++;

            }
            if (isEmpty)
            {
                iWrite.WriteLine("No producers");
            }

        }

        protected void LoadCommandLineInterface_dbg()
        {
            Menu menuDebug = cli.RootMenu.AddMenu("Dbg", "System debug info",
                                   " Created objetcs, access to the system statistics");

            menuDebug.AddCommand("loginTest",
                                    "Run simple test of the login",
                                    " Create a FMRShell.Connection(xmlfile) and call Open()", debugLoginCallback);

            menuDebug.AddCommand("sh161",
                                    "Get TA25 Index weights",
                                    "Get TA25 Index weights",
                                    debugLogSH161DataCallback
                                    );
            menuDebug.AddCommand("AS400TimeTest",
                                    "ping the server",
                                    "ping AS400 server in order to get latency and synchronize local amachine time with server's",
                                    debugGetAS400DTCallback);
            menuDebug.AddCommand("fmrPing",
                                    "Start FMR ping thread",
                                    " Ping AS400 server continuosly [login|logout|stat|kill]",
                                    debugFMRPingCallback);

            menuDebug.AddCommand("threadPoolShow",
                                    "Show thread pools",
                                    " List of created thread pools",
                                    debugThreadPoolShowCallback
                                    );
            menuDebug.AddCommand("timerShow",
                                    "Show timers",
                                    " List of created timers and timer tasks",
                                    debugTimerShowCallback
                                    );
            menuDebug.AddCommand("threadShow",
                                    "Show threads",
                                    " List of created threads and thread states",
                                    debugThreadShowCallback);
            menuDebug.AddCommand("mbxShow",
                                    "Show mailboxes",
                                    " List of created mailboxes with the current status and statistics",
                                    debugMbxShowCallback
                                    );
            menuDebug.AddCommand("poolShow",
                                    "Show pools",
                                    " List of created pools with the current status and statistics",
                                    debugPoolShowCallback
                                    );
#if USEFMRSIM
            menuDebug.AddCommand("loggerTest",
                                    "Run simple test of the logger",
                                    " Create a Collector and start a random data simulator",
                                    debugLoggerTestCallback
                                    );
#endif
            menuDebug.AddCommand("loggerShow",
                                    "Show existing loggers",
                                    " List of created loggers with the statistics",
                                    debugLoggerShowCallback
                                    );
            menuDebug.AddCommand("prodShow",
                                    "Show producers",
                                    " List of created producers",
                                    debugProducerShowCallback
                                    );
            menuDebug.AddCommand("veriShow",
                                    "Show data verifiers",
                                    " List of created data verifiers",
                                    debugVerifierShowCallback
                                    );
        }
        
    }
}
