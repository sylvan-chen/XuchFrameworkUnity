using System;
using System.Text;

namespace XuchFramework.Core.Utils
{
    public static class StringHelper
    {
        /// <summary>
        /// Convert '-' or '_' separated string to PascalCase
        /// </summary>
        public static string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            str = str.Replace("_", " ").Replace("-", " ");
            var words = str.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join("", words);
        }

        /// <summary>
        /// Convert string array to string, split by space
        /// </summary>
        public static string ConvertArrayToStr(string[] array)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i != array.Length - 1)
                    sb.Append(' ');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert string (slit by space) to string array
        /// </summary>
        public static string[] ConvertStrToArray(string str)
        {
            return str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        #region TimeStr

        public static string SecondsToTimeStr(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\:mm\:ss");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\:ss");
            else
                return span.ToString(@"ss");
        }

        public static string SecondsToTimeStr_hms(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\hmm\mss\s");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\mss\s");
            else
                return span.ToString(@"ss\s");
        }

        public static string SecondsToTimeStr_HMS(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\Hmm\Mss\S");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\Mss\S");
            else
                return span.ToString(@"ss\S");
        }

        #endregion
    }
}