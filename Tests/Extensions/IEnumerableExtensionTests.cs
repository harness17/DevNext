using Dev.CommonLibrary.Extensions;
using Xunit;

namespace Tests.Extensions
{
    public class IEnumerableExtensionTests
    {
        [Fact]
        public void IsNullOrEmpty_Null_ReturnsTrue()
        {
            IEnumerable<int>? source = null;
            Assert.True(source.IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_Empty_ReturnsTrue()
        {
            Assert.True(Array.Empty<int>().IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_WithItems_ReturnsFalse()
        {
            Assert.False(new[] { 1, 2, 3 }.IsNullOrEmpty());
        }
    }
}
