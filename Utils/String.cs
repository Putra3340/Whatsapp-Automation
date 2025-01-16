using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WhatsApp;

namespace StringExt
{

    public static partial class StringExtensions
    {

        public static bool IsNotNullOrEmpty(this string pe)
        {
            if (pe == null || pe == "")
            {
                return false;
            }
            return true;
        }
        public static bool IsNullOrEmpty(this string pe)
        {
            if (pe == null || pe == "")
            {
                return true;
            }
            return false;
        }
        public static string AddPrefix(this string command)
        {
            return Messages.Prefix + command;
        }
        public static string GetArgs(this string msg)
        {
            // Split the string by space and skip the first element
            string[] result = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1..];

            // Join the remaining parts for demonstration (optional)
            string joinedResult = string.Join(" ", result);
            return joinedResult;
        }
        public static string ExtractNumbers(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Use Regex to find all numbers in the string
            MatchCollection matches = Regex.Matches(input, @"\d+");
            return string.Join("", matches);  // Combine all numbers into a single string
        }
    }
}
