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

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    for (var value = e.Current; e.MoveNext(); value = e.Current)
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}
