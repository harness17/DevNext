using Microsoft.AspNetCore.Http;

namespace Dev.CommonLibrary.Common
{
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
}
