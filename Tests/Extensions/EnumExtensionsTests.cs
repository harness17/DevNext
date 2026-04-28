using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Extensions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Tests.Extensions
{
    public class EnumExtensionsTests
    {
        private enum SampleEnum
        {
            [Display(Name = "表示名A", Description = "説明A")]
            ValueA,

            [Display(Name = "表示名B")]
            ValueB,

            [SubValue("sub1")]
            ValueC,

            ValueD  // 属性なし
        }

        // ---- DisplayName ----

        [Fact]
        public void DisplayName_WithDisplayAttribute_ReturnsDisplayName()
        {
            Assert.Equal("表示名A", SampleEnum.ValueA.DisplayName());
        }

        [Fact]
        public void DisplayName_NoAttribute_ReturnsEnumName()
        {
            // Display 属性がない場合は Enum メンバー名を返す
            Assert.Equal("ValueD", SampleEnum.ValueD.DisplayName());
        }

        // ---- DisplayDescription ----

        [Fact]
        public void DisplayDescription_WithDescription_ReturnsDescription()
        {
            Assert.Equal("説明A", SampleEnum.ValueA.DisplayDescription());
        }

        [Fact]
        public void DisplayDescription_NoDescription_ReturnsNull()
        {
            // Description を持たない場合は null を返す
            Assert.Null(SampleEnum.ValueB.DisplayDescription());
        }

        [Fact]
        public void DisplayDescription_NoAttribute_ReturnsNull()
        {
            Assert.Null(SampleEnum.ValueD.DisplayDescription());
        }

        // ---- ToSubValue ----

        [Fact]
        public void ToSubValue_WithSubValueAttribute_ReturnsSubValue()
        {
            Assert.Equal("sub1", SampleEnum.ValueC.ToSubValue<string>());
        }

        [Fact]
        public void ToSubValue_NoAttribute_ReturnsDefault()
        {
            // SubValue 属性がない場合は default（string なら null）を返す
            Assert.Null(SampleEnum.ValueD.ToSubValue<string>());
        }
    }
}
