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
			Parent = this;
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
			menu.Parent = this;
			return menu;
		}

	    public List<Command> Commands {
			get;
			protected set;
	    }
		
		public Menu Parent {
			get;
			set;
		}
		                    

		public override bool IsCommand() {
			return false;
		}

		protected void DummyCommand() {
			Console.WriteLine("Menu " + Name + ": callback is called. Probably should be command");
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
	
	public interface IWrite {
		void WriteLine(string s);
		void Write(string s);
	}
	
	public class CommandLineInterface {

		public CommandLineInterface(string title, IWrite iWrite) {
			WriteInterface = iWrite;
			Name = title;
			RootMenu = new Menu("Main menu", "JQuants main menu", "Main menu provides access to the Logging, Trading\n"+ 
	                                                        "and other main system moudles");
			CurrentMenu = RootMenu;
			SystemMenu = new Menu("Commnad line interface system menu", "", "");
			SystemMenu.AddCommand("help", "List commands", "", PrintCommands);
			SystemMenu.AddCommand("..", "One level up", "", OneLevelUp);
			SystemMenu.AddCommand("~", "Go to the main menu (root)", "", GotoRootMenu);
		}
	
		public string Name { get; set;}
	
	
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
		
		protected IWrite WriteInterface;
	
		public void ProcessCommand(string cmdName) {
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
				WriteInterface.WriteLine("Unknown command "+cmdName);
			}
		}
		
		private void OneLevelUp() {
			CurrentMenu = CurrentMenu.Parent;
			PrintCommands();
		}

		private void GotoRootMenu() {
			CurrentMenu = RootMenu;
			PrintCommands();
		}

		protected void PrintCommands() {
			PrintTitle();
			int index = 0;
			foreach ( Command cmd in CurrentMenu.Commands ) {
				WriteInterface.WriteLine(cmd.Name + " - " + cmd.ShortDescription);
				index++;
			}
			if (index == 0) {
				WriteInterface.WriteLine("No commands are available here");
			}
		}
	
		public void PrintPrompt() {
			WriteInterface.Write("$ ");
		}
	
		public void PrintTitle() {
			WriteInterface.WriteLine(Name + " - " + CurrentMenu.Name);
			WriteInterface.WriteLine("=====================================");
			WriteInterface.WriteLine("help, exit, .., ~");
			WriteInterface.WriteLine("");
		}
		
		// Print methods will be replaced by Read/Write I/O interface
		// I want this part as flexible as possible to allow to work with 
		// Console, Telnet, or just anything else.

	}
}