using System;
using System.Collections.Generic;
using System.Text;

namespace TacControl.Common
{
    public static class Utility
    {
        public static string Join<TItem>(this IEnumerable<TItem> enumerable, string separator = ", ")
        {
            return string.Join(separator, enumerable);
        }

    }
}
