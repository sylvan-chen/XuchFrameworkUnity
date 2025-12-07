using System;
using System.Text;

namespace DigiEden.Framework.Utils
{
    public static class StringHelper
    {
        /// <summary>
        /// 转换'-'或'_'分隔法为帕斯卡命名法
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <returns>转换后的字符串</returns>
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

        public static string[] ConvertStrToArray(string str)
        {
            return str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}