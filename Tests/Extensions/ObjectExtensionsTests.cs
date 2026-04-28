using Dev.CommonLibrary.Extensions;
using Xunit;

namespace Tests.Extensions
{
    public class ObjectExtensionsTests
    {
        // ---- ToStringOrDefault ----

        [Fact]
        public void ToStringOrDefault_Null_ReturnsNull()
        {
            object? value = null;
            Assert.Null(value.ToStringOrDefault());
        }

        [Fact]
        public void ToStringOrDefault_WithValue_ReturnsString()
        {
            Assert.Equal("42", ((object)42).ToStringOrDefault());
        }

        [Fact]
        public void ToStringOrDefault_WithFormat_ReturnsFormatted()
        {
            var date = new DateTime(2026, 4, 1);
            Assert.Equal("2026/04/01", ((object)date).ToStringOrDefault("yyyy/MM/dd"));
        }

        // ---- ToStringOrEmpty ----

        [Fact]
        public void ToStringOrEmpty_Null_ReturnsEmpty()
        {
            object? value = null;
            Assert.Equal(string.Empty, value.ToStringOrEmpty());
        }

        [Fact]
        public void ToStringOrEmpty_WithValue_ReturnsString()
        {
            Assert.Equal("hello", ((object)"hello").ToStringOrEmpty());
        }
    }
}
