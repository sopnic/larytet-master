
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


        protected void LoadCommandLineInterface_ms()
        {
            Menu menuMarketSim = cli.RootMenu.AddMenu("ms", "Market simulation",
                                   " Run market simulation");

            menuMarketSim.AddCommand("maof",
                                    "Run MarketSimulationMaof. Usage: maof create <backlogfile> [speedup] | start | stop",
                                    "Create Maof Event Generator, connect to the Maof Market simulation",
                                    debugMarketSimulationMaofCallback
                                    );

            menuMarketSim.AddCommand("stat",
                                    "Show statistics for the running market simulation. Usage: stat core|book|queue",
                                    "Display number of events, number of placed orders at different layers of the market simulaiton",
                                    debugMarketSimulationMaofStatCallback
                                    );

            menuMarketSim.AddCommand("secs",
                                    "Show list of securities. Usage secs maof|core|book|queue",
                                    "Display list of securities including number of orders",
                                    debugMarketSimulationMaofSecsCallback
                                    );

            
            menuMarketSim.AddCommand("trace",
                                    "Enable trace for specific security. Usage: trace <securityId> <enable|disable>",
                                    "Enable/disable debug trace for specific security. "+"Security identifier can be a unique number, \n"+
                                     "or things like 'C1800Oct', 'call 1800 Oct', 'call1800Oct', etc.  ",
                                    debugMarketSimulationMaofTraceCallback
                                    );

            
            menuMarketSim.AddCommand("w",
                                    "Add security to the watch list. Usage: w [add|rmv|legend|list] <securityId> ",
                                    "Add (remove) specific security to (from) watch list, pint list of watched securities.\n"
                                     +"Security identifier can be a unique number, \n"+
                                     "or things like 'C1800Oct', 'call 1800 Oct', 'call1800Oct', etc.  ",
                                    debugMarketSimulationMaofWatchCallback
                                    );
            
            menuMarketSim.AddCommand("p",
                                    "Place order. Usage: p [buy|sell] <securityId>  [limit] [quantity]",
                                    "Place order for specific security. "+"Security identifier can be a unique number, \n"+
                                     "or things like 'C1800Oct', 'call 1800 Oct', 'call1800Oct', etc.  ",
                                    debugMarketSimulationMaofPlaceOrderCallback
                                    );
            
            menuMarketSim.AddCommand("c",
                                    "Cancel order. Usage: c <securityId>",
                                    "Cancels previously placed order. "+"Security identifier can be a unique number, \n"+
                                     "or things like 'C1800Oct', 'call 1800 Oct', 'call1800Oct', etc.  ",
                                    debugMarketSimulationMaofCancelOrderCallback
                                    );
        }
        
    }
}
