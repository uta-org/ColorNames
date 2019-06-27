using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnidecodeSharpCore;

namespace ColorNames.Lib
{
    public static class F
    {
        public static string Sanitize(this string s)
        {
            return String.Join("", s.AsEnumerable()
                    .Select(chr => Char.IsLetter(chr) || Char.IsDigit(chr)
                        ? chr.ToString()      // valid symbol
                        : "_" + (short)chr + "_") // numeric code for invalid symbol
            );
        }

        public static string[] GetLines(this string input)
        {
            return Regex.Split(input, @"\r?\n|\r");
        }

        public static string SanitizeEnum(this string input)
        {
            string str = input.Unidecode().Sanitize();
            return char.IsDigit(str[0]) ? "_" + str : str;
        }

        public static long GetBytes(this WebClient wc, string url)
        {
            wc.OpenRead(url);
            return Convert.ToInt64(wc.ResponseHeaders["Content-Length"]);
        }
    }
}