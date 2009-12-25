
using System;
using System.ComponentModel;

namespace MarketSimulation
{
    public enum ReturnCode
    {
        [Description("UnknownError")]
        UnknownError,
        [Description("OutOfMemory")]
        OutOfMemoryError,
        [Description("Fill")]
        Fill,
        [Description("UnknownSecurity")]
        UnknownSecurity
    };
    
    public enum OrderState
    {
        [Description("Pending")]
        Pending,
        [Description("Canceled")]
        Canceled,
        [Description("Fill")]
        Fill,
        [Description("PartialFill")]
        PartialFill
    };
    
    public class OrderPair : ICloneable
    {
        public int price;
        public int size;

        public object Clone()
        {
            OrderPair op = new OrderPair();
            op.price = this.price;
            op.size = this.size;
            return op;
        }
    }
    
    /// <summary>
    /// Describes an asset, for example, TASE option this class is used in the MarketSimulation
    /// all data (prices / quantities) are integers. If required in cents/agorots
    /// This object is expensive to create. Application will reuse objects or create pool of
    /// objects
    /// </summary>
    public class MarketData : ICloneable
    {
        /// <summary>
        /// On TASE order book has depth 3
        /// </summary>
        public MarketData()
        {
            Init(3);
        }
        
        public MarketData(int marketDepth)
        {
            Init(marketDepth);
        }


        protected void Init(int marketDepth)
        {
            bid = new OrderPair[marketDepth];
            ask = new OrderPair[marketDepth];


            for (int i = 0;i < marketDepth;i++)
            {
                bid[i] = new OrderPair();
                ask[i] = new OrderPair();
            }
        }
        

        public object Clone()
        {
            MarketData md = new MarketData(bid.Length);

            md.ask = (OrderPair[])this.ask.Clone();
            md.bid = (OrderPair[])this.bid.Clone();
            md.id = this.id;
            md.lastDeal = this.lastDeal;
            md.lastDealSize = this.lastDealSize;
            md.dayVolume = this.dayVolume;
            md.dayTransactions = this.dayTransactions;
            
            return md;
        }
        
        // security ID - unique number
        public int id;

        // three (pr more depending on the market depth) best asks and bids - price and size
        // best bid and best ask at the index 0
        public OrderPair[] bid;
        public OrderPair[] ask;
        

        // last deal price and size
        public int lastDeal;
        public int lastDealSize;

        // Aggregated trading data over the trading period (day)
        public int dayVolume;        //volume
        public int dayTransactions;  //number of transactions
    }


    /// <summary>
    /// Simulates real market behaviour. This simulaition is for back testing and requires
    /// feed of historical data. Lot of assumptions, like placed orders do not change the market, etc.
    /// </summary>
    public class Core : JQuant.IResourceStatistics
    {
        /// <summary>
        /// Market simulation will call this method when/if order state changes
        /// Delegate method allows to place orders from different threads and state
        /// machines 
        /// </summary>
        public delegate void OrderCallback(int id, ReturnCode errorCode);

        protected class LimitOrder : JQuant.LimitOrderBase
        {
            public LimitOrder(int id, int price, int quantity, OrderCallback callback)
            {
                this.id = id;
                this.callback = callback;
                this.Price = price;
                this.Quantity = quantity;
                state = OrderState.Pending;
            }
            
            /// <summary>
            /// unique ID of the order
            /// This field is provided by the application and inored by the simulation
            /// </summary>
            public int id;


            /// <summary>
            /// method to call when the order status changes
            /// </summary>
            public OrderCallback callback;


            /// <summary>
            /// State of the order - pending, canceled, etc.
            /// </summary>
            public OrderState state;


            /// <summary>
            /// Order book at the moment when the order was placed
            /// </summary>
            public MarketData marketData;
        }


        /// <summary>
        /// I keep object of this type for every security
        /// </summary>
        protected class FSM
        {
            public FSM(MarketData marketData)
            {
                orders = new System.Collections.ArrayList(3);
                this.marketData = marketData;
            }
            
            /// <summary>
            /// available at the moment market data including security ID
            /// </summary>
            public MarketData marketData;
            
            /// <summary>
            /// List of pending orders
            /// </summary>
            public System.Collections.ArrayList orders;
        }
        
        protected Core()
        {
            // create hash table where all securities are stored
            securities = new System.Collections.Hashtable(200);
        }

        public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
        {
            names = new System.Collections.ArrayList(8);
            values = new System.Collections.ArrayList(8);

            names.Add("Events"); values.Add(eventsCount);
            names.Add("OrdersPlaced"); values.Add(ordersPlacedCount);
            names.Add("OrdersFilled"); values.Add(ordersFilledCount);
            names.Add("OrdersCanceled"); values.Add(ordersCanceledCount);
            names.Add("OrdersPending"); values.Add(ordersPendingCount);
            names.Add("Securities"); values.Add(securities.Count);
        }

        /// <summary>
        /// The method is being called by Event Generator to notify the market simulation, that
        /// there is a new event went through, for example change in the order book
        /// Argument "data" can be reused by the calling thread. If the data to be processed 
        /// asynchronously Notify() should clone the object
        /// </summary>
        public void Notify(int count, MarketData data)
        {
            // GetKey() will return (in the simplest case) BNO_number (boxed integer)
            object key = GetKey(data);

            // hopefully Item() will return null if there is no key in the hashtable
            object fsm = securities[key];

            // do I see this security very first time ? add new entry to the hashtable
            // this is not likely outcome. performance in not an issue at this point
            if (fsm == null) 
            {
                // clone the data first - cloning is expensive and happens only very first time i meet
                // specific security. in the subsequent calls to Notify() only relevant data will be updated
                fsm = new FSM((MarketData)data.Clone());
                securities[key] = fsm;

                // get the security from the hash table in all cases
                fsm = securities[key];
                
                // i am going to call update in case if an order just being placed
                // by concurrently running thread
            }

            UpdateSecurity((FSM)fsm, data);
        }


        /// <summary>
        /// Returns key for the hashtable
        /// The implemenation is trivial - return BNO_Num
        /// </summary>
        protected static object GetKey(MarketData data)
        {
            return data.id;
        }


        /// <summary>
        /// If there is any pending (waiting execution) orders check if I can execute any,
        /// shift the orders position in the queue, etc.
        /// In all cases replace the current value with new one.
        /// </summary>
        /// <param name="fsm">
        /// A <see cref="FSM"/>
        /// Currently stored data
        /// </param>
        /// <param name="curr">
        /// A <see cref="MarketData"/>
        /// New data
        /// </param>
        protected void UpdateSecurity(FSM fsm, MarketData marketData)
        {
            // bump event counter
            eventsCount++;

            int ordersCount = 0;

            // compare field by field and update
            // Fast and dirty - replace the data in the FSM
            lock (fsm)
            {
                fsm.marketData = (MarketData)marketData.Clone();
                ordersCount = fsm.orders.Count;
            }

            // check pending orders
            if (ordersCount != 0)
            {
                LimitOrder[] limitOrders;

                // i assume that the list is not long - convert to array
                // this way i do not have to synchronize the whole matching procedure, but only
                // hopefully quick list-to-array conversion
                lock (fsm)
                {
                    limitOrders = (LimitOrder[])fsm.orders.ToArray();
                }
                foreach (LimitOrder limitOrder in limitOrders)
                {
                    MatchOrder(fsm, limitOrder);
                }
            }
        }

        /// <summary>
        /// Match the order if possible. Current approach is "no partial fills"
        /// I want to see for sell (buy) order
        /// - bid (or ask) with at least the order price
        /// - deals done after the order placed at prices better or equal 
        /// to the order price. number of deals should be at least total size of bids (asks) 
        /// at the moment the order placed
        /// </summary>
        protected void MatchOrder(FSM fsm, LimitOrder limitOrder)
        {
        }

        /// <summary>
        /// currently only limit ordres are supported
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// Returns false on failure. Calling method will analyze errorCode to figure out
        /// what went wrong with the order
        /// </returns>
        public bool PlaceOrder(int security, int price, int quantity, OrderCallback callback, ref ReturnCode errorCode)
        {
            bool res = false;

            do
            {
                object fsm = securities[security];

                if (fsm == null)
                {
                    errorCode = ReturnCode.UnknownSecurity;
                    break;
                }

                LimitOrder limitOrder = new LimitOrder(security, price, quantity, callback);

                lock (fsm)
                {
                    // store the current queue size, trading volume, etc.
                    // limitOrder
                    ((FSM)fsm).orders.Add(limitOrder);
                }
                
                res = true;
            }
            while (false);


            return res;
        }


        /// <summary>
        /// Collection of all traded symbols (different BNO_Num for TASE)
        /// I keep objects of type FSM in the hashtable
        /// </summary>
        protected System.Collections.Hashtable securities;

        protected int eventsCount;
        protected int ordersPlacedCount;
        protected int ordersFilledCount;
        protected int ordersCanceledCount;
        protected int ordersPendingCount;
    }
}
