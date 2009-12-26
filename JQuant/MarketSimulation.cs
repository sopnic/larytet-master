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
        [Description("PartialFill")]
        PartialFill,
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
            md.lastTrade = this.lastTrade;
            md.lastTradeSize = this.lastTradeSize;
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
        public int lastTrade;
        public int lastTradeSize;

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
        public delegate void OrderCallback(int id, ReturnCode errorCode, int price, int quantity);


        /// <summary>
        /// There are two sources of the orders
        /// - system orders, placed by the trading algorithm
        /// - internally (by market simulation itself) placed orders based on the order book updates
        /// </summary>
        protected class LimitOrder : JQuant.LimitOrderBase
        {
            /// <summary>
            /// Order is placed by the system
            /// </summary>
            public LimitOrder(int id, int price, int quantity, JQuant.TransactionType transaction, OrderCallback callback)
            {
                Init(id, price, quantity, transaction, callback);
            }

            /// <summary>
            /// this constructor is used to create non-system orders, where order size is important
            /// I keep price and transaction type for internal checks only
            /// </summary>
            public LimitOrder(int price, int quantity, JQuant.TransactionType transaction)
            {
                Init(0, price, quantity, transaction, null);
            }


            public void UpdateQuantity(int quantity)
            {
                this.Quantity = quantity;
            }

            protected void Init(int id, int price, int quantity, JQuant.TransactionType transaction, OrderCallback callback)
            {
                this.id = id;
                this.callback = callback;
                this.Price = price;
                this.Quantity = quantity;
                state = OrderState.Pending;
                this.Transaction = transaction;
            }



            /// <summary>
            /// Is the order an internal order or placed by the applicaiton
            /// </summary>
            /// <returns>
            /// A <see cref="System.Boolean"/>
            /// Returns false if the order is internal order
            /// </returns>
            public bool SystemOrder()
            {
                return (callback != null);
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
            /// Order book at the moment when the order was placed. Queue size is amount of equal or better
            /// orders according to the order book. Every time deal is closed at better or equal price 
            /// i move the order in the queue - queueSize goes to zero.
            /// </summary>
            public int queueSize;

            /// <summary>
            /// total trading volume when the order was placed
            /// </summary>
            public int volume;            
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


        /// <summary>
        /// OrderQueue and OrderBook will call this method to let know upper layers that the
        /// system order (order place by the application) got fill
        /// </summary>
        protected delegate void FillOrder(LimitOrder order, int quantity);
        
        protected class OrderQueue : JQuant.IResourceStatistics
        {

            /// <summary>
            /// i keep price and transaction type only for debug purposes
            /// </summary>
            public OrderQueue(int securityId, int price, JQuant.TransactionType transaction, FillOrder fillOrder)
            {
                this.price = price;
                this.transaction = transaction;
                this.securityId = securityId;
                this.fillOrder = fillOrder;
                
                // crete queue of orders and preallocate a couple some entries.                
                orders = new System.Collections.Generic.LinkedList<MarketSimulation.Core.LimitOrder>();
            }

            /// <summary>
            /// Add a non-system (internal) order to the end of the queue
            /// I have different cases here
            /// - list is empty
            /// - list's tail is occupied by the system order
            /// - list's tail is occupied by non-sysem order
            /// </summary>
            public void AddOrder(int quantity)
            {
                if (quantity < 0)
                {
                    RemoveOrder(-quantity);
                    return;
                }
                // i am adding entries to the list and I have no idea how many threads
                // will attempt the trck concurrently
                lock (orders)
                {
                    countAddOrderTotal++;
                    
                    LimitOrder lastOrder = orders.Last.Value;
                    if ( (orders.Count == 0) ||  // empty list
                       lastOrder.SystemOrder() ) // last order is a system order
                    {
                        if (orders.Count == 0) countAddOrderToEmpty++;
                        else countAddOrderAfter++;
                        
                        // create a new order, add to the end of list
                        LimitOrder lo = new LimitOrder(this.price, quantity, this.transaction);
                        orders.AddLast(lo);
                    }
                    else // I keep the order queue as short as possible 
                    {    // increment quantity in the last (tail) internal order
                        countAddOrderUpdate++;
                        lastOrder.UpdateQuantity(quantity+lastOrder.Quantity);
                    }

                    sizeTotal += quantity;
                    sizeInternal += quantity;
                } // lock (orders)
            }

            /// <summary>
            /// Add system order - always to the end of list
            /// </summary>
            public void AddOrder(LimitOrder order)
            {
                lock (orders)
                {
                    orders.AddLast(order);
                    countAddSystemOrder++;
                    sizeSystem += order.Quantity;
                    sizeTotal += order.Quantity;
                }
            }

            /// <summary>
            /// Remove specified number of securities from the head of the list
            /// This method handles case when a system order gets fill
            /// </summary>
            public void RemoveOrder(int quantity)
            {
                lock (orders)
                {
                    counRemoveOrderTotal++;

                    // can I remove so many securities ?
                    if (sizeTotal < quantity)
                    {
                        System.Console.WriteLine(ShortDescription() + " Can't remove "+quantity+" from "+sizeTotal+" total");
                        quantity = sizeTotal;
                    }

                    // loop until quantity is removed or the list is empty
                    // i handle two cases here - remove of internal and remove of system orders
                    // this is order placed by the system .this is a simulation, so i have to "fix" the order queue.
                    // i will remove both system orders and internal orders - i double the quantity of orders removed. 
                    // This way i restore the queue like system order fill never happened.
                    while ((quantity > 0) && (orders.Count > 0))
                    {
                        LimitOrder lo = orders.First.Value;

                        if (lo.SystemOrder())
                        {
                            // quantity -= lo.Quantity;  - i am NOT going to do this. market does NOT see me
                            
                            int fillSize = Math.Min(quantity, lo.Quantity);  // can be partial fill - let upper layer handle this
                            // sizeSystem -= fillSize;
                            sizeSystem -= lo.Quantity; // no partial fills
                            
                            // add the order to the queue of filled orders 

                            // if not partial fill remove the order from the queue
                            if (fillSize >= lo.Quantity)
                            {
                                orders.RemoveFirst();
                            }
                            else // this is partial fill - just update the quantity
                            {
                                lo.UpdateQuantity(lo.Quantity - quantity);
                                // i have to skip the node in the list, but at this point i print error and remove the entry 
                                System.Console.WriteLine(ShortDescription() + " Don't know to hanle partial fills order "+lo.id);
                                orders.RemoveFirst();
                                // notify upper layer that the order got fill
                                fillOrder(lo, fillSize);
                            }
                        }
                        else  // internal order
                        {
                            if (lo.Quantity <= quantity)  // remove the whole node
                            {
                                quantity -= lo.Quantity;
                                orders.RemoveFirst();
                            }
                            else  // just update the Quantity in the first node
                            {
                                lo = new LimitOrder(this.price, lo.Quantity-quantity, this.transaction);
                                orders.First.Value = lo;
                                quantity = 0;
                            }
                            sizeInternal -= quantity;
                        }
                    }
                    sizeTotal -= quantity;
                }  // lock (orders)
                    
            }


            /// <summary>
            /// Remove system order. This is result of order cancel
            /// </summary>
            public void RemoveOrder(LimitOrder order)
            {
                lock (orders)
                {
                    System.Collections.Generic.LinkedListNode<LimitOrder> node = orders.Find(order);
                    if (node != null)
                    {
                        sizeSystem -= order.Quantity;
                        sizeTotal -= order.Quantity;
                        orders.Remove(node);
                    }
                    else
                    {
                        System.Console.WriteLine(ShortDescription()+"Can't remove order "+order.id+". Not found in the list");
                    }                    
                } 
            }


            protected string ShortDescription()
            {
                string sd = "Order queue "+this.securityId+" "+this.transaction.ToString()+" "+this.price;
                return sd;
            }
            
            protected System.Collections.Generic.LinkedList<LimitOrder> orders;
            
            public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(8);
                values = new System.Collections.ArrayList(8);
    
                names.Add("securityId"); values.Add(securityId);
                names.Add("price"); values.Add(price);
                names.Add("Sell"); values.Add(transaction == JQuant.TransactionType.SELL);
                names.Add("ListSize"); values.Add(orders.Count);
                names.Add("SizeTotal"); values.Add(sizeTotal);
                names.Add("sizeInternalTotal"); values.Add(sizeInternal);
                names.Add("sizeSystemTotal"); values.Add(sizeSystem);
                names.Add("countAddOrderToEmpty"); values.Add(countAddOrderToEmpty);
                names.Add("countAddOrderAfter"); values.Add(countAddOrderAfter);
                names.Add("countAddOrderUpdate"); values.Add(countAddOrderUpdate);
                names.Add("countAddOrderTotal"); values.Add(countAddOrderTotal);
                names.Add("countAddSystemOrder"); values.Add(countAddSystemOrder);
            }

            /// <summary>
            /// Returns aggregated size of all orders waiting in the queue
            /// </summary>
            public int GetSize()
            {
                return sizeTotal;
            }
            
            public int GetSizeInernal()
            {
                return sizeInternal;
            }
            
            protected int price;
            protected int securityId;
            protected JQuant.TransactionType transaction;

            /// <summary>
            /// total size of orders - summ of all bids (asks)
            /// </summary>
            protected int sizeTotal;
            
            /// <summary>
            /// size of internal orders
            /// </summary>
            protected int sizeInternal;
            
            /// <summary>
            /// size of orders placed by the application (by system)
            /// </summary>
            protected int sizeSystem;
            
            protected int countAddOrderToEmpty;
            protected int countAddOrderAfter;
            protected int countAddOrderUpdate;
            protected int countAddOrderTotal;
            protected int countAddSystemOrder;
            protected int counRemoveOrderTotal;
            protected FillOrder fillOrder;
        }  // class OrderQueue

        /// <summary>
        /// TASE supports order book of depth three - three slots for asks and three slots for bids
        /// I arrange book order as a list of slots ordered by price. The size of the list is not
        /// neccessary three
        /// </summary>
        protected class OrderBook : JQuant.IResourceStatistics
        {
            /// <summary>
            /// Create OrderBook - i keep two books for every security. One book for asks and another
            /// book for bids.
            /// I keep security id for debug
            /// Argument "fillOrder" is a method to call when a system order got fill
            /// </summary>
            public OrderBook(int securityId, JQuant.TransactionType transaction, FillOrder fillOrder)
            {
                this.securityId = securityId;
                this.transaction = transaction;
                this.fillOrder = fillOrder;
                
                slots = new System.Collections.SortedList();
            }


            /// <summary>
            /// If there is no system orders updating of the book is trivial - i just store the market data
            /// The things get interesting when a first order is placed. I allocate the order queues and 
            /// update will update the queues.
            /// </summary>
            public void Update(MarketData md)
            {
                if (sizeSystem <= 0) // most likely there are no system orders - i will store the market data and get out
                {                    // i want to be fast 
                    marketData = md;
                }
                else // there are pending system orders
                {
                    // i have previous record (marketData) and current record (md)
                    // let's figure out what happened
                    Update_trade(md);  // probably a trade ?
                    Update_queue(md); // probably some orders are pulled out or added ?

                    // finally replace the data
                    marketData = md;
                }
            }


            /// <summary>
            /// The method is called if in the Update() i discover that there was a trade
            /// I have to figure out size of the trade and remove the traded securities from the queue
            /// </summary>
            protected void Update_trade(MarketData md)
            {
                int tradeSize = (md.dayVolume - marketData.dayVolume);
                if (tradeSize < 0)
                {
                    System.Console.WriteLine(ShortDescription()+" negative change in day volume from "+md.dayVolume+" to "+ 
                                            marketData.dayVolume+" are not consistent");
                } 
                else if (tradeSize > 0) // there was a trade ? let's check the price of the deal and size of the deal
                {
                    if (tradeSize != md.lastTradeSize)  // sanity check - any misssing records out there ?
                    {
                        System.Console.WriteLine(ShortDescription()+" last trade size "+md.lastTradeSize+
                                                " and change in day volume from "+md.dayVolume+" to "+ marketData.dayVolume+" are not consistent");
                    }
                    // i remove the traded securities from the queue(s) starting from the best (head of the ordered list "slots")
                    while (slots.Count > 0)
                    {
                        OrderQueue orderQueue = (OrderQueue)slots[0];
                        
                        int size0 = orderQueue.GetSize();  // get queue size
                        orderQueue.RemoveOrder(tradeSize); // remove the trade
                        int size1 = orderQueue.GetSize();  // get queue size
                        
                        if (size1 == 0)  // if the queue empty - remove the queue
                        {
                            slots.RemoveAt(0);
                        }
                        int removed = size0 - size1;
                        tradeSize -= removed;
                        if (tradeSize == 0)  // i removed the trade from the order queue ?
                        {
                            break;
                        }
                    }
                    if (tradeSize > 0)
                    {
                        System.Console.WriteLine(ShortDescription()+" failed to remove the trade remains "+tradeSize);
                    }
                } // tradeSize > 0 - there was a trade
            }
                

            /// <summary>
            /// This method is called to check if there was a change in the order queues, for
            /// example some orders were pulled or some orders were added to the order queues
            /// </summary>
            protected void Update_queue(MarketData md)
            {
                OrderPair[] mdBookOrders;
                OrderPair[] marketDataBookOrders;
                if (this.transaction == JQuant.TransactionType.SELL) // i am an ask book
                {
                    mdBookOrders = md.ask;
                    marketDataBookOrders = marketData.ask;
                }
                else  // i am a bid book 
                {
                    mdBookOrders = md.bid;
                    marketDataBookOrders = marketData.bid;
                }

                int size = mdBookOrders.Length;
                for (int i = 0;i < size;i++)
                {
                    int marketDataPrice = marketDataBookOrders[i].price;
                    int mdPrice = mdBookOrders[i].price;
                    int queueIdx;
                    OrderQueue orderQueue;
                    lock (slots)
                    {
                        queueIdx = slots.IndexOfKey(marketDataPrice);
                        orderQueue = (OrderQueue)slots[queueIdx];
                    }
                    // simple case - size of orders changed
                    if ((mdPrice == marketDataPrice) && 
                        (mdBookOrders[i].size != marketDataBookOrders[i].size) && 
                        (orderQueue.GetSizeInernal() == mdBookOrders[i].size) )    // and size of the queue already represents that
                    {                                                              // probably thanks to Update_trade()
                    }
                    if ((mdPrice == marketDataPrice) &&                              
                        (mdBookOrders[i].size != marketDataBookOrders[i].size) && 
                        (orderQueue.GetSizeInernal() != mdBookOrders[i].size) )    // size changed because some of the orders pulled out
                    {
                        int addedOrders = mdBookOrders[i].size - orderQueue.GetSizeInernal();
                        // if result is negative the orders were removed (pulled by the market participants)
                        // orderQueue.AddOrder(int) handles negative numbers
                        orderQueue.AddOrder(addedOrders);
                    }
                    // price of orders changed. In some queues there are system orders. I want to keep the queues
                    // containing system orders
                    // if i do not have order queue with this price i create one
                    // i move all internal orders to another queue and remove the old queue if empty
                    // i add a new queue to the book
                    if (mdBookOrders[i].price != marketDataBookOrders[i].price)
                    {
                        OrderQueue orderQueueNew;
                        int queueIdxNew;
                        lock (slots)  // create a new order queue if there is no queue at this price exists
                        {
                            queueIdxNew = slots.IndexOfKey(mdPrice);
                            if (queueIdxNew < 0)
                            {
                                orderQueueNew = new OrderQueue(this.securityId, mdPrice, this.transaction, FillOrderCallback);
                                slots.Add(mdPrice, orderQueueNew);  // add newly created queue to the list of queues sorted by price
                            }
                            else
                            {
                                orderQueueNew = (OrderQueue)(slots[queueIdxNew]);
                            }
                        }
                        int sizeInternal = orderQueue.GetSizeInernal();  // remove all internal orders from the old queue
                        orderQueue.RemoveOrder(sizeInternal);
                        lock (slots)                    
                        {                                       // remove the queue from the list if empty
                            if (orderQueue.GetSize() <= 0) slots.RemoveAt(queueIdx);
                        }
                        sizeInternal = orderQueueNew.GetSizeInernal();   // add orders to the tail of the new queue
                        orderQueueNew.AddOrder(mdBookOrders[i].size - sizeInternal);  // orderQueue.AddOrder(int) handles both positive and negative arguments
                    }
                }
            }

            protected void FillOrderCallback(LimitOrder order, int fillSize)
            {
            }


                                                           

            /// <summary>
            /// Application calls the method to check if there is immediate fill is possible.
            /// For the ask order books of bids will be checked.
            /// </summary>
            /// <returns>
            /// A <see cref="System.Boolean"/>
            /// </returns>
            public bool CheckImmediateFill(LimitOrder order)
            {
                JQuant.TransactionType orderTransaction = order.Transaction;

                // book of asks for the buy order
                // book of bids for the sell order
                if (this.transaction == orderTransaction)
                {
                    System.Console.WriteLine(ShortDescription()+" Check fill for order "+order.id + " "+order.Transaction);
                    return false;
                }

                
                if (orderTransaction == JQuant.TransactionType.SELL) // check if there is a higher or equal bid 
                {
                    return (order.Price <= marketData.bid[0].price);
                }
                else // buy order - check if there is lower or equal ask
                {
                    return (order.Price >= marketData.ask[0].price);
                }
            }
            
            protected string ShortDescription()
            {
                string sd = "Order book "+this.securityId+" "+this.transaction.ToString()+" ";
                return sd;
            }
            
            public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(8);
                values = new System.Collections.ArrayList(8);
    
                names.Add("securityId"); values.Add(securityId);
                names.Add("Sell"); values.Add(transaction == JQuant.TransactionType.SELL);
                names.Add("ListSize"); values.Add(slots.Count);
                names.Add("sizeSystem"); values.Add(sizeSystem);
            }

            /// <summary>
            /// Slots are ordered by price
            /// Ask order queues are ordered from lower price to higher price and 
            /// book of bids aranged from higher price to lower price
            /// </summary>
            protected System.Collections.SortedList slots;

            /// <summary>
            /// total size of all placed by the system orders
            /// </summary>
            protected int sizeSystem;
            
            protected MarketData marketData;
            protected int securityId;
            protected JQuant.TransactionType transaction;
            protected FillOrder fillOrder;
        } // class OrderBook
        
        /// <summary>
        /// Use child class (wrapper) like MarketSimulationMaof to create instance of the MarketSimulation
        /// </summary>
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
            object fsm;


            lock (securities)
            {
                // hopefully Item() will return null if there is no key in the hashtable
                fsm = securities[key];
    
                // do I see this security very first time ? add new entry to the hashtable
                // this is not likely outcome. performance in not an issue at this point
                if (fsm == null) 
                {
                    // clone the data first - cloning is expensive and happens only very first time i meet
                    // specific security. in the subsequent calls to Notify() only relevant data will be updated
                    fsm = new FSM((MarketData)data.Clone());
                    securities[key] = fsm;
    
                    // get the security from the hash table in all cases
                    // this line can be removed
                    fsm = securities[key];
                }
            }
                    
            // i am going to call update in case if an order just being placed
            // by concurrently running thread
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
        /// Currently only limit ordres are supported
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// Returns false on failure. Calling method will analyze errorCode to figure out
        /// what went wrong with the order
        /// </returns>
        public bool PlaceOrder(int security, int price, int quantity, JQuant.TransactionType transaction, OrderCallback callback, ref ReturnCode errorCode)
        {
            bool res = false;

            do
            {
                object o = securities[security];

                if (o == null)
                {
                    errorCode = ReturnCode.UnknownSecurity;
                    break;
                }

                FSM fsm = (FSM)o;
                MarketData md = fsm.marketData;

                LimitOrder limitOrder = new LimitOrder(security, price, quantity, transaction, callback);

                // store the current queue size, trading volume, etc.
                limitOrder.volume = md.dayVolume;
                int placeInQueue = CalculatePlaceInQueue(md, price, transaction);
                limitOrder.queueSize = placeInQueue;

                // immediate execution ? probably market price
                if (placeInQueue < 0)
                {
                }

                // if there is still something to fill add to the list of pending orders
                if (limitOrder.Quantity > 0)
                {
                    lock (fsm)
                    {
                        ((FSM)fsm).orders.Add(limitOrder);
                    }
                }
                
                res = true;
            }
            while (false);


            return res;
        }

        /// <summary>
        /// Calculate position of an order in the order book given the book and the 
        /// price of the order (assumed limit order)
        /// </summary>
        protected static int CalculatePlaceInQueue(MarketData md, int price, JQuant.TransactionType transaction)
        {
            int placeInQueue = 0;

            return placeInQueue;
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
