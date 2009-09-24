
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

namespace JQuant
{
    public enum TransactionType
    {
        [Description("Sell")]
        SELL,
        
        [Description("Buy")]
        BUY
    }

    /// <summary>
    /// The values match all the order types possible on TASE.
    /// </summary>
    public enum OrderType
    {
        [Description("LMO")]
        LMO,    //Limit Opening - for Rezef securities only

        [Description("MKT")]
        MKT,    //MKT - for Rezef securities only

        [Description("LMT")]
        LMT,    //Limit

        [Description("IOC")]
        IOC,    //Immediate or Cancel - for options only

        [Description("FOK")]
        FOK     //Fill or Kill - for options only

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
        public int Price
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
        int price;
    }

    public class MaofOrder:LimitOrderBase
    {

    }
    
}
