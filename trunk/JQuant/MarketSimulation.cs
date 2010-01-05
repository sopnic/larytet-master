
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
		public OrderPair(int price, int size)
		{
			this.price = price;
			this.size = size;
		}
		
		public OrderPair()
		{
		}
		
        public int price;
        public int size;

        public object Clone()
        {
            OrderPair op = new OrderPair();
            op.price = this.price;
            op.size = this.size;
            return op;
        }
		
		/// <summary>
		/// I have no idea if CSharp array knows to clone itself
		/// I put here some trivial implementation
		/// </summary>
		public static OrderPair[] Clone(OrderPair[] src)
		{
			OrderPair[] dst = new OrderPair[src.Length];
			for (int i = 0;i < dst.Length;i++)
			{
				dst[i] = (OrderPair)src[i].Clone();
			}
			
			return dst;
		}
		
		public static string ToString(OrderPair[] src)
		{
			string res = "";
			foreach (OrderPair op in src)
			{
				res = res + op.price+":"+op.size+" ";
			}
				
			return res;
		}
    }

    /// <summary>
    /// Describes an asset, for example, Maof option on TASE. 
    /// This class is used in the MarketSimulation. 
    /// All data (prices / quantities) are integers. If required in cents/agorots. 
    /// This object is expensive to create. 
    /// Application will reuse objects or create pool of objects.
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


            for (int i = 0; i < marketDepth; i++)
            {
                bid[i] = new OrderPair();
                ask[i] = new OrderPair();
            }
        }


        public object Clone()
        {
            MarketData md = new MarketData(bid.Length);

            md.ask = OrderPair.Clone(this.ask);
            md.bid = OrderPair.Clone(this.bid);
            md.id = this.id;
            md.lastTrade = this.lastTrade;
            md.lastTradeSize = this.lastTradeSize;
            md.dayVolume = this.dayVolume;

            return md;
        }
		
		public override string ToString()
		{
			string res = "id="+id+" bids="+OrderPair.ToString(bid)+" asks="+OrderPair.ToString(ask)+" lt="+lastTrade+":"+lastTradeSize+" dv="+dayVolume;
			return res;
		}

        // security ID - unique number
        public int id;

        // three (or more, depending on the market depth) best asks and bids - price and size
        // best bid and best ask at the index 0
        public OrderPair[] bid;
        public OrderPair[] ask;


        // last deal price and size
        public int lastTrade;
        public int lastTradeSize;

        // Aggregated trading data over the trading period (day)
        public int dayVolume;        //volume
        //I removed dayTransactions, dayVolume alone can serve as new transaction indicator
    }


    /// <summary>
    /// Simulates real market behaviour. This simulaition is for back testing and requires
    /// feed of historical data. Lot of assumptions, like placed orders do not change the market, etc.
    /// 
    /// Notation:
    ///     there are two types of limit orders in the market simulation:
    ///     - "system" order is limit order simulated by the trading algorithm
    ///     - "internal" order is limit order generated from the historical log, internally
    ///     For the sake of simplicity we assume that system orders are invisible to the market, 
    ///     and they don't affect trading (no. of transactions etc.). 
    /// </summary>
    public class Core : JQuant.IResourceStatistics, IDisposable
    {
        /// <summary>
        /// Market simulation will call this method if and when order state changes.
        /// Delegate method allows to place orders from different threads and state machines. 
        /// </summary>
        public delegate void OrderCallback(int id, ReturnCode errorCode, int price, int quantity);


        /// <summary>
        /// There are two sources of the orders placed by the trading algorithm
        /// - system orders (simulated) - placed by the trading algorithm
        /// - non-system orders, placed internally (by market simulation itself), 
        ///   based on the order book updates from the log
        /// </summary>
        protected class LimitOrder : JQuant.LimitOrderBase
        {
            /// <summary>
            /// Order is placed by the system (algorithm)
            /// </summary>
            public LimitOrder(int id, int price, int quantity, JQuant.TransactionType transaction, OrderCallback callback)
            {
                Init(id, price, quantity, transaction, callback);
            }

            /// <summary>
            /// this constructor is used to create non-system (internal, log) orders, 
            /// where only order size is important. 
            /// I keep price and transaction type for internal checks only.
            /// </summary>
            public LimitOrder(int price, int quantity, JQuant.TransactionType transaction)
            {
                Init(0, price, quantity, transaction, null);
            }

            public void UpdateQuantity(int quantity)
            {
                this.Quantity = quantity;
            }

            /// <summary>
            /// Builds an order object tracking real (internal) or simulated (system) orders.
            /// </summary>
            /// <param name="id">Order's id number</param>
            /// <param name="price">limit price</param>
            /// <param name="quantity">order's quantity</param>
            /// <param name="transaction">buy or sell</param>
            /// <param name="callback">callback method </param>
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
            /// Is the order an system (placed by the algorithm, True) 
            /// or internal (read from the log, False)?
            /// </summary>
            /// <returns>
            /// A <see cref="System.Boolean"/> false if the order is internal order
            /// </returns>
            public bool SystemOrder()
            {
                return (callback != null);
            }

            /// <summary>
            /// unique ID of the order
            /// This field is provided by the application and ignored by the simulation
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
            /// daily aggregate trading volume when the order was placed
            /// change in its value indicates new transaction
            /// </summary>
            public int volume;
        }


        /// <summary>
        /// I keep object of this type for every security
        /// </summary>
        protected class FSM
        {
            public FSM(MarketData marketData, FillOrderBook fillOrderCallback)
            {
                // orders = new System.Collections.ArrayList(3);
                this.marketData = marketData;
                orderBookAsk = new OrderBook(marketData.id, JQuant.TransactionType.SELL, fillOrderCallback);
                orderBookBid = new OrderBook(marketData.id, JQuant.TransactionType.BUY, fillOrderCallback);
            }

            /// <summary>
            /// keep all ask orders here
            /// </summary>
            public OrderBook orderBookAsk;

            /// <summary>
            /// keep all bid orders here
            /// </summary>
            public OrderBook orderBookBid;
			
			/// <summary>
			/// I keep reference to the latest update for debug
			/// </summary>
			public MarketData marketData;
        }


        /// <summary>
        /// OrderQueue and OrderBook will call this method to let know upper layers that the
        /// system order (order placed by the application) got fill
        /// </summary>
        protected delegate void FillOrderBook(OrderBook orderBook, LimitOrder order, int quantity);
        protected delegate void FillOrderQueue(OrderQueue orderQueue, LimitOrder order, int quantity);

        protected class OrderQueue : JQuant.IResourceStatistics
        {

            /// <summary>
            /// i keep price and transaction type only for debug purposes
            /// </summary>
            public OrderQueue(int securityId, int price, JQuant.TransactionType transaction, FillOrderQueue fillOrder)
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
            /// - list's tail is occupied by non-sysem order (placed by algorithm)
            /// </summary>
            public void AddOrder(int quantity)
            {
                if (quantity < 0)
                {
                    RemoveOrder(-quantity); //System order cancellation?
                    return;
                }
                // i am adding entries to the list and I have no idea how many threads
                // will attempt the trick concurrently
                lock (orders)
                {
                    countAddOrderTotal++;

                    if ((orders.Count == 0) ||  // list is empty OR
                       orders.Last.Value.SystemOrder()) // last order is a system order
                    {
                        if (orders.Count == 0) countAddOrderToEmpty++;
                        else countAddOrderAfter++;

                        // create a new order, add to the end of list
                        LimitOrder lo = new LimitOrder(this.price, quantity, this.transaction);
                        orders.AddLast(lo);
                    }
                    else // I keep the order queue as short as possible
                    // because the exact structure of the queue is unimportant
                    // - keep only blocks of system orders, around non-system ones
                    {    // increment quantity in the last (tail) internal order
                        countAddOrderUpdate++;
						LimitOrder lastOrder = orders.Last.Value;
                        lastOrder.UpdateQuantity(quantity + lastOrder.Quantity);
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
                        // System.Console.WriteLine(ShortDescription() + " Can't remove " + quantity + " from " + sizeTotal + " total");
                        quantity = sizeTotal;
                    }

                    // loop until quantity is removed or the list is empty
                    // i handle two cases here - (1) remove of internal and (2) remove of system orders
                    // (1) - this is order placed by the system. Because it's is a simulation, i have to "fix" the order queue:
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

                            // if not partial fill - remove the order from the queue
                            if (fillSize >= lo.Quantity) //i got fill
                            {
                                orders.RemoveFirst();
                                // notify upper layer (orderbook) that the order got fill
                                fillOrder(this, lo, fillSize);
								sizeTotal -= lo.Quantity;
                            }
                            else // this is partial fill - just update the quantity
                            {
                                fillSize = lo.Quantity;
                                // lo.UpdateQuantity(lo.Quantity - fillSize);
                                // i have to skip the node in the list, but at this point i print error and remove the entry 
                                System.Console.WriteLine(ShortDescription() + " Don't know to hanle partial fills order " + lo.id);
                                orders.RemoveFirst();
                                // notify upper layer that the order got fill
                                fillOrder(this, lo, fillSize);
								sizeTotal -= fillSize;
                            }
                        }
                        else  // internal order
                        {
                            if (lo.Quantity <= quantity)  // remove the whole node
                            {
                                quantity -= lo.Quantity;
								sizeTotal -= lo.Quantity;
                                orders.RemoveFirst();
                            }
                            else  // just update the Quantity in the first node
                            {
                                lo = new LimitOrder(this.price, lo.Quantity - quantity, this.transaction);
                                orders.First.Value = lo;
								sizeTotal -= quantity;
                                quantity = 0;
                            }
                            sizeInternal -= quantity;
                        }
                    }
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
                        System.Console.WriteLine(ShortDescription() + "Can't remove order " + order.id + ". Not found in the list");
                    }
                }
            }


            protected string ShortDescription()
            {
                string sd = "Order queue " + this.securityId + " " + this.transaction.ToString() + " " + this.price;
                return sd;
            }

            protected System.Collections.Generic.LinkedList<LimitOrder> orders;

            public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(12);
                values = new System.Collections.ArrayList(12);

                /*0*/
                names.Add("securityId");
                values.Add(securityId);
                /*1*/
                names.Add("price");
                values.Add(price);
                /*2*/
                names.Add("Sell");
                values.Add(transaction == JQuant.TransactionType.SELL);
                /*3*/
                names.Add("ListSize");
                values.Add(orders.Count);
                /*4*/
                names.Add("SizeTotal");
                values.Add(sizeTotal);
                /*5*/
                names.Add("sizeInternalTotal");
                values.Add(sizeInternal);
                /*6*/
                names.Add("sizeSystemTotal");
                values.Add(sizeSystem);
                /*7*/
                names.Add("countAddOrderToEmpty");
                values.Add(countAddOrderToEmpty);
                /*8*/
                names.Add("countAddOrderAfter");
                values.Add(countAddOrderAfter);
                /*9*/
                names.Add("countAddOrderUpdate");
                values.Add(countAddOrderUpdate);
                /*10*/
                names.Add("countAddOrderTotal");
                values.Add(countAddOrderTotal);
                /*11*/
                names.Add("countAddSystemOrder");
                values.Add(countAddSystemOrder);
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

            public int GetPrice()
            {
                return price;
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
            protected FillOrderQueue fillOrder;
        }  // class OrderQueue

        /// <summary>
        /// TASE publishes order book of depth three - three slots for asks and three slots for bids.
        /// I arrange book order as a list of slots ordered by price. 
        /// The size of the list is not neccessary three.
        /// </summary>
        protected class OrderBook : JQuant.IResourceStatistics
        {
            /// <summary>
            /// Create OrderBook - i keep two books for every security. 
            /// One book for asks and another book for bids.
            /// I keep security id for debug.
            /// Argument "fillOrder" is a method to call when a system order got fill.
            /// </summary>
            public OrderBook(int securityId, JQuant.TransactionType transaction, FillOrderBook fillOrder)
            {
                this.securityId = securityId;
                this.transaction = transaction;
                this.fillOrder = fillOrder;

                System.Collections.Generic.IComparer<int> iComparer;

                // i keep order queue in the list slots ordered
                // best bid (ask) should be at the head of the list index 0
                if (transaction == JQuant.TransactionType.SELL)
                    iComparer = new AskComparator();
                else
                    iComparer = new BidComparator();

                slots = new System.Collections.Generic.SortedList<int, OrderQueue>(10, iComparer);
				
				// i am creating a dummy data - mostly zeros. Any first real data will cause 
				// the queue to reinitialize
				marketData = new MarketData(3);
		        marketData.id = securityId;
		        marketData.lastTrade = 0;
		        marketData.lastTradeSize = 0;
		        marketData.dayVolume = 0;
            }

            /// <summary>
            /// If there is no system orders updating of the book is trivial - i just store the market data
            /// The things get interesting when a first order is placed. I allocate the order queues and 
            /// update will update the queues.
            /// </summary>
            public void Update(MarketData md)
            {
#if MARKETSIM_FAST
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
#else
                // i have previous record (marketData) and current record (md)
                // let's figure out what happened
                Update_trade(md);  // probably a trade ?
                Update_queue(md);  // probably some orders are pulled out or added ?

                // finally replace the data
                this.marketData = md;
#endif
            }

            /// <summary>
            /// Add a system order to the order book.
            /// </summary>
            public void PlaceOrder(LimitOrder order)
            {
                OrderQueue orderQueue = null;
				int price = order.Price;

                // i am looking for an order queue with specific price
                lock (slots)
                {
                    if (slots.ContainsKey(price)) orderQueue = (OrderQueue)(slots[price]);
                    if (orderQueue == null)  // there is no such order queue  - create a new one 
                    {                        // and add it to the order book
                        orderQueue = new OrderQueue(this.securityId, price, this.transaction, FillOrderCallback);
                        slots.Add(price, orderQueue);
						if (enableTrace)
						{
							System.Console.WriteLine("OrderBook placeOrder add slot price="+order.Price);
						}
                    }
					
                    // i have a system order in the book. update counter
                    sizeSystem += order.Quantity;
                }

                // add order to the queue
                orderQueue.AddOrder(order);
            }

            /// <summary>
            /// The method is called if in the Update() i discover that there was a trade
            /// I have to figure out size of the trade and remove the traded securities from the queue
            /// </summary>
            protected void Update_trade(MarketData md)
            {
                int tradeSize = (md.dayVolume - marketData.dayVolume);
				int totalToRemove = tradeSize;
                if (tradeSize < 0)
                {
                    System.Console.WriteLine(ShortDescription() + " negative change in day volume from " + md.dayVolume + " to " +
                                            marketData.dayVolume + " are not consistent");
                }
                else if (tradeSize > 0) // there was a trade ? let's check the price 
                {                       // of the deal and size of the deal
                    if (tradeSize != md.lastTradeSize)  // sanity check - any misssing records out there ?
                    {
                        System.Console.WriteLine(ShortDescription() + " last trade size " + md.lastTradeSize +
                                                " and change in day volume from " + md.dayVolume + " to " + marketData.dayVolume + " are not consistent");
                    }
					
					lock (slots)
					{
	                    // i remove the traded securities from the queue(s) starting 
						// from the best (head of the ordered list "slots")
						foreach (OrderQueue orderQueue in slots.Values)
						{
							int price = orderQueue.GetPrice();
							
	                        int size0 = orderQueue.GetSize();  // get queue size
	                        orderQueue.RemoveOrder(tradeSize); // remove the trade
	                        int size1 = orderQueue.GetSize();  // get queue size
	
	                        if (size1 == 0)  // if the queue empty - remove the queue
	                        {
								lock (slots)
								{
									if (slots.ContainsKey(price))
									{
			                            slots.Remove(price);
									}
								}
								if (enableTrace)
								{
									System.Console.WriteLine("OrderBook remove head slot price="+orderQueue.GetPrice());
									System.Console.WriteLine("Cur="+marketData.ToString());
									System.Console.WriteLine("New="+md.ToString());
								}
	                        }
	                        int removed = size0 - size1;
	                        tradeSize -= removed;
	                        if (tradeSize == 0)  // i removed the trade from the order queue ?
	                        {
	                            break;
	                        }
						} // foreach
					}

                    if (tradeSize > 0)
                    {
						// if i reached here i have accounting problem. The trade i see in the log is larger than 
						// total of all positions in the order queue. such large trade could not take place unless
						// i have wrong total position
						System.Console.WriteLine(ShortDescription() + " failed to remove the trade remains " + tradeSize+" from "+totalToRemove);
						if (enableTrace)
						{
							System.Console.WriteLine("NewData=" + md.ToString());
							System.Console.WriteLine("CurData=" + marketData.ToString());
						}
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

                if (this.transaction == JQuant.TransactionType.SELL) // i am in an ask book
                {
                    mdBookOrders = md.ask;
                    marketDataBookOrders = marketData.ask;
                }
                else  // i am in a bid book 
                {
                    mdBookOrders = md.bid;
                    marketDataBookOrders = marketData.bid;
                }

                int size = mdBookOrders.Length;
				System.Collections.ArrayList touchedQueues = new System.Collections.ArrayList(5);
                for (int i = 0; i < size; i++)
                {
                    int mdPrice = mdBookOrders[i].price;
					int mdSize = mdBookOrders[i].size;
					// find slot with this price
					OrderQueue orderQueue = null;
                    lock (slots)
                    {
						if (slots.ContainsKey(mdPrice)) orderQueue = (OrderQueue)(slots[mdPrice]);
					}
					if (orderQueue != null)  // i have two cases - there is a slot for this price (typical) and ...
					{
						int size_cur = orderQueue.GetSize();
						
						// i just update size of the queue. i take optimistic approach 
						// and add  the orders to the tail of the queue
						// and remove from the head
						// Method AddOrder() handles negative numbers too
						orderQueue.AddOrder(mdSize-size_cur);
						
						// add to the list of touched queues
						touchedQueues.Add(orderQueue);
					}
					else  // there is no such slot - add a new slot
					{
                        orderQueue = new OrderQueue(this.securityId, mdPrice, this.transaction, FillOrderCallback);
						orderQueue.AddOrder(mdSize);
                        slots.Add(mdPrice, orderQueue);  // add newly created queue to the list of queues sorted by price
						if (enableTrace)
						{
							System.Console.WriteLine("OrderBook add slot price="+mdPrice);
							System.Console.WriteLine("Cur="+marketData.ToString());
							System.Console.WriteLine("New="+md.ToString());
						}
						// add to the list of touched queues
						touchedQueues.Add(orderQueue);
					}
					
					// remove all queues which are not touched and do not contain 
					// system orders
					lock (slots)
					{
						foreach (OrderQueue oq in slots.Values)
						{
							int systemOrders = oq.GetSize() - oq.GetSizeInernal();
							int price = oq.GetPrice();
							if (!touchedQueues.Contains(oq) && (systemOrders >= 0))
							{
								slots.Remove(price);
								if (enableTrace)
								{
									System.Console.WriteLine("OrderBook remove slot price="+price);
									System.Console.WriteLine("Cur="+marketData.ToString());
									System.Console.WriteLine("New="+md.ToString());
								}
							}
							else if (systemOrders > 0)
							{
								System.Console.WriteLine(ShortDescription()+" OrderBook was not removed slot price="+price);
								if (enableTrace)
								{
									System.Console.WriteLine("OrderBook remove slot price="+price);
									System.Console.WriteLine("Cur="+marketData.ToString());
									System.Console.WriteLine("New="+md.ToString());
								}
							}
						}
					}
				}
            }

            /// <summary>
            /// This method is called from the order queue
            /// I forward the call to application. I remove the queue if empty
            /// </summary>
            protected void FillOrderCallback(OrderQueue queue, LimitOrder order, int fillSize)
            {
                this.fillOrder(this, order, fillSize);

                bool queueEmpty = (queue.GetSize() == 0);
                lock (slots)
                {
                    sizeSystem -= order.Quantity;
                    if (queueEmpty) 
					{
						int price = queue.GetPrice();
						slots.Remove(price);
						if (enableTrace)
						{
							System.Console.WriteLine("OrderBook remove head slot price="+price);
						}
					}
                }
            }
			
			public void EnableTrace(bool enable)
			{
				this.enableTrace = enable;
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
                    System.Console.WriteLine(ShortDescription() + " Check fill for order " + order.id + " " + order.Transaction);
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
                string sd = "Order book " + this.securityId + " " + this.transaction.ToString() + " ";
                return sd;
            }

            public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(4);
                values = new System.Collections.ArrayList(4);
                /* 0 */
                names.Add("SecurityId");
                values.Add(securityId);
                /* 1 */
                names.Add("Sell");
                values.Add(transaction == JQuant.TransactionType.SELL);
                /* 2 */
                names.Add("ListSize");
                values.Add(slots.Count);
                /* 3 */
                // name SizeSystem is used in the debug 
                names.Add("SizeSystem");
                values.Add(sizeSystem);
            }

            /// <summary>
            /// Used to help sort and compare order book of bids.
            /// </summary>
            protected class BidComparator : System.Collections.Generic.IComparer<int>
            {
                public int Compare(int x, int y)
                {
                    // higher bid is closer to the head of the sorted list
                    return (y - x);
                }
            }

            /// <summary>
            /// Used to help sort and compare order book of asks.
            /// </summary>
            protected class AskComparator : System.Collections.Generic.IComparer<int>
            {
                public int Compare(int x, int y)
                {
                    // higher ask is closer to the tail of the sorted list
                    return (x - y);
                }
            }

            public OrderQueue[] GetQueues()
            {
                System.Collections.Generic.IList<OrderQueue> collection = slots.Values;
                OrderQueue[] oqs = new OrderQueue[collection.Count];
                collection.CopyTo(oqs, 0);

                return oqs;
            }

            public MarketData GetMarketData()
            {
                return this.marketData;
            }

            /// <summary>
            /// Slots are ordered by price
            /// Ask order queues are ordered from lower price to higher price and 
            /// book of bids aranged from higher price to lower price.
            /// </summary>
            protected System.Collections.Generic.SortedList<int, OrderQueue> slots;

            /// <summary>
            /// total size of all orders placed by the system.
            /// </summary>
            protected int sizeSystem;
            protected MarketData marketData;
            protected int securityId;
            protected JQuant.TransactionType transaction;
            protected FillOrderBook fillOrder;
			protected bool enableTrace;
        } // class OrderBook

        /// <summary>
        /// Use child class (wrapper) like MarketSimulationMaof to create instance of the MarketSimulation
        /// </summary>
        protected Core()
        {
            // create hash table where all securities are stored
            securities = new System.Collections.Hashtable(200);
			enableTrace = new System.Collections.Hashtable(200);
            filledOrdersThread = new FilledOrdersThread();
            filledOrdersThread.Start();
        }
		
		
		/// <summary>
        /// Implement IDisposable.
		/// Call this method to clean up the MarketSimulation
		/// The method stops threads 
		/// </summary>
		public void Dispose()
		{
            filledOrdersThread.Stop();  //Stop() causes MailboxThread.Dispose()
			filledOrdersThread = null;
			securities = null; 
		}

        public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
        {
            names = new System.Collections.ArrayList(8);
            values = new System.Collections.ArrayList(8);
            /*1*/
            names.Add("Events");    //total number of lines (events) read from the historical log
            values.Add(eventsCount);
            /*2*/
            names.Add("OrdersPlaced");  //number of system order placed
            values.Add(ordersPlacedCount);
            /*3*/
            names.Add("OrdersFilled");
            values.Add(ordersFilledCount);
            /*4*/
            names.Add("OrdersCanceled");
            values.Add(ordersCanceledCount);
            /*5*/
            names.Add("Securities");    //number of tradable securities (in the hashtable)
            values.Add(securities.Count);
        }

        /// <summary>
        /// The method is being called by Event Generator to notify the market simulation, that
        /// there is a new event went through, for example change in the order book.
        /// Argument "data" can be reused by the calling thread. If the data is processed 
        /// asynchronously, Notify() should clone the object.
        /// In the current setup MarketSimulationMaof.Notify calls the method
        /// </summary>
        public void Notify(int count, MarketData dataIn)
        {
            // clone the data first - cloning is expensive, but i have no choice right now
			MarketData data = (MarketData)dataIn.Clone();
			
            // GetKey() will return security id
            object key = GetKey(data);
            object fsm;

            lock (securities)
            {
                // hopefully Item() will return null if there is no key in the hashtable
                fsm = securities[key];

                // Do I see this security very first time? Then add new entry to the hashtable.
                // This is not likely outcome (occurs several tens of times at the start of the trading sesssion). 
                // Performance is not an issue at this point
                if (fsm == null)
                {
                    fsm = new FSM(data, FillOrderCallback);
                    securities[key] = fsm;

                    // get the security from the hash table in all cases
                    // this line can be removed
                    fsm = securities[key];
					
					// enable trace in the order books if required
					if (enableTrace[((FSM)fsm).marketData.id] != null)
					{
						((FSM)fsm).orderBookAsk.EnableTrace(true);
						((FSM)fsm).orderBookBid.EnableTrace(true);
					}
                }
            }

            // i am going to call update in case if an order just being placed
            // by concurrently running thread
            UpdateSecurity((FSM)fsm, data);
        }

        /// <summary>
        /// This method is called from the order book
        /// I forward the call to the application
        /// </summary>
        protected void FillOrderCallback(OrderBook book, LimitOrder order, int fillSize)
        {
            // add order to the list of orders which got fill
            filledOrdersThread.Send(order);
            ordersFilledCount++;
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
        /// Let the order books handle the update, in case there is a log or system event. 
        /// </summary>
        protected void UpdateSecurity(FSM fsm, MarketData marketData)
        {
            // bump event counter
            eventsCount++;
			
			fsm.marketData = marketData;
			
            fsm.orderBookAsk.Update(marketData);
            fsm.orderBookBid.Update(marketData);
        }

        /// <summary>
        /// Place a system order. First checks for the possibility of immediate fill,
        /// if not possible - places it to the limit order book.
        /// Currently only limit ordres are supported (suitable for maof options).
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
				object o;
				
				lock (securities)
				{
	                o = securities[security];
				}

                if (o == null)
                {
                    errorCode = ReturnCode.UnknownSecurity;
                    break;
                }

                FSM fsm = (FSM)o;
                OrderBook orderBook;
                if (transaction == JQuant.TransactionType.SELL) orderBook = fsm.orderBookAsk; // sell order - book of asks
                else orderBook = fsm.orderBookBid;  // this is buy order - books of bids

                LimitOrder lo = new LimitOrder(security, price, quantity, transaction, callback);

                // maybe i can get fill immediately?
                bool haveFill = orderBook.CheckImmediateFill(lo);
                if (haveFill)
                {
                    filledOrdersThread.Send(lo);
                    ordersFilledCount++;
                }
                else  // add the order to the order book
                {
                    orderBook.PlaceOrder(lo);
                    ordersPlacedCount++;
                }

                res = true;
            }
            while (false);

            return res;
        }

        public OrderPair[] GetOrderQueue(int securityId, JQuant.TransactionType transType)
        {
            OrderPair[] op = null;
            MarketData md;

			object o;
			lock (securities)
			{
	            o = securities[securityId];
			}

            if (o != null)
            {
	            FSM fsm = (FSM)o;
	
	            if (transType == JQuant.TransactionType.SELL)
	            {
	                md = fsm.orderBookAsk.GetMarketData();
	                if (md!= null)
	                    op = md.ask;
	             }
	            else
	            {
	                md = fsm.orderBookBid.GetMarketData();
	                if (md != null)
	                    op = md.bid;
	            }
            }

                
            return op;
        }
        
        /// <summary>
        /// Small thread which calls application's callbacks
        /// every time an order filled MarketSimulation.Core sends a message to the thread. Thread
        /// wakes up and calls application callback - informs the application that the order got fill
        /// The idea is to execute the application hook from a separate (different priority) context.
        /// </summary>
        protected class FilledOrdersThread : JQuant.MailboxThread<LimitOrder>
        {
            public FilledOrdersThread()
                : base("msFldOrdr", 100)
            {
            }

            protected override void HandleMessage(LimitOrder lo)
            {
                lo.callback(lo.id, ReturnCode.Fill, lo.Price, lo.Quantity);
            }

        }

        /// <summary>
        /// Called from CLI to display counters and debug info.
        /// Returns the current state of the limit order book.
        /// </summary>
        public JQuant.IResourceStatistics GetOrderBook(int securityId, JQuant.TransactionType transaction)
        {
            JQuant.IResourceStatistics ob = null;

            do
            {
				object o;
				
				lock (securities)
				{
                   // hopefully Item() will return null if there is no key in the hashtable
	                o = securities[securityId];
				}

                if (o == null)
                {
                    break;
                }

                FSM fsm = (FSM)o;
                if (transaction == JQuant.TransactionType.SELL)
                    ob = fsm.orderBookAsk;
                else
                    ob = fsm.orderBookBid;
            }
            while (false);
            
            return (ob);
        }

        /// <summary>
        /// Called from CLI to display counters and debug info
        /// and returns order queues statistics.
        /// There is a different method which returns current situation in the bid/ask queues
        /// and a another method which returns the most recent log entry.
        /// </summary>
        public JQuant.IResourceStatistics[] GetOrderQueues(int securityId, JQuant.TransactionType transaction)
        {
            JQuant.IResourceStatistics[] oqs = null;

            do
            {
				// GetOrderBook will take care of exclusive locks
                OrderBook ob = (OrderBook)GetOrderBook(securityId, transaction);

                if (ob == null)
                {
					System.Console.WriteLine("Order book for security "+securityId+" not found");
                    break;
                }

                oqs = (JQuant.IResourceStatistics[])(ob.GetQueues());
            }
            while (false);

            return (oqs);
        }

		public bool GetEnableTrace(int securityId)
		{
			bool res = false;
            object fsm = securities[securityId];
			
			if (fsm != null)
			{
				res = (enableTrace[((FSM)fsm).marketData.id] != null);
			}
					
			return res;
		}
		
		public void EnableTrace(int securityId, bool enable)
		{
            do
            {
				// GetOrderBook will take care of exclusive locks
                OrderBook obBuy = (OrderBook)GetOrderBook(securityId, JQuant.TransactionType.BUY);
                OrderBook obSell = (OrderBook)GetOrderBook(securityId, JQuant.TransactionType.SELL);

                if (obBuy != null)
                {
	                obBuy.EnableTrace(enable);
                }
                if (obSell != null)
                {
	                obSell.EnableTrace(enable);
                }
				
				enableTrace.Remove(securityId);
				// add flag to the hashtable. When i create a book i will see the trace flag
				if (enable)
				{
					enableTrace.Add(securityId,  true);
				}
					
            }
            while (false);
		}

        /// <summary>
        /// Returns list of securities.
        /// </summary>
        public int[] GetSecurities()
        {
			int[] ids;
			
			// i do not know where the hashtable is getting copied
			// just in case i lock everything
			lock (securities)
			{
	            System.Collections.ICollection keys = securities.Keys;
	
	            int size = keys.Count;
	
	            ids = new int[size];
	
	            keys.CopyTo(ids, 0);
			}

            return ids;
        }

        public MarketData GetSecurity(int securityId)
        {
			object o;
			
			lock (securities)
			{
				o = securities[securityId];
			}
			
			FSM fsm = default(FSM);			
			MarketData md = default(MarketData);
			
			if (o != null)
			{
				fsm = (FSM)o;
				md = fsm.marketData;
			}
			

            return md;
        }
		
        /// <summary>
        /// Collection of all traded symbols (different BNO_Num for TASE). 
        /// I keep objects of type FSM in the hashtable.
        /// </summary>
        protected System.Collections.Hashtable securities;
        protected FilledOrdersThread filledOrdersThread;
        /// <summary>
        /// total number of lines (events) read from the historical log
        /// </summary>
        protected int eventsCount;
        /// <summary>
        /// number of system order placed
        /// </summary>
        protected int ordersPlacedCount;
        protected int ordersFilledCount;
        protected int ordersCanceledCount;
		
		/// <summary>
		/// Keep debug trace enable flags  
		/// </summary>
		protected System.Collections.Hashtable enableTrace;
    }
}
