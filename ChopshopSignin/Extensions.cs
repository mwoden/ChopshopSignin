using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChopshopSignin
{
    static class Extensions
    {
        public static IList<T> ToReadOnly<T>(this IEnumerable<T> source)
        {
            return new System.Collections.ObjectModel.ReadOnlyCollection<T>(source.ToArray());
        }
    }
}
