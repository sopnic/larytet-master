
using System;
using System.Collections.Generic;
using FMRShell;

namespace JQuant
{
    /// <summary>
    /// The algorithm machine does only one thing - sends orders data 
    /// to be produced by the order producer(s) (order FSM)
    /// </summary>
    /// <param name="source"><see cref="System.Object"/>,
    /// an algorithm which produces the order parameters</param>
    /// <param name="e"><see cref="JQuant.OrderEventsArgs"/>,
    /// containes all the data needed to the FSM machine to
    /// successfully process an order</param>
    public delegate void SendOrder(object source, OrderEventArgs e);

    /// <summary>
    /// Contains the data passed to the FSM machine
    /// </summary>
    public class OrderEventArgs : EventArgs
    {
        /// <summary>
        /// Once an algorithm creates an order directive, 
        /// no one but the algorithm itself can change 
        /// its parameters, through another correcting 
        /// directive. To the outside world it appears as read only.
        /// </summary>
        /// <returns>A <see cref="JQuant.LimitOrderParameters"/></returns>
        public LimitOrderParameters GetOrderParams()
        {
            return _orderParams;
        }

        public OrderEventArgs(
            Option _opt,
            OrderType _ordType,
            int _quantity,
            double _price,
            TransactionType _trType
            )
        {
            _orderParams.Opt = _opt;
            _orderParams.OType = _ordType;
            _orderParams.Price = _price;
            _orderParams.Quantity = _quantity;
            _orderParams.TransType = _trType;
        }

        LimitOrderParameters _orderParams;
    }

    /// <summary>
    /// Implements certain trading logic, gets market data, and orders fill information, 
    /// and sends order directives to the OrderMachine (which implement the Order FSM). 
    /// TODO: implement it as a while loop (until some stop criteria is met).
    /// There may be multiple machines of this kind, each one implementing different trading logic.
    /// </summary>
    public class Algorithm
    {
        public Algorithm()
        {
            MFOrdProducer = new MaofOrderProducer(this);
        }

        public event SendOrder SendMaofOrder;

        /// <summary>
        /// A trading algorithm is implemented as a while loop,
        /// running until an indication  stop appears.
        /// A stop indication may appear as a result of:
        /// - end of trading session (e.g. end of the day)
        /// - abnormal operating conditions (e.g. trading results non-compliance)
        /// - user interference (stop trading on the human operator discretion)
        /// Also, the thread may be sleeping until some event is raised.
        /// </summary>
        /// <returns>A<see cref="System.Boolean"/> 
        /// indication that the algorithm stopped running</returns>
        public bool TradingLogic()
        {
            bool stopped = false;
            bool trade = false;
            SomeTradingAlgo TrAlgo = new SomeTradingAlgo();
            while (!stopped)
            {
                //do the work - implement the algorithm's logic
                trade = TrAlgo.SomeDoTheJob();

                // once the algorithm has reached the decision to trade,
                // initialize all the OrderEvemntArgs data:

                if (trade)      //TrAlgo.SomeDoTheJob() yielded trading signal
                {
                    if (SendMaofOrder != null)  //check that there are subscribers (order FSMs) to the trading events 
                    {
                        OrderEventArgs args = new OrderEventArgs(TrAlgo.opt, TrAlgo.ordT, TrAlgo.qnty, TrAlgo.prc, TrAlgo.trT);
                        SendMaofOrder(this, args);
                    }
                }
            }

            return stopped; //indication that the algorithm stopped running
        }

        /// <summary>
        /// Order processor - data consumer, also a mailbox to which
        /// order directives are sent through the mail.
        /// </summary>
        MaofOrderFSM MFOrderFSM;
        /// <summary>
        /// An alternative way to send order directive is to use IProducer 
        /// interface, while MFOrderFSM serving as a data sink (ISink).
        /// </summary>
        MaofOrderProducer MFOrdProducer;
    }//class Algorithm

    /// <summary>
    /// this one is a placeholder for an actual algorithm
    /// contains a single function implementing the trading logic
    /// and trading parameters. It's intentionally done modular, 
    /// in order to easily replace it with some other logic.
    /// </summary>
    public class SomeTradingAlgo
    {
        //Order parameters
        public Option opt
        {
            get;
            protected set;
        }

        public OrderType ordT
        {
            get;
            protected set;
        }

        public int qnty
        {
            get;
            protected set;
        }

        public double prc
        {
            get;
            protected set;
        }

        public TransactionType trT
        {
            get;
            protected set;
        }

        public SomeTradingAlgo()
        {
        }

        public bool SomeDoTheJob()
        {
            //TODO:
            // here will come an if-else loop (or switch)
            // if some trading criteria is met, assign 
            // all the parameters with appropriate values
            // then return true. else do nothing, return false.
            return false;
        }
    }




    /// <summary>
    /// Implements IProducer interface, an alternative way
    /// (along with sending mail message) to pass order 
    /// data to data consumer/sink - FSM machine.
    /// </summary>
    public class MaofOrderProducer : IProducer<LimitOrderParameters>
    {
        public MaofOrderProducer(Algorithm Alg)
        {
            OrderListeners = new List<IConsumer<LimitOrderParameters>>(5);
            Alg.SendMaofOrder += new SendOrder(onSendMaofOrder);
            countOrders = 0;
            Name = "MaofOrder";
        }

        protected void onSendMaofOrder(object source, OrderEventArgs args)
        {
            //I'm not sure that *foreach* is the best solution here
            //since we need only one listener which will take care of order processing
            //on the other hand there may be one or more listeners performing different
            //logging / computing tasks, not necessarily related to the main task of 
            //order processing. Need to think more about it. In any case, we need here a 
            //mechanism that will assure that that the same order won't be sent twice (or more)
            //through two (or more) separate FSMs, registered by mistake at the same Algo.
            foreach (IConsumer<LimitOrderParameters> consumer in OrderListeners)
            {
                consumer.Notify(countOrders, args.GetOrderParams());
            }
        }

        public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
        {
            names = new System.Collections.ArrayList(0);
            values = new System.Collections.ArrayList(0);

        }

        public string Name
        {
            get;
            set;
        }

        public bool AddConsumer(IConsumer<LimitOrderParameters> consumer)
        {
            OrderListeners.Add(consumer);
            return true;
        }

        public bool RemoveConsumer(JQuant.IConsumer<LimitOrderParameters> consumer)
        {
            OrderListeners.Remove(consumer);
            return true;
        }

        int countOrders;
        protected static List<IConsumer<LimitOrderParameters>> OrderListeners;
    }//class MaofOrderProducer
}