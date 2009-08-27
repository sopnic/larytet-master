//
//
// This is FMR Simulation project
// Partial implementation of FMRLab API as in the defined in the fmrlib.idl
	
using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;


namespace JQuant 
{
	
	partial class Program :JQuant.IWrite
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
			outputThread.Send(s+"\n");
		}
			
		public void Write(string s) 
		{
			Console.Write(s);

			// and GUI 
			// output is asynchronous - send mail to the thread
			outputThread.Send(s);
		}
		
		protected class OutputThread: MailboxThread<string>
		{
			public OutputThread() : base("ConsoleOut", 100)
			{
			}
			
	        protected override void HandleMessage(string s)
	        {
#if WITHGUI
				consoleOut.Write(s);
#endif                
			}
			
			public JQuantForms.ConsoleOut consoleOut
			{
				get;
				set;
			}
		}

		/// <summary>
		/// this guy is called by Main() and will never be called by anybody else
		/// Use static property instance to access methods of the class
		/// </summary>
		protected Program() 
		{
			outputThread = new OutputThread();

			cli = new CommandLineInterface("Jerusalem Quant");
			LoadCommandLineInterface();
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
		
			cli.PrintTitle(this);
			cli.PrintCommands(this);
		
			// While exit command not entered, process each command
			while (true) {
		
				cli.PrintPrompt(this);
				string input = Console.ReadLine();
		
				if (input != "")
				try {
					// process command - "this" is IWrite interface
					cli.ProcessCommand(this, input);
					if (ExitFlag) break;
		
				} catch (Exception ex) {
					WriteLine(ex.ToString());
		
				} finally {
					WriteLine("");
				}
			}
				
				
			// last chance for the cleanup - close streams and so on
		
		}
			
		protected void CloseGUI()
		{
			Console.Write("Stop output...");
			outputThread.Stop();
			Console.Write("Clear output...");
			consoleOut.Clear();
			Console.Write("Clear table panel...");
			tlp.Controls.Remove(consoleOut);
			tlp.Controls.Remove(consoleIn);
			Console.Write("Clear main form...");
			mainForm.Controls.Remove(tlp);				
		}
		
		/// <summary>
		/// executed in a separate thread - uses spare CPU cycles
		/// </summary>
		protected void InitGUI()
		{
			// create consoles for output/input
			consoleOut = new JQuantForms.ConsoleOut();
			consoleOut.Dock = DockStyle.Fill;

			consoleIn = new JQuantForms.ConsoleIn();
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
            
			mainForm.Activate();
			
			outputThread.consoleOut = consoleOut;
			outputThread.Start();
            
			// spawn a thread to handle the mainForm
			Application.Run(mainForm);
		}
		
		static void Main(string[] args) 
		{
            
			Resources.Init();
				
				
			instance = new Program();			

#if WITHGUI
			// bring up GUI	(spawns separate thread)
			new Thread(instance.InitGUI).Start();
#endif            
		
			// run console (blocking call)
			instance.Run();  
			
			
			
			// very last chance for the cleanup - close streams and so on
			// before i return control to the OS	
			
#if WITHGUI
            Console.Write("Exiting...close GUI...");
            instance.CloseGUI();
			Application.Exit();
            Console.WriteLine("done");
#else            
#endif
		}
		
		protected Form mainForm;
		// tricky part - i need output console before main form is initialized
		protected JQuantForms.ConsoleOut consoleOut;
		protected JQuantForms.ConsoleIn consoleIn;
		protected OutputThread outputThread;
		protected TableLayoutPanel tlp;
		
		public static Program instance
		{
			get;
			protected set;
		}
		
	}		
}