using Dev.CommonLibrary.Extensions;
using Xunit;

namespace Tests.Extensions
{
    public class StringExtensionsTests
    {
        // ---- HtmlEncode / HtmlDecode ----

        [Fact]
        public void HtmlEncode_WithSpecialChars_ReturnsEncoded()
        {
            var result = "<script>alert('xss')</script>".HtmlEncode();
            Assert.Equal("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", result);
        }

        [Fact]
        public void HtmlEncode_Null_ReturnsNull()
        {
            string? input = null;
            Assert.Null(input.HtmlEncode());
        }

        [Fact]
        public void HtmlDecode_WithEntities_ReturnsDecoded()
        {
            var result = "&lt;b&gt;text&lt;/b&gt;".HtmlDecode();
            Assert.Equal("<b>text</b>", result);
        }

        [Fact]
        public void HtmlDecode_Null_ReturnsNull()
        {
            string? input = null;
            Assert.Null(input.HtmlDecode());
        }

        // ---- GetBrText ----

        [Theory]
        [InlineData("line1\r\nline2", "line1<br />line2")]
        [InlineData("line1\nline2", "line1<br />line2")]
        [InlineData("line1\rline2", "line1<br />line2")]
        [InlineData("no newline", "no newline")]
        public void GetBrText_ReplacesNewlinesWithBrTag(string input, string expected)
        {
            Assert.Equal(expected, input.GetBrText());
        }

        [Fact]
        public void GetBrText_Null_ReturnsEmpty()
        {
            string? input = null;
            Assert.Equal(string.Empty, input.GetBrText());
        }

        // ---- ContainsAll ----

        [Fact]
        public void ContainsAll_AllValuesPresent_ReturnsTrue()
        {
            Assert.True("hello world foo".ContainsAll("hello", "world", "foo"));
        }

        [Fact]
        public void ContainsAll_SomeValuesMissing_ReturnsFalse()
        {
            Assert.False("hello world".ContainsAll("hello", "missing"));
        }

        // ---- ContainsAny ----

        [Fact]
        public void ContainsAny_OneValuePresent_ReturnsTrue()
        {
            Assert.True("hello world".ContainsAny("hello", "missing"));
        }

        [Fact]
        public void ContainsAny_NonePresent_ReturnsFalse()
        {
            Assert.False("hello world".ContainsAny("foo", "bar"));
        }

        // ---- Left / Mid / Right ----

        [Theory]
        [InlineData("abcdef", 3, "abc")]
        [InlineData("ab", 5, "ab")]   // 文字列より長い length は全体を返す
        public void Left_ReturnsExpected(string input, int length, string expected)
        {
            Assert.Equal(expected, input.Left(length));
        }

        [Theory]
        [InlineData("abcdef", 3, "def")]
        [InlineData("ab", 5, "ab")]   // 文字列より長い length は全体を返す
        public void Right_ReturnsExpected(string input, int length, string expected)
        {
            Assert.Equal(expected, input.Right(length));
        }

        [Fact]
        public void Mid_ReturnsSubstring()
        {
            Assert.Equal("bcd", "abcdef".Mid(1, 3));
        }
    }
}
