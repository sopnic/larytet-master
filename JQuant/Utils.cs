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
        static public string FormatField(int value, int fieldSize)
        {
            StringBuilder s = new StringBuilder("" + value, fieldSize);

            int count = s.Length;
            for (int i = count; i < fieldSize; i++)
            {
                s.Insert(0, ' ');
            }

            return s.ToString();
        }

        /// <summary>
        /// add trailing blanks to the string if neccessary 
        /// </summary>
        static public string FormatField(string value, int fieldSize)
        {
            StringBuilder s = new StringBuilder("" + value, fieldSize);

            int count = s.Length;
            for (int i = count; i < fieldSize; i++)
            {
                s.Insert(0, ' ');
            }

            return s.ToString();
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
            Type t = typeof(FMRShell.MarketData);
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
                sbLegend.Append(Delimiter);
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
                sbData.Append(Delimiter);
            }
            Values = sbData.ToString();

            IsInitialized = true;
        }

        protected FieldInfo[]fields;
        protected StructType data;
        protected string delimiter;
    }

    public class RandomString
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
}
