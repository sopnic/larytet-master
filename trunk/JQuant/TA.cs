
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
        
        double open;
        double close;
        double min;
        double max;
        double volume;
    }

    /// <summary>
    /// stores price+volume series in list
    /// I need quick iterration through the list and quick adding to the list
    /// </summary>
    public class PriceVolumeSeries
    {
        public PriceVolumeSeries(int size)
        {
            series = new System.Collections.Generic.List<Candle>(size);
        }

        public void Add(double open, double close, double min, double max, double volume)
        {
            series.Add(new Candle(open, close, min, max, volume));
        }
        
        System.Collections.Generic.List<Candle> series;
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
        /// default triangle - the simplest case. i ignore variation, only close price is considered
        /// volume changes through the series are ignored
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
        /// looks for all triangles, including nested (fractal) trinagles
        /// </summary>
        public void Process()
        {
        }


        public  PriceVolumeSeries series
        {
            get;
            protected set;
        }
        
        public System.Collections.Generic.List<Shape> shapes;
    }
}
