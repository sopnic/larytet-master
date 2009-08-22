using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;


namespace JQuant
{


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
}
