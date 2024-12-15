using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
}
