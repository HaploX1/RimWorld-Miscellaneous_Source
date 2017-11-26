using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Blueprint2MapGenConverter
{
    public static class Helper_Text
    {
        private const char DegreeSymbol = '°';

        private static StringBuilder tmpSb = new StringBuilder();

        public static string WithoutVowels(string s)
        {
            string vowels = "aeiouy";
            return new string((from c in s
                               where !vowels.Contains(c)
                               select c).ToArray<char>());
        }

        public static string TrimEndNewlines(this string s)
        {
            return s.TrimEnd(new char[]
            {
                '\r',
                '\n'
            });
        }

        public static string Indented(this string s)
        {
            if (s.NullOrEmpty())
            {
                return s;
            }
            return "    " + s.Replace("\r", string.Empty).Replace("\n", "\n    ");
        }

        public static string ReplaceFirst(this string source, string key, string replacement)
        {
            int num = source.IndexOf(key);
            if (num < 0)
            {
                return source;
            }
            return source.Substring(0, num) + replacement + source.Substring(num + key.Length);
        }

        public static int StableStringHash(string str)
        {
            if (str == null)
            {
                return 0;
            }
            int num = 23;
            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                num = num * 31 + (int)str[i];
            }
            return num;
        }

        public static string StringFromEnumerable<T>(IEnumerable<T> source)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (object current in source)
            {
                stringBuilder.AppendLine("� " + current.ToString());
            }
            return stringBuilder.ToString();
        }
        
        public static IEnumerable<string> LinesFromString(string text)
        {
            string[] separator = new string[]
            {
                "\r\n",
                "\n"
            };
            string[] array = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++)
            {
                string text2 = array[i];
                string text3 = text2.Trim();
                if (!text3.StartsWith("//"))
                {
                    text3 = text3.Split(new string[]
                    {
                        "//"
                    }, StringSplitOptions.None)[0];
                    if (text3.Length != 0)
                    {
                        yield return text3;
                    }
                }
            }
            yield break;
        }

        public static bool IsValidFilename(string str)
        {
            if (str.Length > 30)
            {
                return false;
            }
            string str2 = new string(Path.GetInvalidFileNameChars()) + "/\\{}<>:*|!@#$%^&*?";
            Regex regex = new Regex("[" + Regex.Escape(str2) + "]");
            return !regex.IsMatch(str);
        }

        public static bool NullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string SplitCamelCase(string Str)
        {
            return Regex.Replace(Str, "(?<a>(?<!^)((?:[A-Z][a-z])|(?:(?<!^[A-Z]+)[A-Z0-9]+(?:(?=[A-Z][a-z])|$))|(?:[0-9]+)))", " ${a}");
        }

        public static string CapitalizedNoSpaces(string s)
        {
            string[] array = s.Split(new char[]
            {
                ' '
            });
            StringBuilder stringBuilder = new StringBuilder();
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string text = array2[i];
                if (text.Length > 0)
                {
                    stringBuilder.Append(char.ToUpper(text[0]));
                }
                if (text.Length > 1)
                {
                    stringBuilder.Append(text.Substring(1));
                }
            }
            return stringBuilder.ToString();
        }

        public static string RemoveNonAlphanumeric(string s)
        {
            tmpSb.Length = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    tmpSb.Append(s[i]);
                }
            }
            return tmpSb.ToString();
        }

        public static bool EqualsIgnoreCase(this string A, string B)
        {
            return string.Compare(A, B, true) == 0;
        }

        public static string WithoutByteOrderMark(this string str)
        {
            return str.Trim().Trim(new char[]
            {
                '﻿'
            });
        }

        public static string CapitalizeFirst(this string str)
        {
            if (str.NullOrEmpty())
            {
                return str;
            }
            if (char.IsUpper(str[0]))
            {
                return str;
            }
            if (str.Length == 1)
            {
                return str.ToUpper();
            }
            return str[0].ToString().ToUpper() + str.Substring(1);
        }

        public static string ToNewsCase(string str)
        {
            string[] array = str.Split(new char[]
            {
                ' '
            });
            for (int i = 0; i < array.Length; i++)
            {
                string text = array[i];
                if (text.Length >= 2)
                {
                    if (i == 0)
                    {
                        array[i] = text[0].ToString().ToUpper() + text.Substring(1);
                    }
                    else
                    {
                        array[i] = text.ToLower();
                    }
                }
            }
            return string.Join(" ", array);
        }

        public static string CapitalizeSentences(string input)
        {
            if (input.NullOrEmpty())
            {
                return input;
            }
            if (input.Length == 1)
            {
                return input.ToUpper();
            }
            input = Regex.Replace(input, "\\s+", " ");
            input = input.Trim();
            input = char.ToUpper(input[0]) + input.Substring(1);
            string[] array = new string[]
            {
                ". ",
                "! ",
                "? "
            };
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string text = array2[i];
                int length = text.Length;
                for (int j = input.IndexOf(text, 0); j > -1; j = input.IndexOf(text, j + 1))
                {
                    input = input.Substring(0, j + length) + input[j + length].ToString().ToUpper() + input.Substring(j + length + 1);
                }
            }
            return input;
        }

        public static string ToCommaList(IEnumerable<object> items, bool useAnd = true)
        {
            return ToCommaList(from it in items
                                       select it.ToString(), useAnd);
        }

        public static string ToCommaList(IEnumerable<string> items, bool useAnd = true)
        {
            string text = null;
            string text2 = null;
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder();
            IList<string> list = items as IList<string>;
            if (list != null)
            {
                num = list.Count;
                for (int i = 0; i < num; i++)
                {
                    string text3 = list[i];
                    if (!text3.NullOrEmpty())
                    {
                        if (text2 == null)
                        {
                            text2 = text3;
                        }
                        if (text != null)
                        {
                            stringBuilder.Append(text + ", ");
                        }
                        text = text3;
                    }
                }
            }
            else
            {
                foreach (string current in items)
                {
                    if (!current.NullOrEmpty())
                    {
                        if (text2 == null)
                        {
                            text2 = current;
                        }
                        if (text != null)
                        {
                            stringBuilder.Append(text + ", ");
                        }
                        text = current;
                        num++;
                    }
                }
            }
            if (num == 0)
            {
                return "none";
            }
            if (num == 1)
            {
                return text;
            }
            if (!useAnd)
            {
                stringBuilder.Append(", " + text);
                return stringBuilder.ToString();
            }
            if (num == 2)
            {
                return string.Concat(new string[]
                {
                    text2,
                    " ",
                    "and",
                    " ",
                    text
                });
            }
            stringBuilder.Append("and" + " " + text);
            return stringBuilder.ToString();
        }

        public static string ToSpaceList(IEnumerable<string> entries)
        {
            return ToTextList(entries, " ");
        }

        public static string ToTextList(IEnumerable<string> entries, string spacer)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = true;
            foreach (string current in entries)
            {
                if (!flag)
                {
                    stringBuilder.Append(spacer);
                }
                stringBuilder.Append(current);
                flag = false;
            }
            return stringBuilder.ToString();
        }


        public static string ToStringPercent(this float f, string format)
        {
            return ((f + 1E-05f) * 100f).ToString(format) + "%";
        }

        public static string ToStringMoney(this float f)
        {
            return "$" + f.ToString("F2");
        }

        public static string ToStringWithSign(this int i)
        {
            return i.ToString("+#;-#;0");
        }

        public static string ToStringKilobytes(this int bytes, string format = "F2")
        {
            return ((float)bytes / 1024f).ToString(format) + "Kb";
        }

        public static string ToStringLongitude(this float longitude)
        {
            bool flag = longitude < 0f;
            if (flag)
            {
                longitude = -longitude;
            }
            return longitude.ToString("F2") + '°' + ((!flag) ? "E" : "W");
        }

        public static string ToStringLatitude(this float latitude)
        {
            bool flag = latitude < 0f;
            if (flag)
            {
                latitude = -latitude;
            }
            return latitude.ToString("F2") + '°' + ((!flag) ? "N" : "S");
        }

        public static string ToStringMass(this float mass)
        {
            if (mass == 0f)
            {
                return "0 kg";
            }
            if (Math.Abs(mass) < 0.01f)
            {
                return (mass * 1000f).ToString("0.##") + " g";
            }
            return mass.ToString("0.##") + " kg";
        }

        public static string ToStringMassOffset(this float mass)
        {
            string text = mass.ToStringMass();
            if (mass > 0f)
            {
                return "+" + text;
            }
            return text;
        }

        public static string ToStringBytes(this int b, string format = "F2")
        {
            return ((float)b / 8f / 1024f).ToString(format) + "kb";
        }

        public static string ToStringBytes(this uint b, string format = "F2")
        {
            return (b / 8f / 1024f).ToString(format) + "kb";
        }
    }
}
