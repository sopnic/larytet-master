
using System;
using System.Windows.Forms;

/// <summary>
/// classes supporting application GUI
/// </summary>
namespace JQuantForms
{
	/// <summary>
	/// Graphic console - exactly like text based console
	/// This console for output only
	/// </summary>
	public class ConsoleOut :TextBox
	{
		public ConsoleOut()
		{
			base.Multiline = true;
		}
		
		public void WriteLine(string s)
		{
			base.AppendText(s);
		}
	}
	
	/// <summary>
	/// Graphic console - exactly like text based console
	/// This console for input only
	/// </summary>
	public class ConsoleIn :TextBox
	{
		public ConsoleIn()
		{
			base.Multiline = false;
		}
		
		public string ReadLine()
		{
			return base.Text;
		}
	}
	
}


