
using System;
using System.Windows.Forms;

/// <summary>
/// classes supporting application GUI
/// all Linux/Windows interoperability issues should be handled here
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
			base.ScrollBars = ScrollBars.Vertical;
			base.ReadOnly = true;
			base.WordWrap = true;
		}
		
		public void Write(string s)
		{
			base.AppendText(s);
//			Truncate();
		}
		
		protected void Truncate()
		{
			int maxTextLength = 600;
			if (TextLength > maxTextLength)
			{
				Text = Text.Remove(0, (TextLength-maxTextLength));
			}
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
			base.ReadOnly = false;
			base.ScrollBars = ScrollBars.Horizontal;
		}
		
		public string ReadLine()
		{
			return base.Text;
		}
	}
	
}


