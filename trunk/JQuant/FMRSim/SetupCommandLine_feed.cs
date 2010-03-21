
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
        protected void LoadCommandLineInterface_feed()
        {
            Menu menuFeed = cli.RootMenu.AddMenu("Feed", "Trading data feeds",
                                   " Get data from the data feeds, TA screens");
            menuFeed.AddCommand("get", "Get price/volume series",
                                  " Get price/volume daily series for the specified stock symbol. Args: symbol [fromDate[toDate]]", feedGetSeriesCallback);
            menuFeed.AddCommand("gf", "Write price/volume series to file",
                                  " Get price/volume daily series for the specified stock symbol and write to file. Args: symbol filename [fromDate[toDate]]", feedGetToFileCallback);
            menuFeed.AddCommand("rf", "Get price/volume series from file",
                                  " Load price/volume daily series for the specified file. Args: filename", feedGetSeriesFromFileCallback);
            menuFeed.AddCommand("fisher", "Fisher transform",
                                  "Apply Fisher transform to the latest loaded series", feedFisherTransformCallback);
        }
        
    }
}
