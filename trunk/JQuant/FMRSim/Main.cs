// This is FMR Simulation project
// Partial implementation of FMRLab API as in the defined in the fmrlib.idl

using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;


namespace JQuant
{

    partial class Program : JQuant.IWrite
    {
        /// <summary>
        /// application uses something like Program.instance.WriteLine()
        /// </summary>
        /// <param name="s">
        /// A <see cref="System.String"/>
        /// string to print
        /// </param>
        public void WriteLine(string s)
        {
            // stdio
            Console.WriteLine(s);

            // and GUI 
            // output is asynchronous - send mail to the thread
#if WITHGUI
            consoleOut.Write(s + Environment.NewLine);
#endif
        }


        public void WriteLine()
        {
            // stdio
            Console.WriteLine();

            // and GUI 
            // output is asynchronous - send mail to the thread
#if WITHGUI
            consoleOut.Write(Environment.NewLine);
#endif
        }


        public void Write(string s)
        {
            Console.Write(s);

            // and GUI 
            // output is asynchronous - send mail to the thread
#if WITHGUI
            consoleOut.Write(s);
#endif
        }

        /// <summary>
        /// this guy is called by Main() and will never be called by anybody else
        /// Use static property instance to access methods of the class
        /// </summary>
        protected Program()
        {
            cli = new CommandLineInterface("Jerusalem Quant");
            LoadCommandLineInterface();

#if WITHGUI
            // tricky part - i need output console before main form is initialized        
            consoleOut = new JQuantForms.ConsoleOutDummy();

            // now the rest of the GUI controls
            Thread guiThread = new Thread(this.InitGUI);
            guiThread.Priority = ThreadPriority.Lowest;
            guiThread.Start();
#endif
        }

        private CommandLineInterface cli;
        private bool ExitFlag;


        private void CleanupAndExit(IWrite iWrite, string cmdName, object[] cmdArguments)
        {
            // chance to clean some things before exiting

            // flag to break out of the loop forever
            ExitFlag = true;
        }


        protected void Run()
        {

            //cli.PrintTitle(this);
            cli.PrintCommands(this);

            // While exit command not entered, process each command
            while (true)
            {

                cli.PrintPrompt(this);
                string input = Console.ReadLine();

                if (input != "")
                    try
                    {
                        // process command - "this" is IWrite interface
                        cli.ProcessCommand(this, input);
                        if (ExitFlag)
                        {
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        WriteLine(ex.ToString());

                    }
                    finally
                    {
                        WriteLine("");
                    }
            }


            // last chance for the cleanup - close streams and so on
#if WITHGUI
            CloseGUI();
#endif
        }

        protected delegate void ApplicationExitDelegate();
        protected void CloseGUI()
        {
            ApplicationExitDelegate applicationExit = new ApplicationExitDelegate(Application.Exit);
            if (mainForm.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                mainForm.Invoke(applicationExit);
            }
            else
            {
                // It's on the same thread, no need for Invoke
                Application.Exit();
            }
        }

        protected void ConsoleInCommandHalder(string s)
        {
            cli.ProcessCommand(this, s);
        }

        /// <summary>
        /// executed in a separate thread - uses spare CPU cycles
        /// </summary>
        protected void InitGUI()
        {
            // Control.CheckForIllegalCrossThreadCalls = false;            

            // create consoles for output/input
            // output console is one of the first things to create
            JQuantForms.ConsoleOut consoleOut = new JQuantForms.ConsoleOut();
            consoleOut.Dock = DockStyle.Fill;
            this.consoleOut = consoleOut;

            consoleIn = new JQuantForms.ConsoleIn(new JQuantForms.ConsoleIn.ProcessCommandDelegate(this.ConsoleInCommandHalder));
            consoleIn.Dock = DockStyle.Fill;

            // Create layout
            tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Fill;
            tlp.ColumnCount = 1;
            tlp.RowCount = 2;

            tlp.RowStyles.Add(new System.Windows.Forms.RowStyle
                              (System.Windows.Forms.SizeType.Percent, 80F));
            tlp.RowStyles.Add(new System.Windows.Forms.RowStyle
                              (System.Windows.Forms.SizeType.Absolute, 28F));

            // add consoles
            tlp.Controls.Add(consoleOut, 0, 0);
            tlp.Controls.Add(consoleIn, 0, 1);

            // i have no idea what this thing does
            // tlp.ResumeLayout(false);
            // tlp.PerformLayout();

            // create main form
            mainForm = new Form();
            mainForm.Size = new System.Drawing.Size(600, 400);

            mainForm.SuspendLayout();

            // add layout to the main form
            mainForm.Controls.Add(tlp);

            mainForm.ResumeLayout(false);
            mainForm.PerformLayout();

            mainForm.Show();

            Application.Run(mainForm);
        }

        static void Main(string[] args)
        {
            //check whether JQUANT_ROOT environment variable is set on the system
            if (!Resources.RootDirectoryDefined()) 
                Console.WriteLine(Environment.NewLine 
                    + "Warning! Environment variable JQUANT_ROOT is not set" + Environment.NewLine);

            // init precision clock 
            PreciseTime.Init();

            // lists of critical system objects
            Resources.Init();

            // timers subsystem
            Timers.Init();

            instance = new Program();

            // run console
            instance.Run();
        }

        protected Form mainForm;

        protected JQuantForms.IConsoleOut consoleOut;
        protected JQuantForms.ConsoleIn consoleIn;
        protected TableLayoutPanel tlp;

        protected FMRShell.Connection fmrConection;

        public static Program instance
        {
            get;
            protected set;
        }

    }
}
