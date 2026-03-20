using System.Web;

namespace Dev.CommonLibrary.Extensions
{
    public static class StringExtensions
    {
        public static string? HtmlEncode(this string? value) => value == null ? null : HttpUtility.HtmlEncode(value);
        public static string? HtmlDecode(this string? value) => value == null ? null : HttpUtility.HtmlDecode(value);

        public static string GetBrText(this string? value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\r\n", "<br />").Replace("\r", "<br />").Replace("\n", "<br />");
        }

        public static bool ContainsAll(this string source, params string[] values)
        {
            return values.All(v => source.Contains(v));
        }

        public static bool ContainsAny(this string source, params string[] values)
        {
            return values.Any(v => source.Contains(v));
        }

        public static string Left(this string s, int length) => s.Length <= length ? s : s.Substring(0, length);
        public static string Mid(this string s, int start, int length) => s.Substring(start, Math.Min(length, s.Length - start));
        public static string Right(this string s, int length) => s.Length <= length ? s : s.Substring(s.Length - length);
    }
}
