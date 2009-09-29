
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
        bool Create(TransactionType type, out IOrderBase order);

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
        System.DateTime Created
        {
            get;
            set;
        }

        TransactionType Type
        {
            get;
            set;
        }
    }

    public interface IOrderSell: IOrderBase
    {
        /// <value>
        /// Sell orders are characterized by Ask price. Ask prices 
        /// is number of money units of money 
        /// </value>
        int Ask
        {
            get;
            set;
        }
    }
    
    public interface IOrderBuy: IOrderBase
    {
        /// <value>
        /// Buy orders are characterized by Bid price. Bid price
        /// is number of units of money 
        /// </value>
        int Bid
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
        public int Quantity
        {
            get
            {
                return quantity;
            }
            protected set
            {
                quantity = value;
            }
        }

        public TransactionType TransType
        {
            get
            {
                return transType;
            }
            protected set
            {
                transType = value;
            }
        }

        int quantity;               //Number of units to trade
        TransactionType transType;  //Buy or Sell
    }

    /// <summary>
    /// This class inherits from the basic and serves itself as a base class for all limit
    /// orders - that is, for TASE, it's base for LMT, LMO, IOC and FOK order types
    /// The difference is that this class contains limit price, which is not present for MKT orders
    /// </summary>
    public abstract class LimitOrderBase : OrderBase
    {
        public double Price
        {
            get 
            {
                return price;
            }
            protected set
            {
                price = value;
            }
        }
        double price;
    }

    public class MaofOrder:LimitOrderBase
    {
        MaofOrderType maofOrderType; //used for sending the trading directive   to the API
        
        //reference IDs
        string Asmachta;
        string AsmachtaFMF;
        int OrderId;    //this one is only for taking care of internal errors

        //Special variables required by FMR to treat internal errors:
        OrdersErrorTypes ErrorType;
        int ErrNo;
        string VbMsg;
        string ReEnteredValue;

    }

    /// <summary>
    /// This one I keep here temporarily.
    /// Contains all the data common to all Order objects which doesn't change 
    /// during the trading session.
    /// </summary>
    public struct AccountProfile
    {
        public string Account;
        public string Branch;
        public int SessionId; //obtain it from the Connection object
        string UserName;
        string PassWord;
    }
    
}
