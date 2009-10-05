
using System;
using System.Collections.Generic;
using FMRShell;

namespace JQuant
{
    public delegate void SendOrder(object source, OrderEventArgs e);

    public class OrderEventArgs : EventArgs
    {
        public LimitOrderParameters OrderParams;

        public OrderEventArgs(
            Option _opt,
            OrderType _ordType,
            int _quantity,
            double _price,
            TransactionType _trType
            )
        {
            OrderParams.Opt = _opt;
            OrderParams.OType = _ordType;
            OrderParams.Price = _price;
            OrderParams.Quantity = _quantity;
            OrderParams.TransType = _trType;
        }

    }

    /// <summary>
    /// Implements certain trading logic, gets market data, and orders fill information, 
    /// and sends order directions to the OrderMachine (which implement the Order FSM). 
    /// There may be multiple machines of this kind, each one implementing different trading logic.
    /// </summary>
    public class Algorithm
    {
        public Algorithm()
        {
            MFOrdProducer = new MaofOrderProducer(this);
        }

        public event SendOrder SendMaofOrder;

        public bool TradingLogic
        {
            get
            {
                return false;
            }

            set
            {
                if (SendMaofOrder != null)
                {
                    OrderEventArgs args = new OrderEventArgs(opt, ordT, qnty, prc, trT);
                    SendMaofOrder(this, args);
                }
            }
        }

        MaofOrderProducer MFOrdProducer;
        Option opt;
        OrderType ordT;
        int qnty;
        double prc;
        TransactionType trT;
    }

    public class MaofOrderProducer : IProducer<LimitOrderParameters>
    {
        public MaofOrderProducer(Algorithm Alg)
        {
            OrderListeners = new List<ISink<LimitOrderParameters>>(5);
            Alg.SendMaofOrder += new SendOrder(onSendMaofOrder);
            countOrders = 0;
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
            foreach (ISink<LimitOrderParameters> sink in OrderListeners)
            {
                sink.Notify(countOrders, args.OrderParams);
            }
        }

        public bool AddSink(ISink<LimitOrderParameters> sink)
        {
            OrderListeners.Add(sink);
            return true;
        }

        public bool RemoveSink(JQuant.ISink<LimitOrderParameters> sink)
        {
            OrderListeners.Remove(sink);
            return true;
        }

        int countOrders;
        protected static List<ISink<LimitOrderParameters>> OrderListeners;
    }
}