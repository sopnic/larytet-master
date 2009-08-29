
using System;
using System.Threading;

namespace JQuant
{

    /// <summary>
    /// Thread waiting forever for a message
    /// </summary>
    public class MailboxThread<Message> : IThread
    {

        public MailboxThread(string name, int mailboxCapacity)
        {
            _mailbox = new Mailbox<Message>(name, mailboxCapacity);
            _isAlive = false;
            _name = name;

            // add myself to the list of created mailboxes
            Resources.Threads.Add(this);

            _state = ThreadState.Initialized;
        }

        protected void Dispose()
        {
            if (_isAlive)
            {
                Console.WriteLine("MailboxThread " + GetName() + " disposed but not stopped");
            }

            _mailbox.Dispose();
            // _mailbox = null;

            _state = ThreadState.Destroyed;

            // remove myself from the list of created mailboxes
            Resources.Threads.Remove(this);

            _thread.Abort();
            _thread = null;
        }

        ~MailboxThread()
        {
            Console.WriteLine("MailboxThread " + _name + " destroyed");
        }

        /// <summary>
        /// loop forever (or until _isAlive is true)
        /// get messages from the mailbox, call HandleMessage
        /// </summary>
        protected virtual void Run()
        {
            _state = ThreadState.Started;
            _isAlive = true;
            while (_isAlive)
            {
                Message msg;
                bool result = _mailbox.Receive(out msg);
                if (result)
                {
                    HandleMessage(msg);
                }
            }

            _state = ThreadState.Stoped;
			Dispose();
        }

        /// <summary>
        /// application will override this method
        /// </summary>
        /// <param name="message">
        /// A <see cref="Message"/>
        /// </param>
        protected virtual void HandleMessage(Message message)
        {
            Console.WriteLine("I can't handle message " + message);
        }

        public bool Send(Message message)
        {
            bool result = _mailbox.Send(message);
            return result;
        }

        public void Stop()
        {
            _isAlive = false;
            _mailbox.Pulse();
        }

        /// <summary>
        /// call this method to start the thread - enters loop forever in the 
        /// method Run()
        /// </summary>
        public void Start()
        {
            _isAlive = true;
            _thread = new Thread(this.Run);
            _thread.Start();
        }

        public ThreadState GetState()
        {
            return _state;
        }

        public void WaitForTermination()
        {
            _thread.Join();
        }


        public string GetName()
        {
            return _mailbox.GetName();
        }

        protected Mailbox<Message> _mailbox;
        protected bool _isAlive;
        protected ThreadState _state;
        protected Thread _thread;
        protected string _name;
    }
}