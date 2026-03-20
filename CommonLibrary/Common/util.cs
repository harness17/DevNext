using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// Enum関連Utility
    /// </summary>
    public static class EnumUtility
    {
        public static string GetEnumDisplay<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return string.Empty;
            var attrs = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attrs == null || attrs.Length == 0) return value;
            var attr = attrs[0];
            if (attr.ResourceType != null) return attr.GetName() ?? value;
            return attr.Name ?? value;
        }

        public static string GetEnumSubValue<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return string.Empty;
            var attrs = fieldInfo.GetCustomAttributes(typeof(SubValueAttribute), false) as SubValueAttribute[];
            if (attrs == null || attrs.Length == 0) return string.Empty;
            return attrs[0].SubValue;
        }

        public static int GetEnumDisplayOrder<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return 0;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return 0;
            var attrs = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attrs == null || attrs.Length == 0) return 0;
            return attrs[0].Order;
        }

        public static string GetEnumDescription<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var field = type.GetField(name);
            if (field == null) return string.Empty;
            var customAttribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return customAttribute.Length > 0 ? ((DescriptionAttribute)customAttribute[0]).Description : name;
        }

        private static string? GetEnumName(Type type, string value)
        {
            return Enum.GetNames(type)
                .Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
        }

        public static string GetDescription(Type T, string name)
        {
            var attributes = (DescriptionAttribute[])T.GetField(name)!
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            var description = attributes.Select(n => n.Description).FirstOrDefault();
            return string.IsNullOrEmpty(description) ? name : description!;
        }
    }

    /// <summary>
    /// SelectList関連Utility
    /// </summary>
    public static class SelectListUtility
    {
        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = area,
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>(List<T> obj) where T : struct
        {
            var list = new List<SelectListItem>();
            var targetAreas = obj.Select(s => s.ToString()).ToList();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                if (targetAreas.Contains(area))
                {
                    list.Add(new SelectListItem { Value = area, Text = EnumUtility.GetEnumDisplay<T>(area) });
                }
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemToSubValue<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = EnumUtility.GetEnumSubValue<T>(area),
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemOrder<T>() where T : struct
        {
            var sortlist = Enum.GetNames(typeof(T))
                .Select(area => new { Name = area, Text = EnumUtility.GetEnumDisplay<T>(area), Order = EnumUtility.GetEnumDisplayOrder<T>(area) })
                .OrderBy(x => x.Order)
                .ToList();

            return sortlist.Select(area => new SelectListItem { Value = area.Name, Text = area.Text }).ToList();
        }

        public static IEnumerable<SelectListItem> GetNumberSelectList(int startNumber, int maxNumber, int step = 1, string format = "")
        {
            var list = new List<SelectListItem>();
            for (int i = startNumber; i <= maxNumber; i += step)
            {
                list.Add(new SelectListItem { Value = i.ToString(format), Text = i.ToString(format) });
            }
            return list;
        }
    }

    /// <summary>
    /// Cookie操作Utility (ASP.NET Core版)
    /// </summary>
    public static class CookieUtility
    {
        public static string? GetCookieValueByKey(IRequestCookieCollection cookies, string key)
        {
            cookies.TryGetValue(key, out var value);
            return value;
        }

        public static void SetCookie(IResponseCookies cookies, string key, string value)
        {
            cookies.Append(key, value, new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.Now.AddMonths(1)
            });
        }

        public static void DeleteCookie(IRequestCookieCollection requestCookies, IResponseCookies responseCookies, string key)
        {
            if (!requestCookies.ContainsKey(key)) return;
            responseCookies.Append(key, "", new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.Now.AddMonths(-1)
            });
        }
    }

    /// <summary>
    /// 共通関数クラス
    /// </summary>
    public static class util
    {
        public static string calcMd5(string srcStr)
        {
            byte[] srcBytes = Encoding.UTF8.GetBytes(srcStr);
            byte[] destBytes = MD5.HashData(srcBytes);
            var sb = new StringBuilder();
            foreach (byte b in destBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static string SetFileName(string strFileName)
        {
            string targettext = "\\";
            if (strFileName.Contains(targettext))
            {
                int lastindex = strFileName.LastIndexOf(targettext);
                strFileName = strFileName.Substring(lastindex + targettext.Length);
            }
            return strFileName;
        }

        public static bool IsSafePath(string path, bool isFileName)
        {
            if (string.IsNullOrEmpty(path)) return false;
            char[] invalidChars = isFileName
                ? Path.GetInvalidFileNameChars()
                : Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0) return false;
            if (Regex.IsMatch(path, ConstRegExpr.InValidFileName, RegexOptions.IgnoreCase)) return false;
            return true;
        }

        public static CommonListSummaryModel CreateSummary(CommonListPagerModel pager, int totalRecords, string listSummaryFormat)
        {
            int pageIndex = pager.page - 1;
            int firstRecord = (pageIndex * pager.recoedNumber) + 1;
            int endRecord = firstRecord - 1 + pager.recoedNumber;
            if (firstRecord <= totalRecords && totalRecords <= endRecord) endRecord = totalRecords;
            string summary = string.Format(listSummaryFormat, totalRecords, firstRecord, endRecord);
            return new CommonListSummaryModel(pager.page, totalRecords, firstRecord, endRecord, summary);
        }
    }
}
