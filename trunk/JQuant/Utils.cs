
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;


namespace JQuant
{

    /// <summary>
    /// C# enum does not allow to reload ToString method
    /// I need patch here 
    /// </summary>
    class EnumUtils
    {
        /// <summary>
        /// resolves enum - looks for the description
        /// </summary>
        public static string GetDescription(System.Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }


    /// <summary>
    /// format output - add blanks, remove trailing blanks, etc.
    /// </summary>
    public class OutputUtils
    {
        /// <summary>
        /// add trailing blanks to the integer if neccessary 
        /// </summary>
        static public string FormatField(long val, int fieldSize)
        {
            string s = FormatField(""+val, fieldSize);

            return s;
        }

        /// <summary>
        /// add trailing blanks to the string if neccessary 
        /// </summary>
        static public string FormatField(string val, int fieldSize)
        {
            StringBuilder s = new StringBuilder("" + val, fieldSize);

            int count = s.Length;
            for (int i = count; i < fieldSize; i++)
            {
                s.Insert(0, ' ');
            }

            return s.ToString();
        }

        /// <summary>
        /// add trailing blanks to the integer if neccessary 
        /// </summary>
        static public string FormatField(double val, int fieldSize)
        {
            string s = FormatField(""+val, fieldSize);

            return s;
        }
        
        static public string RemoveLeadingBlanks(string s)
        {
            int blank_idx = s.IndexOf(' ');
            while (blank_idx == 0)
            {
                s.Remove(0, 1);
                blank_idx = s.IndexOf(' ');
            }
            return s;
        }
    }

    /// <summary>
    /// converts a struct to a string with specified delimiters
    /// </summary>
    public class StructToString<StructType>
    {
        public StructToString(string delimiter)
        {
            this.delimiter = delimiter;
            Type t = typeof(StructType);
            fields = t.GetFields();
            InitLegend();
        }

        /// <value>
        /// keeps list of the fields (field names) separated by the Delimiter 
        /// </value>
        public string Legend
        {
            get;
            protected set;
        }

        /// <value>
        /// keeps a string with values separated by the delimiter
        /// </value>
        public string Values
        {
            get;
            protected set;
        }

        /// <value>
        /// delimiter used to separate the fields
        /// If set the object will regenerate strings
        /// </value>
        public string Delimiter
        {
            get
            {
                return this.delimiter;
            }

            set
            {
                // i can check if the delimiter changed indeed
                delimiter = value;
                Init(data);
            }
        }

        /// <value>
        /// This field is true if Init() was called
        /// </value>
        public bool IsInitialized
        {
            get;
            protected set;
        }

        protected void InitLegend()
        {
            StringBuilder sbLegend = new StringBuilder(150);

            foreach (FieldInfo field in fields)
            {
                string name = field.Name;
                sbLegend.Append(name);
                sbLegend.Append(delimiter);
            }
            Legend = sbLegend.ToString();
        }

        public void Init(StructType data)
        {
            this.data = data;
            StringBuilder sbData = new StringBuilder(50);

            foreach (FieldInfo field in fields)
            {
                object val = field.GetValue(data);
                sbData.Append(val.ToString());
                sbData.Append(delimiter);
            }
            Values = sbData.ToString();

            IsInitialized = true;
        }

        protected FieldInfo[] fields;
        protected StructType data;
        protected string delimiter;
    }

    public interface IRandomString
    {
        string Next();
    }

    public class RandomNumericalString : IRandomString
    {
        public RandomNumericalString(int min, int max)
        {
            rand = new Random();
            this.min = min;
            this.max = max;
        }

        public string Next()
        {
            int length = rand.Next(min, max);

            return length.ToString();
        }

        Random rand;
        int min;
        int max;
    }

    public class RandomString : IRandomString
    {
        /// <summary>
        /// create one object and call Next() to get a random string
        /// will generate random strings in the specified length range
        /// </summary>
        /// <param name="minLength">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="maxLength">
        /// A <see cref="System.Int32"/>
        /// </param>
        public RandomString(int minLength, int maxLength)
        {
            rand = new Random();
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        /// <summary>
        /// Build a random string (for id, login, password...)
        /// </summary>
        public string Next()
        {
            int length = rand.Next(minLength, maxLength);
            StringBuilder tempString = new StringBuilder(Guid.NewGuid().ToString());

            tempString = tempString.Replace("-", "");

            while (tempString.Length < length)
            {
                tempString.Append(tempString);
            }

            if (length < tempString.Length)
            {
                tempString = tempString.Remove(0, tempString.Length - length);
            }

            return tempString.ToString();
        }

        Random rand;
        int minLength;
        int maxLength;
    }

    #region StatUtils;
    /// <summary>
    /// Making different statistical computations. 
    /// For the moment implemented for an integer list, need to convert it to any numerical type.
    /// </summary>
    public class StatUtils
    {
        #region List Statistics;

        /// <summary>
        /// Length of a List
        /// </summary>
        /// <param name="lst">List of <see cref="System.Double"/></param>
        /// <returns><see cref="System.Int32"/></returns>
        public static int Length(List<double> lst)
        {
            int n = 0;
            foreach (double x in lst)
            {
                n++;
            }
            return n;
        }

        /// <summary>
        /// Average (mean) of a list of doubles
        /// </summary>
        /// <param name="lst">List of <see cref="System.Double"/></param>
        /// <returns><see cref="System.Double"/></returns>
        public static double Mean(List<double> lst)
        {
            return lst.Average();
        }

        /// <summary>
        /// Standard Deviation of a List of Doubles
        /// </summary>
        /// <param name="lst">List of <see cref="System.Double"/></param>
        /// <returns>see cref="System.Double"/></returns>
        public static double StdDev(List<double> lst)
        {
            double m = Mean(lst);
            double sd = 0.0;
            foreach (int l in lst)
            {
                sd += (l - m) * (l - m);
            }
            return Math.Sqrt(sd) / (Length(lst) - 1);
        }

        /// <summary>
        /// Maximum of List of doubles
        /// </summary>
        /// <param name="lst">List of <see cref="System.Double"/></param>
        /// <returns>see cref="System.Double"/></returns>
        public static double Max(List<double> lst)
        {
            return lst.Max();
        }

        /// <summary>
        /// Minimum of List of doubles
        /// </summary>
        /// <param name="lst">List of <see cref="System.Double"/></param>
        /// <returns>see cref="System.Double"/></returns>
        public static double Min(List<double> lst)
        {
            return lst.Min();
        }

        #endregion;


        #region Math & Stat formulae;
        //////////////////////////////////////////////////////////////////////////////
        //                  Statistical and mathematical tools
        //----------------------------------------------------------------------------

        /// <summary>
        /// Normal Distribution density function.
        /// 1st derivative of the cumilative function
        /// </summary>
        /// <param name="x">Critical Value, <see cref="System.Double"/></param>
        /// <returns>Cumulative Normal Dist., <see cref="System.Double"/></returns>
        public static double NormalCalcPrime(double x)
        {
            const double c = 0.39894228;
            return c * Math.Exp(-x * x / 2.0);
        }//NormalCalcPrime

        /// <summary>
        /// Calculates cumulative Normal distribution (CDF).
        /// </summary>
        /// <param name="x">Critical value, <see cref="System.Double"/></param>
        /// <returns>Normal CDF (probability), <see cref="System.Double"/></returns>
        public static double NormalCalc(double x)
        {
            const double b1 = 0.319381530;
            const double b2 = -0.356563782;
            const double b3 = 1.781477937;
            const double b4 = -1.821255978;
            const double b5 = 1.330274429;
            const double p = 0.2316419;
            const double c = 0.39894228;
            if (x >= 0.0)
            {
                double t = 1.0 / (1.0 + p * x);
                return (1.0 - c * Math.Exp(-x * x / 2.0) *
                    t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
            else
            {
                double t = 1.0 / (1.0 - p * x);
                return (c * Math.Exp(-x * x / 2.0) *
                    t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
        }// NormalCalc

        #endregion;

        #region Black-Scholes Merton;
        /// <summary>
        /// BSM calculator for vanilla European options
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> annualized underlying volatility</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike price</param>
        /// <param name="uPrice"><see cref="System.Double"/> Underlying asset price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns>Options' BSM price, in case of error returns -1</returns>
        public static double CalcBSPrice(
            OptionType type, 
            double vol, 
            double rf, 
            double strike, 
            double uPrice, 
            double T
            )
        {
            double d1 = (Math.Log(uPrice / strike) + (rf + vol * vol / 2) * T) / (vol * Math.Sqrt(T));
            double d2 = d1 - vol * Math.Sqrt(T);

                if(type== OptionType.CALL)
                    return (uPrice * NormalCalc(d1) - strike * Math.Exp(-rf * T) * NormalCalc(d2));
                else 
                    return (strike * Math.Exp(-rf * T) * NormalCalc(-d2) - uPrice * NormalCalc(-d1));

        }//CalcBSPrice

        #endregion;

        #region BSM analytical Greeks;

        /// <summary>
        /// Analytical BSM Delta, 1st partial derivative 
        /// with respect to underlying price
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> annualized underlying volatility</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike</param>
        /// <param name="uPrice"><see cref="System.Double"/> Underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns><see cref="System.Double"/> Delta, in case of error returns -99999</returns>
        public static double CalcDelta(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double uPrice,
            double T
            )
        {
            double d1 = (Math.Log(uPrice / strike) + (rf + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));

            if (type == OptionType.CALL) return StatUtils.NormalCalc(d1);
            else return -StatUtils.NormalCalc(-d1);

        }//CalcDelta

        /// <summary>
        /// BSM analytical Gamma, the second partial derivative with respect
        /// to the underlying price, Delta sensitivity to the changes in the 
        /// underlying. Note that it's the same for both Call and Put.
        /// </summary>
        /// <param name="vol"><see cref="System.Double"/> annualized underlying volatility</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/>Strike</param>
        /// <param name="uPrice"><see cref="System.Double"/>Underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns><see cref="System.Double"/>Gamma</returns>
        public static double CalcGamma(
            double vol,
            double rf,
            double strike,
            double uPrice,
            double T
            )
        {
            double d1 = (Math.Log(uPrice / strike) + (rf + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));
            return StatUtils.NormalCalcPrime(d1) / (uPrice * vol * Math.Sqrt(T));
        }//CalcGamma

        /// <summary>
        /// Theta, the 1st partial derivative with respect to T (time to expiration).
        /// Option's sensitivity to the pass of time.
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/>underlying volatility</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/>Strike</param>
        /// <param name="uPrice"><see cref="System.Double"/>Underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns><see cref="System.Double"/>Theta</returns>
        public static double CalcTheta(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double uPrice,
            double T
            )
        {
            double d1 = (Math.Log(uPrice / strike) + (rf + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));
            double d2 = d1 - vol * Math.Sqrt(T);
            
            if (type==OptionType.CALL)
                    return (-uPrice * NormalCalcPrime(d1) * vol) / (2.0 * Math.Sqrt(T)) - 
                        rf * strike * Math.Exp(-rf * T) * NormalCalc(d2);
            else
                    return (-uPrice * NormalCalcPrime(d1) * vol) / (2.0 * Math.Sqrt(T)) + 
                        rf * strike * Math.Exp(-rf * T) * NormalCalc(-d2);

        }//CalcTheta

        /// <summary>
        /// Vega, 1st partial derivative with respect to the volatility.
        /// Note that it's the same for both Call and Put
        /// </summary>
        /// <param name="vol"><see cref="System.Double"/>underlying volatility</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/>Strike</param>
        /// <param name="uPrice"><see cref="System.Double"/> underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns><see cref="System.Double"/>Vega</returns>
        public static double CalcVega(
            double vol,
            double rf,
            double strike,
            double uPrice,
            double T
            )
        {
            double d1 = (Math.Log(uPrice / strike) + (rf + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));
            return uPrice * NormalCalcPrime(d1) * Math.Sqrt(T);
        }//CalcVega

        /// <summary>
        /// implied volatility using Newton-Raphson method
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="premium"><see cref="System.Double"/> Option's premium</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike</param>
        /// <param name="uPrice"><see cref="System.Double"/> Underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <returns></returns>
        public static double CalcIV(
            OptionType type,
            double premium,
            double rf,
            double strike,
            double uPrice,
            double T
            )
        {
            const double ACCURACY = 1.0e-6;
            double tryIV =             // initial value of volatility
                Math.Pow(Math.Abs(Math.Log(uPrice / strike) + rf * T) * 2 / T, 0.5);
            double tryPremium = CalcBSPrice(type, tryIV, rf, strike, uPrice, T);
            double Vega = CalcVega(tryIV, rf, strike, uPrice, T);

            while (Math.Abs(premium - tryPremium) > ACCURACY)
            {
                tryIV = tryIV - ((tryPremium - premium) / Vega);
                tryPremium = CalcBSPrice(type, tryIV, rf, strike, uPrice, T);
                Vega = CalcVega(tryIV, rf, strike, uPrice, T);
            }

            return tryIV;
        }//CalcIV

        #endregion;

        #region Numerical Greeks;

        //////////////////////////////////////////////////////////////////////////////
        //            Numerical Greeks, using finite-difference method
        //----------------------------------------------------------------------------

        /// <summary>
        /// Numerical Delta, by finite-difference
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> volatility, assumes constant over small changes</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike</param>
        /// <param name="S"><see cref="System.Double"/> underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <param name="dS"><see cref="System.Double"/> d(underlying price)</param>
        /// <returns>Numerical Delta</returns>
        public static double CalcNDelta(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double S,
            double T,
            double dS
            )
        {
            return (CalcBSPrice(type, vol, rf, strike, S + dS, T) -
                CalcBSPrice(type, vol, rf, strike, S - dS, T)) / (2 * dS);
        }

        /// <summary>
        /// Overloaded numerical Delta, to address
        /// volatility skew impact
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike price</param>
        /// <param name="S"><see cref="System.Double"/> underlying asset price</param>
        /// <param name="T"><see cref="System.Double"/> time to expiration (in years)</param>
        /// <param name="dS"><see cref="System.Double"/> d(underlying price)</param>
        /// <param name="vol_up"><see cref="System.Double"/> underlying-up-volatility</param>
        /// <param name="vol_down"><see cref="System.Double"/> underlying-down-volatility</param>
        /// <returns><see cref="System.Double"/> Numerical Delta</returns>
        public static double CalcNDelta(
            OptionType type,
            double rf,
            double strike,
            double S,
            double T,
            double dS,
            double vol_up,
            double vol_down
            )
        {
            return (CalcBSPrice(type, vol_up, rf, strike, S + dS, T) -
                CalcBSPrice(type, vol_down, rf, strike, S - dS, T)) / (2 * dS);
        }

        /// <summary>
        /// Numerical Gamma using finite-difference
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> volatility, assumes constant over small changes</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike price</param>
        /// <param name="S"><see cref="System.Double"/> underlying asset price</param>
        /// <param name="T"><see cref="System.Double"/> time to expiration (in years)</param>
        /// <param name="dS"><see cref="System.Double"/> d(underlying price)</param>
        /// <returns><see cref="System.Double"/> Numerical Gamma</returns>
        public static double CalcNGamma(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double S,
            double T,
            double dS
            )
        {
            return (CalcBSPrice(type, vol, rf, strike, S + dS, T) -
                2.0 * CalcBSPrice(type, vol, rf, strike, S, T) +
                CalcBSPrice(type, vol, rf, strike, S - dS, T)) / (dS * dS);
        }

        /// <summary>
        /// Numerical Gamma - overloaded mathod
        /// addressing volatility skew.
        /// Computed by finite-difference
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> volatility, assumes constant over small changes</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike price</param>
        /// <param name="S"><see cref="System.Double"/> underlying asset price</param>
        /// <param name="T"><see cref="System.Double"/> time to expiration (in years)</param>
        /// <param name="dS"><see cref="System.Double"/> d(underlying price)</param>
        /// <param name="vol_up"><see cref="System.Double"/> underlying-up-volatility</param>
        /// <param name="vol_down"><see cref="System.Double"/> underlying-down-volatility</param>
        /// <returns><see cref="System.Double"/> Numerical Gamma</returns>
        public static double CalcNGamma(
            OptionType type,
            double rf,
            double vol,
            double strike,
            double S,
            double T,
            double dS,
            double vol_up,
            double vol_down)
        {
            return (CalcBSPrice(type, vol_up, rf, strike, S + dS, T) -
                2.0 * CalcBSPrice(type, vol, rf, strike, S, T) +
                CalcBSPrice(type, vol_down, rf, strike, S - dS, T)) / (dS * dS);
        }


        /// <summary>
        /// Numerical Theta, by finite-difference
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> volatility, assumes constant over small changes</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike price</param>
        /// <param name="S"><see cref="System.Double"/> underlying asset price</param>
        /// <param name="T"><see cref="System.Double"/> time to expiration (in years)</param>
        /// <param name="dT"><see cref="System.Double"/> d(Time to expiration) --in years. Note here that time
        /// always goes forward</param>
        /// <returns><see cref="System.Double"/> Numerical Theta</returns>
        public static double CalcNTheta(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double S,
            double T,
            double dT
            )
        {
            return (CalcBSPrice(type, vol, rf, strike, S, T) -
                CalcBSPrice(type, vol, rf, strike, S, T - dT)) / (dT);
        }



        /// <summary>
        /// Numerical Vega, by finite-difference
        /// </summary>
        /// <param name="type">Either CALL or PUT, <see cref="JQuant.OptionType"/></param>
        /// <param name="vol"><see cref="System.Double"/> volatility, assumes constant over small changes</param>
        /// <param name="rf"><see cref="System.Double"/> annualized (risk-free) inerest rate</param>
        /// <param name="strike"><see cref="System.Double"/> Strike</param>
        /// <param name="S"><see cref="System.Double"/> underlying price</param>
        /// <param name="T"><see cref="System.Double"/> Time to expiration (in years)</param>
        /// <param name="dV"><see cref="System.Double"/> d(volatility)</param>
        /// <returns>Numerical Vega</returns>
        public static double CalcNVega(
            OptionType type,
            double vol,
            double rf,
            double strike,
            double S,
            double T,
            double dV
            )
        {
            return (CalcBSPrice(type, vol + dV, rf, strike, S, T) -
                CalcBSPrice(type, vol - dV, rf, strike, S, T)) / (2 * dV);
        }

        #endregion;

        #region Maof Specs
        //////////////////////////////////////////////////////////////////////////////
        //              Maof Options Specialities

        /// <summary>
        /// This one addresses the fact that Maof options are priced in constatnt increments.
        /// </summary>
        /// <param name="premium"><see cref="System.Double"/> the original premium</param>
        /// <returns><see cref="System.Int32"/> whole number of NIS</returns>
        public static int RoundPremia(double premium)
        {
            if (premium >= 2000.0) return ((int)Math.Round(premium / 20.0)) * 20;
            else if (premium >= 200.0) return ((int)Math.Round(premium / 10.0)) * 10;
            else if (premium >= 20.0) return ((int)Math.Round(premium / 5.0)) * 5;
            else return (int)Math.Round(premium);
        }

        /// <summary>
        /// Computes the nearest increment (up-tick) resulting from
        /// Maof options pricing rules.
        /// </summary>
        /// <param name="premium"><see cref="System.Double"/> the original premium</param>
        /// <returns><see cref="System.Int32"/> whole number of NIS</returns>
        public static double PremiaIncrement(double premium)
        {
            if (RoundPremia(premium) >= 2000.0) return 20.0;
            else if (RoundPremia(premium) >= 200.0) return 10.0;
            else if (RoundPremia(premium) >= 20.0) return 5.0;
            else return 1.0;
        }

        /// <summary>
        /// Computes the nearest decrement (down-tick) resulting from
        /// Maof options pricing rules.
        /// </summary>
        /// <param name="premium"><see cref="System.Double"/> the original premium</param>
        /// <returns><see cref="System.Int32"/> whole number of NIS</returns>
        public static double PremiaDecrement(double premium)
        {
            if (RoundPremia(premium) >= 2020.0) return 20.0;
            else if (RoundPremia(premium) >= 210.0) return 10.0;
            else if (RoundPremia(premium) >= 25.0) return 5.0;
            else return 1.0;
        }

        #endregion;

    }
    #endregion;
}
