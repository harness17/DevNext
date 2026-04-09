using Dev.CommonLibrary.Common;
using Xunit;

namespace Tests.Common
{
    public class UtilTests
    {
        // ---- calcMd5 ----

        [Fact]
        public void CalcMd5_KnownInput_ReturnsExpectedHash()
        {
            // "test" の MD5 は既知の値
            var result = Util.CalcMd5("test");
            Assert.Equal("098f6bcd4621d373cade4e832627b4f6", result);
        }

        [Fact]
        public void CalcMd5_SameInputTwice_ReturnsSameHash()
        {
            var hash1 = Util.CalcMd5("hello");
            var hash2 = Util.CalcMd5("hello");
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalcMd5_DifferentInputs_ReturnDifferentHashes()
        {
            Assert.NotEqual(Util.CalcMd5("foo"), Util.CalcMd5("bar"));
        }

        // ---- SetFileName ----

        [Fact]
        public void SetFileName_PathWithBackslash_ReturnsFilenameOnly()
        {
            var result = Util.SetFileName(@"C:\Users\harne\files\document.pdf");
            Assert.Equal("document.pdf", result);
        }

        [Fact]
        public void SetFileName_NoBackslash_ReturnsAsIs()
        {
            var result = Util.SetFileName("document.pdf");
            Assert.Equal("document.pdf", result);
        }

        // ---- IsSafePath ----

        [Theory]
        [InlineData("normalfile.txt", true, true)]   // 正常なファイル名
        [InlineData("normal/path", false, true)]   // 正常なパス
        [InlineData("CON", true, false)]  // Windows 予約デバイス名
        [InlineData("NUL.txt", true, false)]  // 拡張子付き予約名
        [InlineData("COM1", true, false)]  // シリアルポート予約名
        public void IsSafePath_ReturnsExpected(string path, bool isFileName, bool expected)
        {
            Assert.Equal(expected, Util.IsSafePath(path, isFileName));
        }

        [Fact]
        public void IsSafePath_NullOrEmpty_ReturnsFalse()
        {
            Assert.False(Util.IsSafePath("", true));
            Assert.False(Util.IsSafePath("", false));
        }

        // ---- CreateSummary ----

        [Fact]
        public void CreateSummary_FirstPage_ReturnsCorrectRange()
        {
            // 1ページ目、10件/ページ、全30件
            var pager = new CommonListPagerModel(page: 1, recoedNumber: 10);
            var result = Util.CreateSummary(pager, 30, "{0}件中 {1}〜{2}件");

            Assert.Equal(1, result.FirstRecord);
            Assert.Equal(10, result.EndRecord);
            Assert.Equal(30, result.TotalRecords);
            Assert.Equal(1, result.CurrentPage);
        }

        [Fact]
        public void CreateSummary_LastPage_EndRecordClampsToTotal()
        {
            // 3ページ目、10件/ページ、全25件 → EndRecord は 25 に丸まる
            var pager = new CommonListPagerModel(page: 3, recoedNumber: 10);
            var result = Util.CreateSummary(pager, 25, "{0}件中 {1}〜{2}件");

            Assert.Equal(21, result.FirstRecord);
            Assert.Equal(25, result.EndRecord);  // 30 ではなく 25 に丸まること
        }

        [Fact]
        public void CreateSummary_SummaryStringIsFormatted()
        {
            var pager = new CommonListPagerModel(page: 1, recoedNumber: 10);
            var result = Util.CreateSummary(pager, 30, "{0}件中 {1}〜{2}件");

            Assert.Equal("30件中 1〜10件", result.Summary);
        }
    }
}
