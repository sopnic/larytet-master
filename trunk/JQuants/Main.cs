//
// To compile the example use line under Mono 2.4
// /usr/bin/gmcs2 BaseConsole.cs Main.cs 
//
// This is a sample CLI application 
	
	
	
using System;

namespace JQuants {
	
  class Program :JQuants.IWrite {   
		
	 Program() {
		cli = new CommandLineInterface("JQuants", this);
		LoadCommandLineInterface();
	}
		
	private CommandLineInterface cli;
	private bool ExitFlag;
	

	private void SetExitFlag() {
		ExitFlag = true;
	}
		
	protected void menu1cmd1Callback() {
		WriteLine("Menu 1 Command 1 called");
	}

	protected void menu1cmd2Callback() {
		WriteLine("Menu 1 Command 2 called");
	}

	protected void menu2cmd1Callback() {
		WriteLine("Menu 2 Command 1 called");
	}

	protected void menu2cmd2Callback() {
		WriteLine("Menu 2 Command 2 called");
	}

	protected void LoadCommandLineInterface() {  
		cli.SystemMenu.AddCommand("exit", "Exit from the program", "Clean and exit", this.SetExitFlag);
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

		cli.PrintTitle();

		// While not exit command entered, process each command
		while (true) {

			cli.PrintPrompt();
			string input = Console.ReadLine();

			if (input != "")
			try {
				// process command
				cli.ProcessCommand(input);
				if (ExitFlag) break;

			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());

			} finally {
				Console.WriteLine();
			}
		}

	}
		
	static void Main(string[] args) {  
		new Program().Run();  
	}

		
  }
	
}
