
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
        protected void LoadCommandLineInterface_oper()
        {
            Menu menuOperations = cli.RootMenu.AddMenu("Oper", "Operations",
                                   " Login, start stream&log");

            menuOperations.AddCommand("Login",
                                        "Login to the remote server",
                                        " The call will block until login succeeds",
                                        operLoginCallback
                                        );
            menuOperations.AddCommand("Logout",
                                        "Perform the logout process",
                                        " The call will block until logout succeeds",
                                        operLogoutCallback
                                        );
            menuOperations.AddCommand("StartLog",
                                        "Log data stream - choose MF|RZ|MDD.",
                                        " Start trading data stream and run logger. In simulation mode playback file can be specified",
                                        operLogCallback
                                        );
            menuOperations.AddCommand("StopLog",
                                        "Stop previosly started Maof Log - MF | MDD | RZ, to stop stream type Y",
                                        " Stop logger - Maof(MF) | Madad (MDD) | Rezef (RZ) and stream (Y/N). ",
                                        operStopLogCallback);
            menuOperations.AddCommand("StopStream",
                                        "Stop previosly started data stream - MF | MDD | RZ",
                                        " Stop data stream - Maof(MF) | Madad (MDD) | Rezef (RZ) ",
                                        StopStreamCallBack
                                        );
            menuOperations.AddCommand("ShowLog",
                                        "Show existing loggers",
                                        " List of created loggers with the statistics",
                                        debugLoggerShowCallback
                                        );
            menuOperations.AddCommand("AS400TimeTest",
                                        "ping the server",
                                        "ping AS400 server in order to get latency and synchronize local amachine time with server's",
                                        debugGetAS400DTCallback);
        }
        
    }
}
