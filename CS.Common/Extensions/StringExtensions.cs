﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CS.Common.Extensions
{
    public static class StringExtensions
    {
        public static readonly HashSet<char> Base64Characters = new HashSet<char> {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/',
            '='
        };

        public const string NormalEmailAddress = @"^([\w\!\#$\%\&\'\*\-\/\=\?\^\`{\|\}\~]+\.)*[\w\!\#$\%\&\'\*\-\/\=\?\^\`{\|\}\~]+@((((([a-zA-Z0-9]{1}[a-zA-Z0-9\-]{0,62}[a-zA-Z0-9]{1})|[a-zA-Z])\.)+[a-zA-Z]{2,6})|(\d{1,3}\.){3}\d{1,3}(\:\d{1,5})?)$";

        public static bool IsBase64String(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (value.Any(c => !Base64Characters.Contains(c)))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static T? EnumParse<T>(this string input) where T : struct
        {
            if (Enum.TryParse(input, true, out T result))
                return result;

            return null;
        }

        public static string SplitAndInsert(this string input, char separator, string textToInsert, int index)
        {
            var parts = input.Split(separator).ToList();
            parts.Insert(index + 1, textToInsert);
            return string.Join(separator.ToString(), parts);
        }

        public static string HashHMACBase64(this string message, string keyHex)
        {
            var hash = HexDecode(keyHex).HashHMAC(UTF8StringEncode(message));
            return Convert.ToBase64String(hash);
        }

        public static string HashHMACHex(this string message, string keyHex)
        {
            var hash = HexDecode(keyHex).HashHMAC(UTF8StringEncode(message));
            return hash.HashEncode();
        }

        public static string MaybeSubstring(this string message, int length)
        {
            if (message.Length <= length)
                return message;

            return message.Substring(0, length);
        }

        public static byte[] ASCIIStringEncode(this string text)
        {
            var encoding = new ASCIIEncoding();
            return encoding.GetBytes(text);
        }

        public static byte[] UTF8StringEncode(this string text)
        {
            var encoding = new UTF8Encoding();
            return encoding.GetBytes(text);
        }

        public static byte[] HexDecode(this string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }

        public static byte[] Compress(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Decompress(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static string LowercaseFirstChar(this string text)
        {
            if (!string.IsNullOrEmpty(text) && char.IsUpper(text[0]))
                return char.ToLower(text[0]) + text.Substring(1);

            return text;
        }

        public static string UppercaseFirstChar(this string text)
        {
            if (!string.IsNullOrEmpty(text) && char.IsLower(text[0]))
                return char.ToUpper(text[0]) + text.Substring(1);

            return text;
        }

        public static string Normalise(this string text)
        {
            return RemoveDiacritics(text).Trim().ToLowerInvariant();
        }

        public static int ToIntOrDefault(this string s, int defaultValue = -1)
        {
            if (!int.TryParse(s, out var result))
                return defaultValue;

            return result;
        }

        public static int? ToNullableInt(this string s)
        {
            if (!int.TryParse(s, out var result))
                return null;

            return result;
        }

        public static string ToLiteral(this string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            literal.Append("\"");
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        if (Char.GetUnicodeCategory(c) != UnicodeCategory.Control)
                        {
                            literal.Append(c);
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((ushort)c).ToString("x4"));
                        }
                        break;
                }
            }
            literal.Append("\"");
            return literal.ToString();
        }

        public static Dictionary<string, string> ParseQueryString(this string query, bool decode = true)
        {
            var result = new Dictionary<string, string>();

            foreach (string token in query.TrimStart(new char[] { '?' }).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                    result[parts[0].Trim()] = decode ? HttpUtility.UrlDecode(parts[1]).Trim() : parts[1].Trim();
                else
                    result[parts[0].Trim()] = "";
            }

            return result;
        }

        /// <summary>Checks whether a string can be converted to an integer.</summary>
        /// <returns>true if <paramref name="value" /> can be converted to the specified type; otherwise, false.</returns>
        /// <param name="value">The string value to test.</param>
        public static bool IsInt(this string value)
        {
            int result;
            return int.TryParse(value, out result);
        }

        public static bool IsAsciiCharacters(this string value)
        {
            string sOut = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
            return sOut == value;
        }

        public static bool StartsWithAny(this string value, params string[] tests)
        {
            return tests.Select(t => t.ToLowerInvariant()).Any(value.ToLowerInvariant().StartsWith);
        }

        public static string GetUrlPath(this string s)
        {
            s = s.Trim();

            if (s.StartsWith("http"))
            {
                return new Uri(s).AbsolutePath;
            }

            s = s.StartsWith("/") ? s : "/{0}".FormatWith(s);
            var index = s.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);
            return s.Substring(0, index == -1 ? s.Length : index).ToLowerInvariant();
        }

        public static string TrimTo(this string s, int count, string ending = "...")
        {
            return s == null ? string.Empty : s.Length > count ? s.Substring(0, count) + ending : s;
        }

        public static string FirstXAsciiCharacters(this string s, int count)
        {
            return new string(s.ToLowerInvariant().Where(IsAsciiAz).Take(count).Select(s1 => s1).ToArray());
        }

        public static bool InvariantCultureIgnoreCaseCompare(this string obj, string compareTo)
        {
            return string.Equals(obj?.Trim(), compareTo?.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        public static T ChangeType<T>(this string obj)
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }

        public static string StripLineBreaks(this string s, string replace = "")
        {
            return s?.Replace("\r\n", replace).Replace("\r", replace).Replace("\n", replace).Trim();
        }

        public static string GetQueueName(this string destination)
        {
            var parts = destination.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return parts[parts.Length - 1];
        }

        public static Dictionary<string, T> ToDictionary<T>(this string s, Func<string, T> valueAction)
        {
            if (s.IsNullOrEmpty())
                return new Dictionary<string, T>();

            return s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(kvp => kvp[0], kvp => valueAction(kvp.Length > 1 ? kvp[1] : null));
        }

        public static string GetNumbers(this string s, int? count = null)
        {
            var result = s.AsEnumerable().Where(c => char.IsDigit(c) || c == ' ' || c == '-');
            return new String(count.HasValue && count.Value < result.Count() ? result.Take(count.Value).ToArray() : result.ToArray());
        }

        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool IsNotNullOrEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static bool IsSameAs(this string source, string dest)
        {
            return source.InvariantCultureIgnoreCaseCompare(dest) || (source.IsNullOrEmpty() && dest.IsNullOrEmpty());
        }

        public static string RavenEscape(this string s)
        {
            return s.Replace(":", "");
        }

        public static string FormatWith(this string s, params object[] args)
        {
            return string.Format(s, args);
        }

        public static string TryFormatWith(this string s, string failureValue, params object[] args)
        {
            try
            {
                return string.Format(s, args);
            }
            catch
            {
                return failureValue;
            }
        }

        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static string LastElement(this string input, char seperator)
        {
            if (input == null)
                return string.Empty;

            string[] elements = input.Split(new[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
            return elements[elements.Length - 1];
        }

        public static string Base64Encode(this string input)
        {
            if (input.IsNullOrEmpty())
                return string.Empty;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        public static string Base64Decode(this string input)
        {
            if (input.IsNullOrEmpty())
                return string.Empty;

            return Encoding.UTF8.GetString(Convert.FromBase64String(input));
        }

        public static string MergeQueryStrings(this string query1, string query2)
        {
            var nvc1 = HttpUtility.ParseQueryString(query1);
            var nvc2 = HttpUtility.ParseQueryString(query2);
            return nvc1.MergeWith(nvc2).ToString();
        }

        public static string ReplaceFirstOccurrance(this string original, string oldValue, string newValue)
        {
            if (String.IsNullOrEmpty(original))
                return String.Empty;
            if (String.IsNullOrEmpty(oldValue))
                return original;
            if (String.IsNullOrEmpty(newValue))
                newValue = String.Empty;

            int loc = original.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
            return loc >= 0 ? original.Remove(loc, oldValue.Length).Insert(loc, newValue) : original;
        }

        public static string ReplaceLastOccurrance(this string original, string oldValue, string newValue)
        {
            if (String.IsNullOrEmpty(original))
                return String.Empty;
            if (String.IsNullOrEmpty(oldValue))
                return original;
            if (String.IsNullOrEmpty(newValue))
                newValue = String.Empty;

            int loc = original.LastIndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
            return loc >= 0 ? original.Remove(loc, oldValue.Length).Insert(loc, newValue) : original;
        }

        public static string Hash(this string input)
        {
            // step 1, calculate MD5 hash from input
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                // step 2, convert byte array to hex string
                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static string Quantity(this string singularForm, int qty, string irregularPluralForm = null)
        {
            return qty + " " + Pluralise(singularForm, qty, irregularPluralForm);
        }

        public static string Pluralise(this string singularForm, int qty, string irregularPluralForm = null)
        {
            if (qty == 1)
                return singularForm;

            return irregularPluralForm ?? (singularForm + "s");
        }

        /// <summary>
        /// Remove all characters except 0-9 and a-z, replace spaces with - and remove double spaces at the end
        /// </summary>
        /// <param name="value"></param>
        /// <param name="spaceReplacement"></param>
        /// <param name="allowPeriod"></param>
        /// <returns></returns>
        public static string SanitizeForUrl(this string value, string spaceReplacement = "-", bool allowPeriod = false)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder();

            char[] originalChars = value.UnicodeToAscii().Trim().ToLowerInvariant().ToCharArray();

            foreach (char t in originalChars)
            {
                if (AcceptableUrlChar(t)) //only allow a-z and 0-9
                    builder.Append(t);
                else if (t == ' ') //replace spaces with hyphen
                    builder.Append(spaceReplacement);
                else if (t == '.' && allowPeriod)
                    builder.Append(".");
            }

            return builder.ToString().Replace("--", "-");
        }

        /// <summary>
        /// Remove all characters for language detection
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizeForTranslation(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder();

            char[] originalChars = value.Trim().ToLowerInvariant().ToCharArray();

            foreach (char t in originalChars)
            {
                if (t > 192)
                {
                    builder.Append(t);
                }
            }

            return builder.ToString();
        }

        public static string RemoveDiacritics(this string text)
        {
            //http://stackoverflow.com/a/249126/3856
            //note this only removes diacritics it doesn't ascii-ise
            //characters that are genuine single characters (e.g. ß, æ)
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string UnicodeToAscii(this string text)
        {
            //http://stackoverflow.com/a/2086575/3856
            //note that ß -> ? with this
            byte[] tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(text);
            string asciiStr = Encoding.UTF8.GetString(tempBytes);
            return asciiStr;
        }

        /// <summary>
        /// Only returns true if character is a-z or 0-9
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private static bool AcceptableUrlChar(char character)
        {
            return ((character > 96 && character < 123) || (character > 47 && character < 58) || character == '-' || (character > 191 && character < 383));
        }

        /// <summary>
        /// Only returns true if character is a-z
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private static bool IsAsciiAz(char character)
        {
            return (character > 96 && character < 123);
        }

        /// <summary>
        /// Remove illegal XML characters from a string. For more info see this http://stackoverflow.com/a/12469826/52360
        /// </summary>
        public static string SanitizeForXml(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var buffer = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (IsLegalXmlChar(c))
                {
                    buffer.Append(c);
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        public static bool IsLegalXmlChar(int character)
        {
            return
                (
                    character == 0x9 /* == '\t' == 9   */          ||
                    character == 0xA /* == '\n' == 10  */          ||
                    character == 0xD /* == '\r' == 13  */          ||
                    (character >= 0x20 && character <= 0xD7FF) ||
                    (character >= 0xE000 && character <= 0xFFFD) ||
                    (character >= 0x10000 && character <= 0x10FFFF)
                );
        }

        public static string CleanPhoneNumber(this string s)
        {
            return s.IsNullOrEmpty() ? null : s.Trim().TrimStart('0');
        }

        public static string Limit(this string s, int max, string ending = "...")
        {
            return s.Length > max ? "{0}{1}".FormatWith(s.Substring(0, max), ending) : s;
        }

        public static string ToPascalCase(this string s, char delimiter = ' ', bool keepDelimiter = true)
        {
            if (s == null) return null;
            if (s.Length < 2) return s.ToUpper();

            var words = s.Split(delimiter);

            var result = string.Join(keepDelimiter ? delimiter.ToString() : string.Empty, words.Select(word => word.FirstLetterToUpper()).ToArray());
            return result;
        }

        public static string PutSpacesInPascalCase(this string s, bool firstLetterToUpper = false)
        {
            var result = SpacesInPascalCaseRegex.Replace(s, m => string.Format("{0} {1}", m.Groups["notcaps"].Value, m.Groups["caps"].Value));
            return firstLetterToUpper ? result.FirstLetterToUpper() : result;
        }

        private static readonly Regex SpacesInPascalCaseRegex = new Regex("(?'notcaps'[^A-Z^ ])(?'caps'[A-Z])", RegexOptions.Compiled);

        public static string WordsBeforeCharacterX(this string value, int index, string continuationString = null, int graceRegion = 0)
        {
            var split = value.SplitWordsAtCharacterX(index, graceRegion);

            return split.Item1 + (split.Item2.IsNullOrEmpty() ? "" : continuationString);
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public static Tuple<string, string> SplitWordsAtCharacterX(this string value, int index, int graceRegion = 0)
        {
            if (value == null) return new Tuple<string, string>(null, null);

            if (value.Length - index < graceRegion)
                return Tuple.Create(value, (string)null);

            var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int cumu = 0;
            var boundary = words.Where(w => (cumu += w.Length + 1) <= index + 1).Aggregate(0, (i, word) => i + 1);

            var before = words.Take(boundary).StringConcat(" ");
            var after = words.Skip(boundary).StringConcat(" ");

            return Tuple.Create(before.TrimEnd(), after.TrimEnd());
        }

        public static string SafeReplace(this string value, string oldValue, string newValue)
        {
            if (value == null)
                return null;

            return value.Replace(oldValue, newValue);
        }

        public static string SafeGetFileName(this string fileName)
        {
            int length;
            if ((length = fileName.LastIndexOf('.')) == -1)
                return fileName;
            return StripQuerystring(fileName.Substring(0, length));
        }

        public static string SafeGetFileExtension(this string fileName)
        {
            return GetFileExtension(fileName) ?? fileName;
        }

        public static string GetFileExtension(this string fileName)
        {
            int length;
            if ((length = fileName.LastIndexOf('.')) == -1)
                return null;

            return StripQuerystring(fileName.Substring(length, fileName.Length - length));
        }

        public static string StripQuerystring(this string input)
        {
            return input.Substring(0, input.IndexOf('?') != -1 ? input.IndexOf('?') : input.Length);
        }

        public static List<string> Wrap(this string s, int maxLength)
        {
            if (s.Length == 0) return new List<string>();

            var words = s.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var currentWord in words)
            {

                if ((currentLine.Length > maxLength) ||
                    ((currentLine.Length + currentWord.Length) > maxLength))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                if (currentLine.Length > 0)
                    currentLine += " " + currentWord;
                else
                    currentLine += currentWord;

            }

            if (currentLine.Length > 0)
                lines.Add(currentLine);


            return lines;
        }

        public static string EscapeForCsv(this string str)
        {
            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }

        public static string GetAlphanumericsOnly(this string str)
        {
            if (str == null)
                return null;

            return (new Regex("[^a-zA-Z0-9]")).Replace(str, "");
        }

        public static bool IsValidEmailAddress(this string m)
        {
            if (m.IsNullOrEmpty())
                return false;

            Regex normalEmailRegex = new Regex(NormalEmailAddress);
            Match normalMatch = normalEmailRegex.Match(m);
            return normalMatch.Success;
        }
    }
}
