
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace JQuant
{

    partial class Program
    {


        protected void LoadCommandLineInterface()
        {
            cli.SystemMenu.AddCommand("exit", "Exit from the program",
                "Cleanup and exit", this.CleanupAndExit);
            // Menu menuFMRLib = 
            cli.RootMenu.AddMenu("FMRLib", "Access to  FMRLib API",
                          " Allows to access the FMRLib API directly");
            // Menu menuFMRLibSim = 
            cli.RootMenu.AddMenu("FMRLibSim", "Configure FMR simulation",
                           " Condiguration and debug of the FMR simulatoion");

            LoadCommandLineInterface_oper();
            LoadCommandLineInterface_dbg();
            LoadCommandLineInterface_test();
            LoadCommandLineInterface_ms();
            LoadCommandLineInterface_feed();
        }



    }//partial class Program
}//namespace JQuant
