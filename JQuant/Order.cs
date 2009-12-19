
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
    /// <summary>
    /// Either BUY or SELL
    /// </summary>
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
        /// Price is integer even (presented by cents, for example)
        /// </summary>
        public int Price
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// This one is specific to the options trading.
    /// this is a dataholder used for effective communication between 
    /// the order producer (algorithm) and the order processor (FSM)
    /// all option's orders types are variations of 'LMT', therefore the parameters 
    /// needed to initialize and process such an order are always the same.
    /// </summary>
    public struct LimitOrderParameters
    {
        public Option Opt;                  //what to trade?
        public TransactionType TransType;   //buy or sell?
        public int Quantity;                //how many?
        public double Price;                //how much per unit?
        public OrderType OType;             //which kind of order? (LMT/FOK/IOC)
    }

    #region Interfaces

    /// <summary>
    /// Any order processor (OrderFSM) will implement this interface
    /// </summary>
    public interface IOrderProcessor
    {
        bool Create(LimitOrderParameters OrdParams, out IMaofOrder order);

        bool Submit(IMaofOrder order);

        bool Cancel(IMaofOrder order);

        // add notification related API here
        // only the most basic
    }

    /// <summary>
    /// this interface is what external world sees
    /// Order processor will see all the fields
    /// </summary>
    public interface IMaofOrder
    {
        DateTime Created
        {
            get;        //note there is no public set here - it's determined internally
        }

        /// <summary>
        /// Buy or Sell
        /// </summary>
        TransactionType TransType
        {
            get;
            set;
        }

        /// <summary>
        /// LMT, MKT, ... etc.
        /// </summary>
        OrderType OrdrType
        {
            get;
            set;
        }
    }

    #endregion


}//namespace




