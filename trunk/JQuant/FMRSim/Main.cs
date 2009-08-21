//
//
// This is FMR Simulation project
// Partial implementation of FMRLab API as in the defined in the fmrlib.idl
	
	
	
using System;


namespace JQuant {
	
  class Program :JQuant.IWrite {   
		
	 Program() {
		cli = new CommandLineInterface("Jerusalem Quant");
		LoadCommandLineInterface();
	}
		
	private CommandLineInterface cli;
	private bool ExitFlag;
	

	private void CleanupAndExit(IWrite iWrite, string cmdName, object[] cmdArguments) {
		// chance to clean some things before exiting
			
		// flag to break out of the loop forever
		ExitFlag = true;
	}
		
	protected void debugMbxShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments) {
        iWrite.WriteLine(
            OutputUtils.FormatField("Name", 10)+
			OutputUtils.FormatField("Count", 10)+
			OutputUtils.FormatField("Capacity", 10)+
			OutputUtils.FormatField("MaxCount", 10)+
			OutputUtils.FormatField("Dropped", 10)+
			OutputUtils.FormatField("Sent", 10)+
			OutputUtils.FormatField("Received", 10)+
			OutputUtils.FormatField("Timeouts", 10)
		);
        iWrite.WriteLine("---------------------------------------------------------------------------------");
		bool isEmpty = true;
		foreach (IMailbox iMbx in Resources.Mailboxes) 
		{
				isEmpty = false;
				iWrite.WriteLine(
					OutputUtils.FormatField(iMbx.GetName(), 10)+
					OutputUtils.FormatField(iMbx.GetCount(), 10)+
					OutputUtils.FormatField(iMbx.GetCapacity(), 10)+
					OutputUtils.FormatField(iMbx.GetMaxCount(), 10)+
					OutputUtils.FormatField(iMbx.GetDropped(), 10)+
					OutputUtils.FormatField(iMbx.GetSent(), 10)+
					OutputUtils.FormatField(iMbx.GetReceived(), 10)+
					OutputUtils.FormatField(iMbx.GetTimeouts(), 10)
				);
				                 
		}		
		if (isEmpty) 
		{
			iWrite.WriteLine("No mailboxes");
		}
	}

	protected void debugMbxTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments) 
	{
		Mailbox<bool> mbx = new Mailbox<bool>("TestMbx", 2);

		iWrite.WriteLine("TestMbx created");
		bool message = true;
		bool result = mbx.Send(message);
		if (!result) {
			iWrite.WriteLine("Mailbox.Send returned false");
		}
		result = mbx.Receive(message);
		if (!result) {
			iWrite.WriteLine("Mailbox.Receive returned false");
		}
		if (!message) {
			iWrite.WriteLine("I did not get what i sent");
		}
		debugMbxShowCallback(iWrite, cmdName, cmdArguments);
	}

		
	protected void debugThreadTestCallback(IWrite iWrite, string cmdName, object[] cmdArguments) 
	{
		MailboxThread<bool> thr = new MailboxThread<bool>("TestMbx", 2);
		debugThreadShowCallback(iWrite, cmdName, cmdArguments);
		thr.Start();
		debugThreadShowCallback(iWrite, cmdName, cmdArguments);
		bool message = true;
		bool result = thr.Send(message);
		if (!result) {
			iWrite.WriteLine("Thread.Send returned false");
		}
		thr.Stop();
		debugThreadShowCallback(iWrite, cmdName, cmdArguments);
		thr = null;
	}
		
		
	protected void debugThreadShowCallback(IWrite iWrite, string cmdName, object[] cmdArguments) {
        iWrite.WriteLine(
            OutputUtils.FormatField("Name", 10)+
			OutputUtils.FormatField("State", 14)
		);
        iWrite.WriteLine("---------------------------------");
		bool isEmpty = true;
		foreach (IThread iThread in Resources.Threads) 
		{
				isEmpty = false;
				iWrite.WriteLine(
					OutputUtils.FormatField(iThread.GetName(), 10)+
					OutputUtils.FormatField(EnumUtils.GetDescription(iThread.GetState()), 14)
				);
				                 
		}		
		if (isEmpty) 
		{
			iWrite.WriteLine("No mailboxes");
		}
	}
		
		
	protected void LoadCommandLineInterface() {  
		cli.SystemMenu.AddCommand("exit", "Exit from the program", "Cleanup and exit", this.CleanupAndExit);
		Menu menuFMRLib = cli.RootMenu.AddMenu("FMRLib", "Access to  FMRLib API", 
			                  " Allows to access the FMRLib API directly");
		Menu menuFMRLibSim = cli.RootMenu.AddMenu("FMRLibSim", "Configure FMR simulation", 
			                   " Condiguration and debug of the FMR simulatoion");
		Menu menuDebug = cli.RootMenu.AddMenu("Debug", "System debug info", 
			                   " Created objetcs, access to the system statistics");
		menuDebug.AddCommand("mbxTest", "Run simple mailbox tests", 
                              " Create a mailbox, send a message, receive a message, print debug info", debugMbxTestCallback);
		menuDebug.AddCommand("mbxShow", "Show mailboxes", 
                              " List of created mailboxes with the current status and statistics", debugMbxShowCallback);
		menuDebug.AddCommand("threadTest", "Run simple thread", 
                              " Create a mailbox thread, send a message, print debug info", debugThreadTestCallback);
		menuDebug.AddCommand("threadShow", "Show threads", 
                              " List of created threads and thread states", debugThreadShowCallback);
	}  

	public void WriteLine(string s) {
		Console.WriteLine(s);
	}
		
	public void Write(string s) {
		Console.Write(s);
	}
		
	public void Run() {

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
		
	static void Main(string[] args) {  
		Resources.Init();
		new Program().Run();  
		
		// very last chance for the cleanup - close streams and so on
		// before i return control to the OS	
	}

		
  }
	
}