//
// To compile the example use line under Mono 2.4
// /usr/bin/gmcs2 BaseConsole.cs Main.cs 
//
// This is a sample CLI application 
	
	
	
using System;

namespace JQuants {
	
  class Program :JQuants.CommandLineInterface {   
		
	Program() {
		LoadCommandLineInterface();
	}
	
	public override string Name {  
		get { return "JQuants"; }  
	}  

	private void SetExitFlag() {
		ExitFlag = true;
	}

	protected void LoadCommandLineInterface() {  
		SystemMenu.AddCommand("exit", "Exit from the program", "Clean and exit", this.SetExitFlag);
	}  
   
		
	public void Run() {

		PrintTitle();

		// While not exit command entered, process each command
		while (true) {

			PrintPrompt();
			string input = Console.ReadLine();

			if (input != "")
			try {
				// process command
				ProcessCommand(input);
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
