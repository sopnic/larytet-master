
using System;
using System.Text;
using System.Net;
using System.IO;

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
            bool result = false;
            series = null;

            do
            {
                int size = (int)Math.Round((end - start).TotalDays);
                if (size <= 0)
                {
                    break;
                }

                // preallocate some memory
                series = new TA.PriceVolumeSeries(size);

                string url;
                result = buildURL(ticker, start, end, dataType, out url);
                if (!result)
                {
                    break;
                }

                
                Console.WriteLine("Get data from URL "+url);
    
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
    
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
    
                Stream readStream = httpResponse.GetResponseStream();
    
                result = fillDataArray(readStream, series);
            }
            while (false);
            
   
            return result;
            
        }

        public DataFeed.DataType GetDataType()
        {
            return DataFeed.DataType.Daily | 
                    DataFeed.DataType.Weekly |
                    DataFeed.DataType.Monthly |
                    DataFeed.DataType.Dividends;
        }

        bool fillDataArray(Stream readStream, TA.PriceVolumeSeries series)
        {
            bool result = false;
            byte[] buf = new byte[8192];
            StringBuilder sb  = new StringBuilder();


            // read all data from the stream
            while (true)
            {
                int count = readStream.Read(buf, 0, buf.Length);
                // nothing to read ?
                if (count == 0)
                {
                    break;
                }
                
                // translate from bytes to ASCII text
                string tempString = Encoding.ASCII.GetString(buf, 0, count);

                // continue building the string
                sb.Append(tempString);
            }

            string str = sb.ToString();
            
            // buffer sb contains lines separated by 0x0A (line feed)
            // skip first line
            int indexEnd;
            int indexStart = str.IndexOf((char)0x0A, 0);
            result = true;
            do
            {
                indexStart += 1;
                indexEnd = str.IndexOf((char)0x0A, indexStart);

                if (indexEnd <= 0)
                {
                    break;
                }
                
                result = ((indexEnd - indexStart) > 1);
                if (!result)
                {
                    break;
                }

                string data = str.Substring(indexStart, indexEnd-1);
                TA.Candle candle;
                result = strToCandle(data, out candle);
                if (!result)
                {
                    break;
                }
                
            }
            while (true);
            
            return result;
            
        }

        /// <summary>
        /// Example
        /// Date,Open,High,Low,Close,Volume,Adj Close
        /// 2009-09-25,74.04,74.39,73.37,73.80,3470700,73.80
        /// </summary>
        bool strToCandle(string str, out TA.Candle candle)
        {
            candle = null;
            int start = 0;
            int end = 0;
            bool result = true;

            // skip date
            end = str.IndexOf(',', start);
            if (end <= 1) // no date ?
            {
                return false;
            }
            start = end + 1;

            double open, close, max, min;
            int volume;
            string strVal;
            
            // open
            end = str.IndexOf(',', start);
            if ((end <= 1) || (end <= start)) // no open ?
            {
                return false;
            }
            strVal = str.Substring(start, end-1);
            result = Double.TryParse(strVal, out open);
            if (!result)
            {
                return false;
            }
            start = end + 1;

            // max
            end = str.IndexOf(',', start);
            if ((end <= 1) || (end <= start)) // no max ?
            {
                return false;
            }
            strVal = str.Substring(start, end-1);
            result = Double.TryParse(strVal, out max);
            if (!result)
            {
                return false;
            }
            start = end + 1;

            // min
            end = str.IndexOf(',', start);
            if ((end <= 1) || (end <= start)) // no min ?
            {
                return false;
            }
            strVal = str.Substring(start, end-1);
            result = Double.TryParse(strVal, out min);
            if (!result)
            {
                return false;
            }
            start = end + 1;

            
            // close
            end = str.IndexOf(',', start);
            if ((end <= 1) || (end <= start)) // no close ?
            {
                return false;
            }
            strVal = str.Substring(start, end-1);
            result = Double.TryParse(strVal, out close);
            if (!result)
            {
                return false;
            }
            start = end + 1;

            // volume
            end = str.IndexOf(',', start);
            if ((end <= 1) || (end <= start)) // no open ?
            {
                return false;
            }
            strVal = str.Substring(start, end-1);
            result = Int32.TryParse(strVal, out volume);
            if (!result)
            {
                return false;
            }
            start = end + 1;

            candle = new TA.Candle(open, close, min, max, volume);

            return true;
        }
        

        /// <summary>
        /// Example
        /// MMM Apr 5, 1970 - 27 Feb,2009, daily
        /// http://ichart.finance.yahoo.com/table.csv?s=MMM&a=03&b=5&c=1970&d=01&e=27&f=2009&g=d&ignore=.csv
        /// </summary>
        bool buildURL(string ticker, DateTime start, DateTime end, DataFeed.DataType dataType, out string url)
        {
            url = "";
            string dataTypeURL = DataTypeToURL(dataType);

            if (dataTypeURL == "")
            {
                return false;
            }
            

            url = "http://ichart.finance.yahoo.com/table.csv?s="+ticker+
                "&a="+start.Month+
                "&b="+start.Day+
                "&c="+start.Year+
                "&d="+end.Month+
                "&e="+end.Day+
                "&e="+end.Year+
                "&g="+dataTypeURL+
                "&ignore=.csv";

            return true;
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
