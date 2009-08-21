using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;


namespace JQuant {

	/// <summary>
	/// objects implementing this interface will be forwarded to all commands 
	/// to allow output to the correct I/O
	/// </summary>
	public interface IWrite {
		void WriteLine(string s);
		void Write(string s);
	}
	
	
	/// <summary>
	/// Class defines a command of the command line interface
	/// </summary>
	public class Command {

		public delegate void Callback(IWrite iWrite, string cmdName, object[] cmdArguments);

		public Command(string name, string shortDesctiption, string description, Callback callback) {
			Name = name;
			ShortDescription = shortDesctiption;
			Description = description;
			Handler = callback;
		}

		public Command(string name, string shortDesctiption, string description) {
			Name = name;
			ShortDescription = shortDesctiption;
			Description = description;
			Handler = new Callback(Unsupported);
		}
		
		void Unsupported(IWrite iWrite, string cmdName, object[] cmdArguments) {
			iWrite.WriteLine("Unsupported command "+Name);
		}
		
		public virtual bool IsCommand() {
			return true;
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

		/// <value>
		/// this guy will be reloaded by the specific application command 
		/// </value>
		public Callback Handler {
			get;
			protected set;
		}
		
	}
	
	/// <summary>
	/// Class defines a menu (kind of command) of the command line interface
	/// </summary>
    public class Menu :Command{
		public Menu(string name, string shortDesctiption, string description) 
			:base(name, shortDesctiption, description) {
			Parent = this;
			Handler = new Callback(DummyCommand);
			Commands = new List<Command>();
		}
		
		public Command AddCommand(string name, string shortDesctiption, string description, Callback callback) {
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

		protected void DummyCommand(IWrite iWrite, string cmdName, object[] cmdArguments) {
			Console.WriteLine("Menu " + Name + 
				": callback is called. Probably should be command");
		}
		
		/// <summary>
		/// look in the list of commands 
		/// </summary>
		/// <param name="cmdLine">
		/// A <see cref="System.String"/>
		/// look for a command specified by this command line
		/// currently no arguments are expected, case not sensitive
		/// </param>
		/// <param name="command">
		/// A <see cref="Command"/>
		/// returns found command
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// returns true if command is found
		/// </returns>
		public bool FindCommand(string cmdLine, out Command command) {
			// case not sensitive - convert everything to lower case
			string cmdLineLower = cmdLine.ToLower();
			foreach ( Command cmd in Commands ) {
				string cmdNameLower = cmd.Name.ToLower();
				if (cmdNameLower.Equals(cmdLineLower)) {
					command = cmd;
					return true;
				}				
			}
			command = null;
			return false;
		}
	}
	
	/// <summary>
	/// enigne behind command line interface
	/// application will usually create a single object of ths type
	/// the object keeps tree of commands and menus
	/// </summary>
	public class CommandLineInterface {

		public CommandLineInterface(string title) {
			Name = title;
			RootMenu = new Menu("Main menu", "JQuants main menu", "Main menu provides access to the Logging, Trading\n"+ 
	                                                        "and other main system moudles");
			CurrentMenu = RootMenu;
			SystemMenu = new Menu("Commnad line interface system menu", "", "");
			SystemMenu.AddCommand("help", "List commands", "", PrintCommandsFull);
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
			
		public void ProcessCommand(IWrite iWrite, string cmdName) {
			Command cmd;
				
			if (cmdName.Equals("")) return;	
				
			// may be exit or help commands - always look in the system menu	
			bool found = SystemMenu.FindCommand(cmdName, out cmd);
			// not a system command try current menu then
			if (!found) found = CurrentMenu.FindCommand(cmdName, out cmd);
				
			if (found && cmd.IsCommand()) {
				// temporary no parsing for the arguments
				cmd.Handler(iWrite, cmdName, null);
			} else if (found && !cmd.IsCommand()) {
				CurrentMenu = (Menu)cmd;
				PrintCommands(iWrite);
			} else {
				iWrite.WriteLine("Unknown command "+cmdName);
			}
		}
		
		private void OneLevelUp(IWrite iWrite, string cmdName, object[] cmdArguments) {
			CurrentMenu = CurrentMenu.Parent;
			PrintCommands(iWrite);
		}

		private void GotoRootMenu(IWrite iWrite, string cmdName, object[] cmdArguments) {
			CurrentMenu = RootMenu;
			PrintCommands(iWrite);
		}

		public void PrintCommandsFull(IWrite iWrite, string cmdName, object[] cmdArguments) {
			PrintCommands(iWrite, cmdName, cmdArguments, true);
		}
		
		public void PrintCommands(IWrite iWrite, string cmdName, object[] cmdArguments) {
			PrintCommands(iWrite, cmdName, cmdArguments, false);
		}

		public void PrintCommands(IWrite iWrite, string cmdName, object[] cmdArguments, bool longDescr) {
			PrintTitle(iWrite);
			foreach ( Command cmd in CurrentMenu.Commands ) {
				iWrite.WriteLine(cmd.Name + " - " + cmd.ShortDescription);
				if (longDescr) {
					iWrite.WriteLine(cmd.Description);
					iWrite.WriteLine("");
				}
			}
			if (CurrentMenu.Commands.Count == 0) {
				iWrite.WriteLine("No commands are available here");
			}
		}

		public void PrintCommands(IWrite iWrite) {
			PrintCommands(iWrite, "", null, false);
		}

		public void PrintPrompt(IWrite iWrite) {
			iWrite.Write("$ ");
		}
	
		public void PrintTitle(IWrite iWrite) {
			iWrite.WriteLine(Name + " - " + CurrentMenu.Name);
			iWrite.WriteLine("=====================================");
			iWrite.WriteLine("help, exit, one level up - .., main menu - ~");
			iWrite.WriteLine("");
		}
		
	}
}