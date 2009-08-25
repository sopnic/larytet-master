using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ConsoleApplication1
{
    class Connector
    {
        string MyUserName = "aryeh";
        string MyAS400PassWord = "abc123";
        string AppPassword ="";     // an empty string, this parameter is not in use, but the method requires it anyway
        public string ErrMsg = "";    // not sure if I need to initialize its value
        int MySessionId;
        TaskBarLib.User MyUserClass = new TaskBarLib.UserClass();

        public int StartConn()
        {
            int LoginReturnCode = this.MyUser.Login(this.MyUserName, this.MyAS400PassWord, AppPassword, out this.ErrMsg, out this.MySessionId);
            return LoginReturnCode;
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Connector NewConn = new Connector();
            int x = NewConn.StartConn();
            Console.WriteLine("return code: " + x);
            Console.WriteLine("ErrorMsg: " + NewConn.ErrMsg);
            Console.ReadLine();
        }
    }
}
