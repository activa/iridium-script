using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iridium.Script
{
    public delegate TOut Converter<in TIn, out TOut>(TIn input);

    public static class ScriptExtensions
    {
        public static TOutput[] ConvertAll<TInput, TOutput>(this TInput[] array, Converter<TInput, TOutput> converter)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var newArray = new TOutput[array.Length];

            for (int i = 0; i < array.Length; i++)
                newArray[i] = converter(array[i]);

            return newArray;
        }
    }
}
