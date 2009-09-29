
using System;
using System.ComponentModel;

namespace JQuant
{
    public enum SecurityType
    {
        Derivative,
        Stock,
        Bond
    }

    public enum OptionType
    {
        [Description("Call")]
        CALL,
        [Description("PUT")]
        PUT,
    }

    #region Security;
    /// <summary>
    /// Security class implements generic security
    /// serving base for all instruments traded on TASE
    /// </summary>
    abstract public class Security
    {
        //Public properties
        public int IdNum
        {
            get { return idNum; }
            set { idNum = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        //Constructor
        public Security()
        {
            idNum = 0;
            Name  = "Security";
            //LimitBook = new LOB();
            //lastTrans = new Transaction();
        }

        //Security class private members
        private int idNum;                      //security's id number on TASE
        private string name;                    //its name
        private SecurityType securityType;    //whether stock, bond or derivative
        private string description;           //short textual description

    }   //class Security 
    #endregion;

    #region Option;
    /// <summary>
    /// Currently defines MAOF index options.
    /// </summary>
    /// 
    class Option : Security
    {

        //Members
        private OptionType type;    //'C'all or 'P'ut
        private double strike;      //strike
        private DateTime expDate;   //expiration date
        private double T;           //Time to expiration
        private double vol;         //volatility

        // These parameters aren't specific for the option instances, 
        // therefore they aren't members of the Option type:
        //
        //private double rate;              //risk-free interest
        //private double S;                 //underlying price

        //Properties
        public OptionType Type
        {
            get { return this.type; }
            protected set { type = value; }
        }

        public double Strike
        {
            get { return strike; }
            protected set {strike = value; }
        }

        //Methods
        #region Methods - Greeks
        //Greeks
        /// <summary>
        /// Computes option's delta, given the market conditions:
        /// (volatility, underlying price, rf and T). Either numerical or analytical
        /// value is returned, depending on the presence of "Numerical" flag
        /// </summary>
        /// <param name="vol">volatility</param>
        /// <param name="rate">(risk-free) interest rate</param>
        /// <param name="S">underlying price</param>
        /// <param name="T">time to expiration</param>
        /// <param name="Numerical">perform numerical computation, rather than analytival one</param>
        /// <returns>Delta, options sensitivity to the underlying price changes</returns>
        public double Delta(double vol, double rate, double S, double T, bool Numerical)
        {
            if (Numerical) return StatUtils.CalcNDelta(type, vol, rate, strike, S, T, S+1.0);
            else return StatUtils.CalcDelta(type, vol, rate, strike, S, T);
        }

        /// <summary>
        /// Computes option's Gamma, given the market conditions:
        /// (volatility, underlying price, rf and T)
        /// </summary>
        /// <param name="vol">volatility</param>
        /// <param name="rate">(risk-free) interest rate</param>
        /// <param name="S">underlying price</param>
        /// <param name="T">time to expiration</param>
        /// <returns>Gamma, option's Delta sensitivity 
        /// to the underlying price changes</returns>
        public double Gamma(double vol, double rate, double S, double T, bool Numerical)
        {
            if (Numerical) return StatUtils.CalcNGamma(type, vol, rate, strike, S, T, 1.0);
            else return StatUtils.CalcGamma(vol, rate, strike, S, T);
        }

        /// <summary>
        /// Computes option's Vega, given the market conditions:
        /// (volatility, underlying price, rf and T)
        /// </summary>
        /// <param name="vol">volatility</param>
        /// <param name="rate">(risk-free) interest rate</param>
        /// <param name="S">underlying price</param>
        /// <param name="T">time to expiration</param>
        /// <returns>Vega, option's sensitivity 
        /// to the volatility changes</returns>
        public double Vega(double vol, double rate, double S, double T, bool Numerical)
        {
            if (Numerical) return StatUtils.CalcNVega(type, vol, rate, strike, S, T, vol / 100);
            return StatUtils.CalcVega(vol, rate, strike, S, T);
        }
        /// <summary>
        /// Computes option's Theta, given the market conditions:
        /// (volatility, underlying price, rf and T)
        /// </summary>
        /// <param name="vol">volatility</param>
        /// <param name="rate">(risk-free) interest rate</param>
        /// <param name="S">underlying price</param>
        /// <param name="T">time to expiration</param>
        /// <returns>Theta, option's sensitivity 
        /// to the pass of time</returns>

        public double Theta(double vol, double rate, double S, double T, bool Numerical)
        {
            return StatUtils.CalcTheta(type, vol, rate, strike, S, T);
        }

        #endregion;

        #region Constructors;
        //Constructors
        public Option()
            : base()
        {
            expDate = new DateTime(2009, 6, 30);    //uses DateTime(int,int,int) constructor
            strike = 820;
            //this.OptLOB = new LOB();
        }

        public Option(double X, OptionType ot, System.DateTime ExDate, int OptID)
        {
            expDate = ExDate;
            strike = X;
            type = ot;
            IdNum = OptID;
            Name = ot + " " + X.ToString() + " " +
                ExDate.Date.ToLongDateString().Split(',', ' ')[2].Substring(0, 3).ToUpper();
        }
        #endregion;
        #region Strings;
        public override string ToString()
        {
            return Name;
        }
        #endregion;
    }//class Option

    #endregion;
}   //namespace