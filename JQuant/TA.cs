
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
        
        public double price
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
    
    public class Candle
    {
        public Candle(double open, double close, double min, double max, int volume)
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

        public int volume
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

        public void Add(double open, double close, double min, double max, int volume)
        {
            paramsCalculated = false;
            Data.Add(new Candle(open, close, min, max, volume));
        }
        
        public System.Collections.ArrayList Data
        {
            get;
            protected set;
        }

        public static void CalculateAverageStdDeviation
            (PriceVolumeSeries series, out double average, out double max, out double min, out double stdDeviation)
        {
            CalculateAverageStdDeviation(series, 0, series.Data.Count, out average, out max, out min, out stdDeviation);
        }
        
        public static void CalculateAverageStdDeviation
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

        public static void CalculateAverage
            (PriceVolumeSeries series, int start, int count, out double average, out double max, out int maxIdx, out double min, out int minIdx)
        {
            Candle candle = (Candle)series.Data[start];
            double close = candle.close;
            average = close;
            max = close;
            maxIdx = 0;
            min = max;
            minIdx = 0;

            int end = start+count-1;
            for (int i=start+1;i <= end;i++)
            {
                candle= (Candle)series.Data[i]; 
                close = candle.close;
                if (max < close)
                {
                    max = close;
                    maxIdx = i;
                }
                if (min > close)
                {
                    min = close;
                    minIdx = i;
                }
                average += close;
            }

            average = average/count;
        }
        
        public void CalculateParams()
        {
            if (!paramsCalculated)
            {
                double average;double max;double min;double stdDeviation;
                
                CalculateAverageStdDeviation(this, out average, out max, out min, out stdDeviation);
                
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
        public AscendingTriangle(PriceVolumeSeries series, double stdDeviations)
        {
            this.series = series;
            this.shapes = new System.Collections.Generic.List<TA.Shape>(1);

            StdDeviations = stdDeviations;
        }
        /// <summary>
        /// find ascending triangel in the series
        /// </summary>
        /// <param name="series">
        /// A <see cref="PriceVolumeSeries"/>
        /// The data to analyse
        /// </param>
        public AscendingTriangle(PriceVolumeSeries series)
            : this (series, 1.0)
        {
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
        public bool isTriangle(int start, int end, PriceVolumeSeries series)
        {
            bool result = false;
            
            do
            {
                // no enough points for triangle
                if ((end - start) < 6) break;
                
                int halfPoint = (end - start)/2;
                double average, min1, max1, min2, max2;
                int min1Idx, max1Idx, min2Idx, max2Idx;

                double stdDeviation = StdDeviations*series.StdDeviation;
                
                PriceVolumeSeries.CalculateAverage(series, start, halfPoint-start+1, out average, out min1, out min1Idx, out max1, out max1Idx);
                PriceVolumeSeries.CalculateAverage(series, halfPoint, end-halfPoint+1, out average, out min2, out min2Idx, out max2, out max2Idx);

                //   max2-stdDeviation < max1 < max2+stdDeviation
                if ( (max1 <= (max2-stdDeviation)) || (max1 >= (max2+stdDeviation)) )
                {
                    break;
                }

                if (min2 < min1+stdDeviation)
                {
                    break;
                }

                // i have two conditions - maxs are "close" and second low is higher than the first
                // do i have two highs and three lows ? two highs (two maxs) are in place. now lows
                double tangent = (min2 - min1)/(min2Idx - min1Idx);
                double target = min1-tangent*(min1Idx-start);
                bool thirdPoint = false;
                bool closeUnder = false;

                // look for 3rd close on line described by the tangent
                // on the way I make sure that all closes are above the ascending line
                for (int i=start;i <= end;i++)
                {
                    Candle candle= (Candle)series.Data[i]; 
                    double close = candle.close;

                    if ( (i != min1Idx) && (i != min2Idx) && (close < target-stdDeviation)  )
                    {
                        closeUnder = true;
                        break;
                    }

                    thirdPoint = thirdPoint | ( (i != min1Idx) && (i != min2Idx) && (close > target-stdDeviation) && (close < target+stdDeviation)  );
                    
                    target = target + tangent;
                }

                result = !closeUnder && thirdPoint;
                
            }
            while (false);

            
            return result;
        }


        public  PriceVolumeSeries series
        {
            get;
            protected set;
        }

        public double StdDeviations
        {
            get;
            protected set;
        }
        
        public System.Collections.Generic.List<Shape> shapes;
    }
}