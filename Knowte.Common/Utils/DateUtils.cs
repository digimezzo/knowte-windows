using System;
using System.Globalization;

namespace Knowte.Common.Utils
{
    public sealed class DateUtils
    {
        public static int CountDays(DateTime startTime, DateTime endTime)
        {
            TimeSpan span = endTime.Date.Subtract(startTime.Date);

            return span.Days;
        }

        public static string DateDifference(DateTime startTime, DateTime endTime, string formatDate, bool simpleOutput)
        {
            string retVal = string.Empty;

            if (simpleOutput)
            {
                TimeSpan span = endTime.Date.Subtract(startTime.Date);

                if (span.Days > 1 & span.Days < 7)
                {
                    retVal = span.Days + " days ago";
                }
                else if (span.Days == 0)
                {
                    retVal = "Today";
                }
                else if (span.Days == 1)
                {
                    retVal = "Yesterday";
                }
                else if (span.Days >= 7 & span.Days < 14)
                {
                    retVal = "Last week";
                }
                else if (span.Days >= 14 & span.Days < 21)
                {
                    retVal = "Two weeks ago";
                }
                else if (span.Days >= 21 & span.Days < 31)
                {
                    retVal = "Three weeks ago";
                }
                else if (endTime.Month == startTime.Month + 1 & endTime.Year == startTime.Year)
                {
                    retVal = "Last month";
                }
                else if (endTime.Month == startTime.Month + 2 & endTime.Year == startTime.Year)
                {
                    retVal = "2 months ago";
                }
                else if (endTime.Month == startTime.Month + 3 & endTime.Year == startTime.Year)
                {
                    retVal = "3 months ago";
                }
                else if (endTime.Month == startTime.Month + 4 & endTime.Year == startTime.Year)
                {
                    retVal = "4 months ago";
                }
                else if (endTime.Month == startTime.Month + 5 & endTime.Year == startTime.Year)
                {
                    retVal = "5 months ago";
                }
                else if (endTime.Month == startTime.Month + 6 & endTime.Year == startTime.Year)
                {
                    retVal = "6 months ago";
                }
                else if (endTime.Month == startTime.Month + 7 & endTime.Year == startTime.Year)
                {
                    retVal = "7 months ago";
                }
                else if (endTime.Month == startTime.Month + 8 & endTime.Year == startTime.Year)
                {
                    retVal = "8 months ago";
                }
                else if (endTime.Month == startTime.Month + 9 & endTime.Year == startTime.Year)
                {
                    retVal = "9 months ago";
                }
                else if (endTime.Month == startTime.Month + 10 & endTime.Year == startTime.Year)
                {
                    retVal = "10 months ago";
                }
                else if (endTime.Month == startTime.Month + 11 & endTime.Year == startTime.Year)
                {
                    retVal = "11 months ago";
                }
                else
                {
                    retVal = "Long ago";
                }
            }
            else
            {
                retVal = startTime.ToString(formatDate, CultureInfo.CurrentCulture);
            }
            return retVal;

        }
    }
}