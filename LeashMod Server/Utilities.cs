using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LeashMod_Server
{
    public static class Utilities
    {
        public static string GetTextAfter(this string sourceString, string getTextAfterThis, string getToThis = null, int lengthOfWhatToGetAfterPrevString = 90000)
        {
            if (lengthOfWhatToGetAfterPrevString == 90000)
            {
                return sourceString.Substring(sourceString.IndexOf(getTextAfterThis) + getTextAfterThis.Length);
            }

            if (lengthOfWhatToGetAfterPrevString < sourceString.Length)
            {
                return sourceString.Substring(sourceString.IndexOf(getTextAfterThis) + getTextAfterThis.Length, (!string.IsNullOrEmpty(getToThis) ? sourceString.Replace(getTextAfterThis, "").IndexOf(getToThis) : lengthOfWhatToGetAfterPrevString));
            }

            return null;
        }

        public static string GetToEndOfLine(this string sourceString)
        {
            return sourceString.Substring(0, sourceString.IndexOf("\n")).Replace("\r", "");
        }
    }
}
