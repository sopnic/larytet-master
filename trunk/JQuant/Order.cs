
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

    public enum OrderType
    {
        [Description("At the Openning")]
        ATO,

        [Description ("At the Closing")]
        AOC, 
        [Description("Market")]
        MKT,

        [Description("Limit")]
        LMT,

        [Description("Immediate or Cancel")]
        IOC,

        [Description("Fill or Kill")]
        FOK
    }
    
    public enum CurrencyType
    {
        [Description("USD")]
        USD,
        
        [Description("NIS")]
        NIS,
        
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
    
}
