
using System;


/// <summary>
/// Recognition of basic TA patterns
/// </summary>
namespace TA
{
    public class Point
    {
        public Point(double price, double volume)
        {
            this.price = price;
            this.volume = volume;
        }
        
        double price;
        double volume;
    }
    
    public class Candle
    {
        public Candle(double open, double close, double min, double max, double volume)
        {
            this.open = open;
            this.close = close;
            this.min = min;
            this.max = max;
            this.volume = volume;
        }
        
        public double open
        {
            get;
            protected set;
        }

        public double close
        {
            get;
            protected set;
        }

        public double min
        {
            get;
            protected set;
        }

        public double max
        {
            get;
            protected set;
        }

        public double volume
        {
            get;
            protected set;
        }

    }

    /// <summary>
    /// stores price+volume series in list
    /// I need quick iterration through the list and quick adding to the list
    /// </summary>
    public class PriceVolumeSeries
    {
        public PriceVolumeSeries(int size)
        {
            Data = new System.Collections.ArrayList(size);
            paramsCalculated = false;
        }

        public void Add(double open, double close, double min, double max, double volume)
        {
            paramsCalculated = false;
            Data.Add(new Candle(open, close, min, max, volume));
        }
        
        public System.Collections.ArrayList Data
        {
            get;
            protected set;
        }

        public static void AverageStdDeviation
            (PriceVolumeSeries series, out double average, out double max, out double min, out double stdDeviation)
        {
            AverageStdDeviation(series, 0, series.Data.Count, out average, out max, out min, out stdDeviation);
        }
        
        public static void AverageStdDeviation
            (PriceVolumeSeries series, int start, int count, out double average, out double max, out double min, out double stdDeviation)
        {
            Candle candle = (Candle)series.Data[start];
            double close = candle.close;
            average = close;
            max = close;
            min = max;

            stdDeviation = close*close;

            int end = start+count-1;
            for (int i=start+1;i <= end;i++)
            {
                candle= (Candle)series.Data[i]; 
                close = candle.close;
                
                average += close;
                max = Math.Max(max, close);
                min = Math.Min(min, close);

                stdDeviation += close * close;
            }

            stdDeviation = Math.Sqrt(stdDeviation)/count;
            average = average/count;
        }
        
        public void CalculateParams()
        {
            if (!paramsCalculated)
            {
                double average;double max;double min;double stdDeviation;
                
                AverageStdDeviation(this, out average, out max, out min, out stdDeviation);
                
                Average = average;
                Min = min;
                Max = max;
                StdDeviation = stdDeviation;
    
                paramsCalculated = true;
            }
        }

        public double Average
        {
            get;
            protected set;
        }
        
        public double Max
        {
            get;
            protected set;
        }
        
        public double Min
        {
            get;
            protected set;
        }
        
        public double StdDeviation
        {
            get;
            protected set;
        }

        bool paramsCalculated;
    }

    /// <summary>
    /// describes triangle as a start and end index of the given series
    /// </summary>
    public class Shape
    {

        public Shape(int start, int end, PriceVolumeSeries series)
        {
            this.start = start;
            this.end = end;

            this.series = series;
        }
        
        public int start
        {
            get;
            protected set;
        }
        
        public int end
        {
            get;
            protected set;
        }

        public PriceVolumeSeries series
        {
            get;
            protected set;
        }

    }

    /// <summary>
    /// ascending triangle is defined as 3 lows and two highs where susequent lows are higher than previous
    /// and tops are the same. 
    /// </summary>
    public class AscendingTriangle
    {

        /// <summary>
        /// find ascending triangel in the series
        /// </summary>
        /// <param name="series">
        /// A <see cref="PriceVolumeSeries"/>
        /// The data to analyse
        /// </param>
        public AscendingTriangle(PriceVolumeSeries series)
        {
            this.series = series;
            this.shapes = new System.Collections.Generic.List<TA.Shape>(1);

        }

        /// <summary>
        /// process the data
        /// looks for largest triangles, ignores nesting (fractal shapes)
        /// </summary>
        public void Process()
        {
            // make sure that basic staff is calculated
            series.CalculateParams();
        }




        /// <summary>
        /// divide the series in two parts, calculate maximum and minimum in two parts
        /// maximums should be "close" enough and subsequent low should be higher than the previous
        /// check if i have at least three points luying on the same ascending line and at least two
        /// points around the maximum
        /// </summary>
        public static bool isTriangle(int start, int end, PriceVolumeSeries series)
        {
            bool result = false;
            
            do
            {
                // no enough point for triangle
                if ((end - start) < 6) break;

                
                int halfPoint = (end - start)/2;
                
            }
            while (false);

            
            return result;
        }


        public  PriceVolumeSeries series
        {
            get;
            protected set;
        }
        
        public System.Collections.Generic.List<Shape> shapes;
    }
}
