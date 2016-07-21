namespace Knowte.Core.Extensions
{
    public static class StringExtensions
    {
        public static string SanitizeFilename(this string iInput)
        {
            string retVal = "";
            string replaceStr = "";

            // Invalid characters for filenames: \ / : * ? " < > |

            retVal = iInput.Replace("\\", replaceStr);
            retVal = retVal.Replace("/", replaceStr);
            retVal = retVal.Replace(":", replaceStr);
            retVal = retVal.Replace("*", replaceStr);
            retVal = retVal.Replace("?", replaceStr);
            retVal = retVal.Replace("\"", replaceStr);
            retVal = retVal.Replace("<", replaceStr);
            retVal = retVal.Replace(">", replaceStr);
            retVal = retVal.Replace("|", replaceStr);

            return retVal;
        }
    }
}
