
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

#if USEFMRSIM
using TaskBarLibSim;
#else
using TaskBarLib;
#endif

namespace JQuant
{
    public enum TransactionType
    {
        [Description("SELL")]
        SELL,

        [Description("BUY")]
        BUY
    }

    /// <summary>
    /// The values match all the order types possible on TASE.
    /// Attribute [Flags] allow to use boolean combinations of the order types
    /// for example order can be of type (LMT | FOK | IOC)
    /// </summary>
    [Flags]
    public enum OrderType
    {
        [Description("LMO")]
        LMO = 0x0001,    // Limit Opening - for Rezef securities only

        [Description("MKT")]
        MKT = 0x0002,    // MKT - for Rezef securities only

        [Description("LMT")]
        LMT = 0x0004,    // Limit

        [Description("IOC")]
        IOC = 0x0008,    // Immediate or Cancel - for options only

        [Description("FOK")]
        FOK = 0x0010,    // Fill or Kill - for options only

        [Description("GTC")]
        GTC = 0x0020,    // Good till cancel - this one is not on TASE, there are continous orders (esp. Rezef)

        [Description("EOD")]
        EOD = 0x0040     // End of day (on close) - this one is not on TASE
    }

    public enum CurrencyType
    {
        [Description("USD")]
        USD,    //thinking NASDAQ?

        [Description("EUR")]
        EUR,    //who knows - maybe one day ...

        [Description("GBP")]
        GBP,    //LSE is very strong in algos :)

        [Description("ILS")]
        ILS,     //humble me :) - I'd prefer to have it in ISO 4217

        [Description("CNY")]
        CNY     //now guess what is this one... :D
    }


    /// <summary>
    /// Any order processor (OrderFSM) will implement this interface
    /// </summary>
    public interface IOrderProcessor
    {
        bool Create(TransactionType type, OrderType Otype, out IOrderBase order);

        bool Place(IOrderBase order);

        bool Cancel(IOrderBase order);

        // add notification related API here
        // only the most basic
    }


    /// <summary>
    /// this interface is what external world sees
    /// Order processor will see all the fields
    /// </summary>
    public interface IOrderBase
    {
        DateTime Created
        {
            get;
            set;
        }

        /// <summary>
        /// Buy or Sell
        /// </summary>
        TransactionType Type
        {
            get;
            set;
        }

        /// <summary>
        /// LMT, MKT, ... etc.
        /// </summary>
        OrderType OType
        {
            get;
            set;
        }
    }



    /// <summary>
    /// Base Order, from which all the other Order types inherit
    /// </summary>
    public abstract class OrderBase
    {
        /// <summary>
        /// Number of units to trade
        /// </summary>
        public int Quantity
        {
            get;
            protected set;
        }

        /// <summary>
        /// Buy or Sell
        /// </summary>
        public TransactionType TransType
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// This class inherits from the basic and serves itself as a base class for all limit
    /// orders - that is, for TASE, it's base for LMT, LMO, IOC and FOK order types
    /// The difference is that this class contains limit price, which is not present for MKT orders
    /// </summary>
    public abstract class LimitOrderBase : OrderBase
    {
        /// <summary>
        /// Limit price - transaction will only take place 
        /// if the counterparty's limit (currently on the order book)
        /// is better than mine
        /// (for sell - it's higher than my limit, 
        /// and for buy it's lower than mine)
        /// </summary>
        public double Price
        {
            get;
            protected set;
        }
    }
}//namespace
