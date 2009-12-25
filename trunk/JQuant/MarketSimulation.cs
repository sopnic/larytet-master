
using System;

namespace MarketSimulation
{
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


    public class LimitOrder : JQuant.LimitOrderBase
    {
        JQuant.OrderType type;
    }



    /// <summary>
    /// Simulates real market behaviour. This simulaition is for back testing and requires
    /// feed of historical data. Lot of assumptions, like placed orders do not change the market, etc.
    /// </summary>
    public class Core : JQuant.IResourceStatistics
    {
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
            object security = securities[key];

            // do I see this security (this BNO_number) very first time ? add new entry to the hashtable
            // security is not in the table. this is not likely outcome. performance in not an issue at this point
            if (security == null) 
            {
                // clone the data first - cloning is expensive and happens only very first time i meet
                // specific security. in the subsequent calls to Notify() only relevant data will be updated
                security = (MarketData)data.Clone();
                securities[key] = security;

                // get the security from the hash table in all cases
                security = securities[key];
            }
            UpdateSecurity((MarketData)security, data);
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
        /// <param name="prev">
        /// A <see cref="MarketData"/>
        /// Currently stored data
        /// </param>
        /// <param name="curr">
        /// A <see cref="MarketData"/>
        /// New data
        /// </param>
        protected void UpdateSecurity(MarketData prev, MarketData curr)
        {
            // bump event counter
            eventsCount++;


            // compare field by field and update
            // check pending orders
        }


        /// <summary>
        /// Collection of all traded symbols (different BNO_Num for TASE) 
        /// </summary>
        protected System.Collections.Hashtable securities;

        protected int eventsCount;
        protected int ordersPlacedCount;
        protected int ordersFilledCount;
        protected int ordersCanceledCount;
        protected int ordersPendingCount;
    }
}
