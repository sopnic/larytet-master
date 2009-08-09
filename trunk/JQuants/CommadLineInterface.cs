using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;


namespace JQuants {
	
	public class Command {
		
		public Command(string name, string shortDesctiption, string description, Action callback) {
			Name = name;
			ShortDescription = shortDesctiption;
			Description = description;
			Callback = callback;
		}

		public Command(string name, string shortDesctiption, string description) {
			Name = name;
			ShortDescription = shortDesctiption;
			Description = description;
			Callback = CallbackNotImplemented;
		}
		
		public virtual bool IsCommand() {
			Console.WriteLine("Command's isCommand");
			return true;
		}
		
		protected void CallbackNotImplemented() {
		}

		public string Name {
			get;
			protected set;
		}
		
		public string ShortDescription {
			get;
			protected set;
		}
		
		public string Description {
			get;
			protected set;
		}
		
		public Action Callback {
			get;
			protected set;
		}
		
	}
	
    public class Menu :Command{
		public Menu(string name, string shortDesctiption, string description) 
			:base(name, shortDesctiption, description) {
			
			Callback = DummyCommand;
			Commands = new List<Command>();
		}
		
		public Command AddCommand(string name, string shortDesctiption, string description, Action callback) {
			Command cmd = new Command(name, shortDesctiption, description, callback);
			Commands.Add(cmd);
			return cmd;
		}

		public Command AddCommand(Command command) {
			Command cmd = command;
			Commands.Add(cmd);
			return cmd;
		}

		public Menu AddMenu(string name, string shortDesctiption, string description) {
			Menu menu = new Menu(name, shortDesctiption, description);
			Commands.Add(menu);
			return menu;
		}

	    public List<Command> Commands {
			get;
			protected set;
	    }

		public virtual bool IsCommand() {
			Console.WriteLine("Menu's isCommand");
			return false;
		}

		protected void DummyCommand() {
			Console.WriteLine("Menu is called");
		}
		
		public bool FindCommand(string name, out Command command) {
			foreach ( Command cmd in Commands ) {
				if (cmd.Name.Equals(name)) {
					command = cmd;
					return true;
				}				
			}
			command = null;
			return false;
		}
	}
	
	/// <summary>
	/// Base class for a command line interface
	/// </summary>
	public abstract class CommandLineInterface {

		public CommandLineInterface() {
			ExitFlag = false;
			RootMenu = new Menu("Main menu", "JQuants main menu", "Main menu provides access to the Logging, Trading\n"+ 
	                                                        "and other main system moudles");
			CurrentMenu = RootMenu;
			SystemMenu = new Menu("Commnad line interface system menu", "", "");
			SystemMenu.AddCommand("help", "List commands", "", PrintCommands);
		}
	
		public abstract string Name { get; }
		protected bool ExitFlag;
	
	
		public Menu RootMenu {
			get;
			private set;
		}
	
		public Menu SystemMenu {
			get;
			protected set;
		}
	
		public Menu CurrentMenu { 
			get;
			protected set;
		}
	
		protected void ProcessCommand(string cmdName) {
			Action action = null;
			Command cmd;
				
			if (cmdName.Equals("")) return;	
				
			// may be exit or help commands - always look in the system menu	
			bool found = SystemMenu.FindCommand(cmdName, out cmd);
			// not a system command try current menu then
			if (!found) found = CurrentMenu.FindCommand(cmdName, out cmd);
				
			if (found && cmd.IsCommand()) {
				cmd.Callback();
			} else if (found && !cmd.IsCommand()) {
				CurrentMenu = (Menu)cmd;
				PrintCommands();
			} else {
				PrintLine("Unknown command "+cmdName);
			}
		}
	
		protected void PrintCommands() {
			int index = 0;
			foreach ( Command cmd in CurrentMenu.Commands ) {
				PrintLine(cmd.Name + " - " + cmd.ShortDescription);
				index++;
			}
			if (index == 0) {
				PrintLine("No commands are available here");
			}
		}
	
		protected void PrintPrompt() {
			Print("$ ");
		}
	
		protected void PrintTitle() {
			PrintLine(Name);
			PrintLine("=====================================");
			PrintLine("Type 'help' to get commands list");
			PrintLine("Type 'exit' to exit");
		}
		
		protected abstract void PrintLine(string s);
		protected abstract void Print(string s);

	}
}