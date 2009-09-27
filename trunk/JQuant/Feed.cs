
using System;

namespace JQuant
{


    public class Equity
    {
        public Equity(string ticker)
        {
            ticker = ticker;
        }


        public string Ticker
        {
            get;
            protected set;
        }
    }
    
    public interface IDataFeed
    {        
        bool GetSeries(DateTime start, DateTime end, Equity equity, DataFeed.DataType dataType, out TA.PriceVolumeSeries series);
        DataFeed.DataType GetDataType();
    }

    public class DataFeed
    {
        [Flags]
        public enum DataType
        {
            Daily                       = 0x0001,
            Weekly                      = 0x0002,
            Monthly                     = 0x0004,
            Dividends                   = 0x0008,
            PriceToBook                 = 0x0010,
            PriceToEarnings             = 0x0020
        }
    }

    /// <summary>
    /// opens data feed, build object TA.PriceVolumeSeries
    /// example of Yahoo Feed
    /// 
    /// MMM Jan 1, 1970 - 27 Sep,2009, daily
    /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=00&b=2&c=1970&d=08&e=27&f=2009&g=d&ignore=.csv
    /// 
    /// MMM Mar 5, 1970 - 27 Feb,2009, weekly
    /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=02&b=5&c=1970&d=01&e=27&f=2009&g=w&ignore=.csv
    /// 
    /// MMM Apr 5, 1970 - 27 Feb,2009, weekly
    /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=03&b=5&c=1970&d=01&e=27&f=2009&g=w&ignore=.csv
    /// 
    /// MMM Apr 5, 1970 - 27 Feb,2009, daily
    /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=03&b=5&c=1970&d=01&e=27&f=2009&g=d&ignore=.csv
    /// </summary>
    public class FeedYahoo : DataFeed, IDataFeed
    {        
        public FeedYahoo()
        {
        }
        
        public bool GetSeries(DateTime start, DateTime end, Equity equity, DataFeed.DataType dataType, out TA.PriceVolumeSeries series)
        {
            string ticker = equity.Ticker;
            int size = (int)Math.Round((end - start).TotalDays);
            series = new TA.PriceVolumeSeries(size);

            string url = buildURL(ticker, start, end, dataType);
   
            return true;
            
        }

        public DataFeed.DataType GetDataType()
        {
            return DataFeed.DataType.Daily | 
                    DataFeed.DataType.Weekly |
                    DataFeed.DataType.Monthly |
                    DataFeed.DataType.Dividends;
        }

        /// <summary>
        /// Example
        /// MMM Apr 5, 1970 - 27 Feb,2009, daily
        /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=03&b=5&c=1970&d=01&e=27&f=2009&g=d&ignore=.csv
        /// </summary>
        string buildURL(string ticker, DateTime start, DateTime end, DataFeed.DataType dataType)
        {
            string dataTypeURL = DataTypeToURL(dataType);

            string result = "http://ichart.finance.yahoo.com/table.csv?s="+ticker+
                "&a="+start.Month+
                "&b="+start.Day+
                "&c="+start.Year+
                "&d="+end.Month+
                "&e="+end.Day+
                "&e="+end.Year+
                "&g="+dataTypeURL+
                "&ignore=.csv";

            return result;
        }

        string DataTypeToURL(DataFeed.DataType dataType)
        {
            switch (dataType)
            {
            case DataType.Daily:
                return "d";
            case DataType.Monthly:
                return "m";
            case DataType.Weekly:
                return "w";
            case DataType.Dividends:
                return "v";
            default:
                return "";
            }
        }
    }
    
}
