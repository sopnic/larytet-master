
using System;
using System.Threading;
using System.Collections.Generic;

namespace JQuant
{


    partial class Program
    {

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


        protected void debugMbxShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugPrintResourcesNameAndStats(iWrite, Resources.Mailboxes);
        }

        protected void debugMbxTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            Mailbox<bool> mbx = new Mailbox<bool>("TestMbx", 2);

            iWrite.WriteLine("TestMbx created");
            bool message = true;
            bool result = mbx.Send(message);
            if (!result)
            {
                iWrite.WriteLine("Mailbox.Send returned false");
            }
            else
            {
                iWrite.WriteLine("Mailbox.Send message sent");
            }
            result = mbx.Receive(out message);
            if (!result)
            {
                iWrite.WriteLine("Mailbox.Receive returned false");
            }
            else
            {
                iWrite.WriteLine("Mailbox.Send message received");
            }
            if (!message)
            {
                iWrite.WriteLine("I did not get what i sent");
            }
            debugMbxShowCallback(iWrite, cmdName, cmdArguments);

            mbx.Dispose();

            System.GC.Collect();
        }


        protected void debugThreadTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugThreadShowCallback(iWrite, cmdName, cmdArguments);
            MailboxThread<bool> thr = new MailboxThread<bool>("TestMbx", 2);
            debugThreadShowCallback(iWrite, cmdName, cmdArguments);
            thr.Start();
            debugThreadShowCallback(iWrite, cmdName, cmdArguments);
            bool message = true;
            bool result = thr.Send(message);
            if (!result)
            {
                iWrite.WriteLine("Thread.Send returned false");
            }
            thr.Stop();
            debugThreadShowCallback(iWrite, cmdName, cmdArguments);

            System.GC.Collect();
        }


        protected void debugThreadShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            iWrite.WriteLine();
            iWrite.WriteLine(
                OutputUtils.FormatField("Name", 10) +
                OutputUtils.FormatField("State", 14) +
                OutputUtils.FormatField("Ticks", 10)
            );
            iWrite.WriteLine("-------------------------------------------");
            bool isEmpty = true;
            foreach (IThread iThread in Resources.Threads)
            {
                isEmpty = false;
                iWrite.WriteLine(
                    OutputUtils.FormatField(iThread.Name, 10) +
                    OutputUtils.FormatField(EnumUtils.GetDescription(iThread.GetState()), 14) +
                    OutputUtils.FormatField(iThread.GetLongestJob(), 10)
                );

            }
            if (isEmpty)
            {
                iWrite.WriteLine("No threads");
            }
            int workerThreads;
            int completionPortThreads;
            System.Threading.ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            iWrite.WriteLine("workerThreads=" + workerThreads + ",completionPortThreads=" + completionPortThreads);

        }

        protected void debugGcCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            iWrite.WriteLine("Garbage collection done");
        }

        protected void debugPoolShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugPrintResourcesNameAndStats(iWrite, Resources.Pools);
        }

        protected void debugLoginCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            FMRShell.Connection connection = new FMRShell.Connection("ConnectionParameters.xml");

            bool openResult;
            int errResult;
            openResult = connection.Open(iWrite, out errResult, true);

            iWrite.WriteLine("");
            if (openResult)
            {
                iWrite.WriteLine("Connection openned for " + connection.GetUserName());
                iWrite.WriteLine("errResult=" + errResult);
            }
            else
            {
                iWrite.WriteLine("Connection failed errResult=" + errResult);
                iWrite.WriteLine("Error description: " + connection.LoginErrorDesc());
            }

            iWrite.WriteLine("Login status is " + connection.loginStatus.ToString());

            // final cleanup
            connection.Dispose();
        }

        protected void debugPoolTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            Pool<bool> pool = new Pool<bool>("TestPool", 2);

            bool message1 = true;
            bool message2 = false;
            pool.Fill(message1); pool.Fill(message2);

            bool result = pool.Get(out message1);
            if (!result)
            {
                iWrite.WriteLine("Pool.Get returned false");
            }
            if (message1)
            {
                iWrite.WriteLine("I did not get what i stored");
            }
            pool.Free(message1);
            debugPoolShowCallback(iWrite, cmdName, cmdArguments);

            pool.Dispose();

            System.GC.Collect();
        }

        protected void debugGetAS400DTCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            iWrite.WriteLine("Latency is " + FMRShell.AS400Synch.GetLatency().ToString());
            iWrite.WriteLine("Time is " + FMRShell.AS400Synch.GetAS400DateTime().ToString("hh:mm:ss.fff"));
            iWrite.WriteLine("Now pinging 30 times...");

            int ltncy;
            DateTime dt;
            System.Collections.Generic.List<double> lst = new System.Collections.Generic.List<double>(30);
            for (int i = 0; i < 30; i++)
            {
                FMRShell.AS400Synch.Ping(out dt, out ltncy);
                lst.Add(Convert.ToDouble(ltncy));
                Thread.Sleep(500);
                iWrite.Write(".");
            }
            string M = Math.Round(Convert.ToDecimal(StatUtils.Mean(lst)), 2).ToString();
            string SD = Math.Round(Convert.ToDecimal(StatUtils.StdDev(lst)), 2).ToString();
            string Min = StatUtils.Min(lst).ToString();
            string Max = StatUtils.Max(lst).ToString();

            iWrite.WriteLine();
            iWrite.Write(OutputUtils.FormatField("Mean", 10));
            iWrite.Write(OutputUtils.FormatField("Std.Dev.", 10));
            iWrite.Write(OutputUtils.FormatField("Min", 10));
            iWrite.Write(OutputUtils.FormatField("Max", 10));
            iWrite.Write(Environment.NewLine);
            iWrite.Write("----------------------------------------");
            iWrite.Write(Environment.NewLine);
            iWrite.Write(OutputUtils.FormatField(M, 10));
            iWrite.Write(OutputUtils.FormatField(SD, 10));
            iWrite.Write(OutputUtils.FormatField(Min, 10));
            iWrite.Write(OutputUtils.FormatField(Max, 10));
            iWrite.WriteLine();
        }

        protected void debugLoggerTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            OpenStreamAndLog(iWrite, true, FMRShell.DataType.Maof, "simLog.txt", "simLogger");
        }

        protected void debugOperationsLogMaofCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            // generate filename
            string filename = "maofLog." + DateTime.Now.ToString() + ".txt";
            filename = filename.Replace('/', ' ');
            filename = filename.Replace(' ', '_');
            iWrite.WriteLine("Log file " + filename);

            OpenStreamAndLog(iWrite, false, FMRShell.DataType.Maof, filename, "MaofLogger");
        }

        protected FMRShell.Collector tradingDataCollector;
        protected TradingDataLogger DataLogger;

        protected void debugOperatonsStopLogCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            CloseDataStreamAndLog(iWrite, (FMRShell.DataType)cmdArguments[0]);
        }

        protected void CloseDataStreamAndLog(IWrite iWrite, FMRShell.DataType dt)
        {
            DataLogger.Stop();
            DataLogger.Dispose();
            DataLogger = null;
            tradingDataCollector.Stop(dt);
        }

        protected void OpenStreamAndLog(IWrite iWrite, bool test, FMRShell.DataType TrDataType, string filename, string loggerName)
        {
#if USEFMRSIM
            if (TrDataType == FMRShell.DataType.Maof)
            {
                // create Maof data generator
                TaskBarLibSim.MaofDataGeneratorRandom dataMaofGenerator = new TaskBarLibSim.MaofDataGeneratorRandom();
                // setup the data generator(s) in the K300Class
                TaskBarLibSim.K300Class.InitStreamSimulation(dataMaofGenerator);
            }
            else if (TrDataType == FMRShell.DataType.Rezef)
            {
                //create Rezef data generator
                TaskBarLibSim.RezefDataGeneratorRandom dataRzfGenerator = new TaskBarLibSim.RezefDataGeneratorRandom();
                TaskBarLibSim.K300Class.InitStreamSimulation(dataRzfGenerator);
            }
            else
            {
                iWrite.WriteLine("Unknown data type: " + TrDataType.ToString());
            }
#endif

            // create Collector (producer) - will do it only once
            tradingDataCollector = new FMRShell.Collector();

            // create logger which will register itself (AddSink) in the collector
            DataLogger = new TradingDataLogger(loggerName, filename, false, tradingDataCollector, TrDataType);


            // start logger
            DataLogger.Start();

            // start collector, which will start the stream in K300Class, whch will start
            // data generator
            tradingDataCollector.Start(TrDataType);

            Thread.Sleep(100);
            debugLoggerShowCallback(iWrite, "", null);

            if (test)
            {
                Thread.Sleep(1000);

                CloseDataStreamAndLog(iWrite, TrDataType);
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

        protected void Timer5sHandler(ITimer timer)
        {
             Console.WriteLine("5s timer expired "+ DateTime.Now);
        }
        
        protected void Timer30sHandler(ITimer timer)
        {
             Console.WriteLine("30s timer expired "+ DateTime.Now);
        }
        
        protected void debugTimerTestThread()
        {
            // create set (timer task). initially empty
            TimerTask timerTask = new TimerTask("ShortTimers");

            Console.WriteLine("Start timers "+ DateTime.Now);
            
            // create two types of timers
            TimerList timers_5sec = new TimerList("5sec", 5*1000, 100, this.Timer5sHandler, timerTask);
            TimerList timers_30sec = new TimerList("30sec", 30*1000, 100, this.Timer30sHandler, timerTask);
            
            timerTask.Start();

            // start some timers
            timers_5sec.Start();
            timers_5sec.Start();
            timers_5sec.Start();
            Thread.Sleep(1*1000);
            timers_5sec.Start();

            ITimer timer;
            long timerId;
            timers_30sec.Start(out timer, out timerId, null, false);            
            timers_5sec.Start();

            debugTimerShowCallback(null, null, null);

            // wait for the first timer to expire
            Thread.Sleep(10*1000);
            timers_30sec.Stop(timer, timerId);
            
            Thread.Sleep(30*1000);
            debugTimerShowCallback(null, null, null);
            
            // clean up
            timers_5sec.Dispose();
            timers_30sec.Dispose();
            timerTask.Dispose();            
        }
        
        protected void debugTimerTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            // call once - init timers subsystem
            Timers.Init();

            // timer test contains delauys. run the test from a separate thread and release
            // user input context
            System.Threading.Thread thread = new System.Threading.Thread(debugTimerTestThread);
            thread.Start();
        }

        protected void debugTimerShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            System.Collections.ArrayList names;
            System.Collections.ArrayList values;
            int entry = 0;
            int columnSize = 12;
            
            bool isEmpty = true;
            
            ((JQuant.IWrite)this).WriteLine();
            
            foreach (IResourceTimerList timerList in Resources.TimerLists)
            {
                timerList.GetEventCounters(out names, out values);
                isEmpty = false;

                if (entry == 0)
                {
                    names.Insert(0, "TimerListName");
                    names.Insert(0, "TimerTaskName");
                    CommandLineInterface.printTableHeader((JQuant.IWrite)this, names, columnSize);
                }
                values.Insert(0, OutputUtils.FormatField(timerList.Name, columnSize));
                values.Insert(0, OutputUtils.FormatField(timerList.GetTaskName(), columnSize));
                CommandLineInterface.printValues((JQuant.IWrite)this, values, columnSize);
                
                entry++;

            }
            if (isEmpty)
            {
                System.Console.WriteLine("No timers");
            }
        }

        protected long[] threadpoolTestTicks;
        
        protected void ThreadPoolJobEnter(object argument)
        {
        }

        protected void ThreadPoolJobDone(object argument)
        {
            int c = (int)argument;
            long tick = DateTime.Now.Ticks;
            threadpoolTestTicks[c] = (tick - threadpoolTestTicks[c]);
        }

        protected void debugThreadPoolTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            int maxJobs = 5;
            JQuant.ThreadPool threadPool = new JQuant.ThreadPool("test", 1, maxJobs, ThreadPriority.Lowest);

            threadpoolTestTicks = new long[maxJobs];
            long tick = DateTime.Now.Ticks;
            for (int i = 0;i < maxJobs;i++)
            {
                threadpoolTestTicks[i] = tick;
            }
            
            for (int i = 0;i < maxJobs;i++)
            {
                threadPool.PlaceJob(ThreadPoolJobEnter, ThreadPoolJobDone, i);
            }
            Thread.Sleep(500);
            
            debugThreadPoolShowCallback(iWrite, cmdName, cmdArguments);
            threadPool.Dispose();

            for (int i = 0;i < threadpoolTestTicks.Length;i++)
            {
                iWrite.WriteLine("ThreadPoolJob done  idx =" + i + ", time = " + (double)threadpoolTestTicks[i]/(double)(10*1) + " micros");
            }
            
        }

        protected void feedGetSeriesCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
        }
        
        protected void debugThreadPoolShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            debugPrintResourcesNameAndStats(iWrite, Resources.ThreadPools);
        }


        protected void debugOrderShowCallback()
        {
            OrderType orderType = (OrderType.LMT | OrderType.FOK | OrderType.IOC);
        }
        
        protected void LoadCommandLineInterface()
        {

            cli.SystemMenu.AddCommand("exit", "Exit from the program", "Cleanup and exit", this.CleanupAndExit);

            Menu menuOperations = cli.RootMenu.AddMenu("Oper", "Operations",
                                   " Login, start stream&log");
            menuOperations.AddCommand("Login", "Login to the remote server",
                                  " The call will block until login succeeds", debugLoginCallback);
            menuOperations.AddCommand("StartLog", "Log Maof stream",
                                  " Start stream and run logger", debugOperationsLogMaofCallback);
            menuOperations.AddCommand("StopLog", "Stop previosly started Log",
                                  " Stop stream andlogger", debugOperatonsStopLogCallback);
            menuOperations.AddCommand("ShowLog", "Show existing loggers",
                                  " List of created loggers with the statistics", debugLoggerShowCallback);

            // Menu menuFMRLib = 
            cli.RootMenu.AddMenu("FMRLib", "Access to  FMRLib API",
                          " Allows to access the FMRLib API directly");
            // Menu menuFMRLibSim = 
            cli.RootMenu.AddMenu("FMRLibSim", "Configure FMR simulation",
                           " Condiguration and debug of the FMR simulatoion");


            Menu menuFeed = cli.RootMenu.AddMenu("Feed", "Trading data feeds",
                                   " Get data from the data feeds, TA screens");
            menuFeed.AddCommand("getseries", "Get price/volume series",
                                  " Get price/volume series for the specified stock symbol", feedGetSeriesCallback);
            
            Menu menuDebug = cli.RootMenu.AddMenu("Dbg", "System debug info",
                                   " Created objetcs, access to the system statistics");
            menuDebug.AddCommand("GC", "Run garbage collector",
                                  " Forces garnage collection", debugGcCallback);
            menuDebug.AddCommand("mbxTest", "Run simple mailbox tests",
                                  " Create a mailbox, send a message, receive a message, print debug info", debugMbxTestCallback);
            menuDebug.AddCommand("mbxShow", "Show mailboxes",
                                  " List of created mailboxes with the current status and statistics", debugMbxShowCallback);
            menuDebug.AddCommand("threadTest", "Run simple thread",
                                  " Create a mailbox thread, send a message, print debug info", debugThreadTestCallback);
            menuDebug.AddCommand("threadShow", "Show threads",
                                  " List of created threads and thread states", debugThreadShowCallback);
            menuDebug.AddCommand("poolTest", "Run simple pool tests",
                                  " Create a pool, add object, allocate object, free object", debugPoolTestCallback);
            menuDebug.AddCommand("poolShow", "Show pools",
                                  " List of created pools with the current status and statistics", debugPoolShowCallback);
            menuDebug.AddCommand("loginTest", "Run simple test of the login",
                                  " Create a FMRShell.Connection(xmlfile) and call Open()", debugLoginCallback);
            menuDebug.AddCommand("AS400TimeTest", "ping the server",
                                  "ping AS400 server in order to get latency and synchronize local amachine time with server's",
                                  debugGetAS400DTCallback);


            menuDebug.AddCommand("timerTest", "Run simple timer tests",
                                  " Create a timer task, two timer lists, start two timers, clean up", debugTimerTestCallback);
            menuDebug.AddCommand("timerShow", "Show timers",
                                  " List of created timers and timer tasks", debugTimerShowCallback);

            menuDebug.AddCommand("threadPoolTest", "Run simple thread pool tests",
                                  " Create a thread pool, start a couple of jobs, destroy the pool", debugThreadPoolTestCallback);
            menuDebug.AddCommand("threadPoolShow", "Show thread pools",
                                  " List of created thread pools", debugThreadPoolShowCallback);

#if USEFMRSIM
            menuDebug.AddCommand("loggerTest", "Run simple test of the logger",
                                  " Create a Collector and start a random data simulator", debugLoggerTestCallback);
#endif
            menuDebug.AddCommand("loggerShow", "Show existing loggers",
                                  " List of created loggers with the statistics", debugLoggerShowCallback);
        }

    }
}
