
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


        protected void LoadCommandLineInterface_test()
        {
            Menu menuTests = cli.RootMenu.AddMenu("tst", "Short tests",
                                   " Infrastructure/API tests");

            menuTests.AddCommand("GC", "Run garbage collector",
                                  " Forces garnage collection", debugGcCallback);
            menuTests.AddCommand("mbxTest", "Run simple mailbox tests",
                                  " Create a mailbox, send a message, receive a message, print debug info", debugMbxTestCallback);
            menuTests.AddCommand("mbxShow", "Show mailboxes",
                                  " List of created mailboxes with the current status and statistics", debugMbxShowCallback);
            menuTests.AddCommand("threadTest", "Run simple thread",
                                  " Create a mailbox thread, send a message, print debug info", debugThreadTestCallback);
            menuTests.AddCommand("threadShow", "Show threads",
                                  " List of created threads and thread states", debugThreadShowCallback);
            menuTests.AddCommand("poolTest", "Run simple pool tests",
                                  " Create a pool, add object, allocate object, free object", debugPoolTestCallback);
            menuTests.AddCommand("poolShow", "Show pools",
                                  " List of created pools with the current status and statistics", debugPoolShowCallback);

            menuTests.AddCommand("timerTest", "Run simple timer tests",
                                  " Create a timer task, two timer lists, start two timers, clean up", debugTimerTestCallback);
            menuTests.AddCommand("timerShow", "Show timers",
                                  " List of created timers and timer tasks", debugTimerShowCallback);

            menuTests.AddCommand("threadPoolTest", "Run simple thread pool tests",
                                  " Create a thread pool, start a couple of jobs, destroy the pool", debugThreadPoolTestCallback);
            menuTests.AddCommand("threadPoolShow", "Show thread pools",
                                  " List of created thread pools", debugThreadPoolShowCallback);

            menuTests.AddCommand("cbtest", "Cyclic buffer class test",
                                  " Create a cyclic buffer, check functionality", debugCyclicBufferTestCallback);

            menuTests.AddCommand("rtclock", "RT clock test",
                                  " Calls PreciseTime periodically and checks that the returned time is reasonable", debugRTClockCallback);
            menuTests.AddCommand("rtclock_1", "RT clock test",
                                  " Calls PreciseTime in tight loop and checks that the returned time is reasonable", debugRTClock1Callback);
            menuTests.AddCommand("rtclock_2", "RT clock test",
                                  " Calls PreciseTime and print out ticks", debugRTClock2Callback);
        }
        
    }
}
