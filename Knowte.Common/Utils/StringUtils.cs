using System;
using System.Linq;

namespace Knowte.Common.Utils
{
    public sealed class StringUtils
    {
        /// <summary>
        /// Splits strings on spaces, but preserves string which are between double quotes
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string[] SplitWords(string inputString)
        {
            // return inputString.Split(new char[] { ' ' });

            return inputString.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToArray();
        }
    }
}
