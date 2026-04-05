using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// ã§í ä÷êîÉNÉâÉX
    /// </summary>
    public static class Util
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
