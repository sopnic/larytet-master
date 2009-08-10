//
// To compile the example use line under Mono 2.4
// /usr/bin/gmcs2 BaseConsole.cs Main.cs 
//
// This is a sample CLI application 
	
	
	
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
		
	protected void menu1cmd1Callback(IWrite iWrite, string cmdName, object[] cmdArguments) {
		iWrite.WriteLine("Menu 1 Command 1 called");
	}

	protected void menu1cmd2Callback(IWrite iWrite, string cmdName, object[] cmdArguments) {
		iWrite.WriteLine("Menu 1 Command 2 called");
	}

	protected void menu2cmd1Callback(IWrite iWrite, string cmdName, object[] cmdArguments) {
		iWrite.WriteLine("Menu 2 Command 1 called");
	}

	protected void menu2cmd2Callback(IWrite iWrite, string cmdName, object[] cmdArguments) {
		iWrite.WriteLine("Menu 2 Command 2 called");
	}

	protected void LoadCommandLineInterface() {  
		cli.SystemMenu.AddCommand("exit", "Exit from the program", "Cleanup and exit", this.CleanupAndExit);
		Menu menu1 = cli.RootMenu.AddMenu("menu1", "Menu 1", "This is menu 1");
		Menu menu2 = cli.RootMenu.AddMenu("menu2", "Menu 2", "This is menu 2");
		menu1.AddCommand("cmd1", "Command 1", "This is command 1", menu1cmd1Callback);
		menu1.AddCommand("cmd2", "Command 2", "This is command 2", menu1cmd2Callback);
		menu2.AddCommand("cmd1", "Command 1", "This is command 1", menu2cmd1Callback);
		menu2.AddCommand("cmd2", "Command 2", "This is command 2", menu2cmd2Callback);
	}  

	public void WriteLine(string s) {
		Console.WriteLine(s);
	}
		
	public void Write(string s) {
		Console.Write(s);
	}
		
	public void Run() {

		cli.PrintTitle(this);

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
		new Program().Run();  
		
		// very last chance for the cleanup - close streams and so on
		// before i return control to the OS	
	}

		
  }
	
}
