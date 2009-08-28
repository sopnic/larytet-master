
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
            setTextDelegate = new SetTextDelegate(SetText);
        }
		
		public void Write(string s)
		{
            if (this.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                this.Invoke(setTextDelegate, new object[] { s });
            }
            else
            {
                // It's on the same thread, no need for Invoke
                SetText(s);
            }
		}

        private void SetText(string s)
        {
            const int maxTextLength = 32*1024;
            
            // prepare string (truncate if neccessary)
            
            int totalLength = (s.Length+TextLength);
            if (totalLength > maxTextLength)
            {
                // truncate by at least 30% to avoid truncation on every string
                int removeCount = Math.Max(totalLength - maxTextLength, (int)(maxTextLength*0.3F));
                s = Text + s;
                s = s.Remove(0, removeCount);

                Text = s;
                
                SelectionStart = Text.Length;
            }
            else
            {
                base.AppendText(s);
            }

            base.ScrollToCaret();
            base.Refresh();                
        }

        protected delegate void SetTextDelegate(string s);
        SetTextDelegate setTextDelegate;
		
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


