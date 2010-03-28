
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;


namespace JQuant
{
    partial class Program
    {

        protected void LoadCommandLineInterface_sa()
        {
            Menu menuDebug = cli.RootMenu.AddMenu("sa", "Semiautomatics trader",
                                   " Semiautomatic traidng routines");

        }
        
    }
}
