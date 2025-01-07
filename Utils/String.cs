using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
