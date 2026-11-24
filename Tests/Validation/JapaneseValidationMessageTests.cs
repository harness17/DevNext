using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Site.Common;
using Xunit;

namespace Tests.Validation
{
    /// <summary>
    /// 実際の MVC メタデータパイプラインを組み立て、属性に割り当てられた日本語メッセージを検証する
    /// </summary>
    public class JapaneseValidationMessageTests
    {
        private class SampleModel
        {
            [Required]
            [Display(Name = "氏名")]
            public string? Name { get; set; }

            [Required(ErrorMessage = "独自メッセージ")]
            [Display(Name = "タイトル")]
            public string? Title { get; set; }

            [StringLength(10)]
            [Display(Name = "コード")]
            public string? Code { get; set; }

            [StringLength(10, MinimumLength = 3)]
            [Display(Name = "略称")]
            public string? ShortName { get; set; }

            [MaxLength(20)]
            [Display(Name = "備考")]
            public string? Note { get; set; }

            [Range(1, 99)]
            [Display(Name = "数量")]
            public int Quantity { get; set; }

            [EmailAddress]
            [Display(Name = "メール")]
            public string? Email { get; set; }

            [Compare(nameof(Email))]
            [Display(Name = "メール（確認）")]
            public string? ConfirmEmail { get; set; }

            // Nullable 有効時の非 null 参照型には MVC が暗黙に [Required] を付与する（プロバイダーの登録順の回帰確認）
            [Display(Name = "区分")]
            public string Kind { get; set; } = "";

            [MinLength(4)]
            [Display(Name = "パスコード")]
            public string? PassCode { get; set; }

            [Phone]
            [Display(Name = "電話番号")]
            public string? Tel { get; set; }

            [Url]
            [Display(Name = "サイト")]
            public string? Site { get; set; }

            [RegularExpression("^[0-9]+$")]
            [Display(Name = "郵便番号")]
            public string? Zip { get; set; }

            [Required(ErrorMessageResourceType = typeof(TestMessages), ErrorMessageResourceName = nameof(TestMessages.Custom))]
            [Display(Name = "リソース指定")]
            public string? ResourceBased { get; set; }
        }

        /// <summary>ErrorMessageResourceName 指定時に上書きされないことを確かめるためのリソース代替</summary>
        public static class TestMessages
        {
            public static string Custom => "リソースの文言";
        }

        private static (IModelMetadataProvider Provider, MvcOptions Options) BuildMvc()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews(options => options.UseJapaneseValidationMessages());
            var sp = services.BuildServiceProvider();
            return (sp.GetRequiredService<IModelMetadataProvider>(), sp.GetRequiredService<IOptions<MvcOptions>>().Value);
        }

        /// <summary>プロパティに付いた指定型の検証属性を表示名つきでフォーマットした結果を返す</summary>
        private static string Format<TAttribute>(string propertyName) where TAttribute : ValidationAttribute
        {
            var (provider, _) = BuildMvc();
            var metadata = provider.GetMetadataForProperty(typeof(SampleModel), propertyName);
            var attribute = metadata.ValidatorMetadata.OfType<TAttribute>().Single();
            return attribute.FormatErrorMessage(metadata.GetDisplayName());
        }

        [Fact]
        public void Required_WithoutErrorMessage_ReturnsJapanese()
        {
            Assert.Equal("氏名は必須です。", Format<RequiredAttribute>(nameof(SampleModel.Name)));
        }

        [Fact]
        public void Required_WithExplicitErrorMessage_KeepsOriginal()
        {
            Assert.Equal("独自メッセージ", Format<RequiredAttribute>(nameof(SampleModel.Title)));
        }

        [Fact]
        public void StringLength_WithoutMinimum_ReturnsMaxOnlyMessage()
        {
            Assert.Equal("コードは10文字以内で入力してください。", Format<StringLengthAttribute>(nameof(SampleModel.Code)));
        }

        [Fact]
        public void StringLength_WithMinimum_ReturnsRangeMessage()
        {
            Assert.Equal("略称は3文字以上10文字以内で入力してください。", Format<StringLengthAttribute>(nameof(SampleModel.ShortName)));
        }

        [Fact]
        public void MaxLength_ReturnsJapanese()
        {
            Assert.Equal("備考は20文字以内で入力してください。", Format<MaxLengthAttribute>(nameof(SampleModel.Note)));
        }

        [Fact]
        public void Range_ReturnsJapaneseWithBounds()
        {
            Assert.Equal("数量は1～99の範囲で入力してください。", Format<RangeAttribute>(nameof(SampleModel.Quantity)));
        }

        [Fact]
        public void EmailAddress_ReturnsJapanese()
        {
            Assert.Equal("メールはメールアドレスの形式で入力してください。", Format<EmailAddressAttribute>(nameof(SampleModel.Email)));
        }

        [Fact]
        public void Compare_ReturnsJapaneseWithOtherProperty()
        {
            // CompareAttribute は他方プロパティの表示名をメタデータ生成時に解決できないため、プロパティ名がそのまま入る
            Assert.Equal("メール（確認）とEmailが一致しません。", Format<CompareAttribute>(nameof(SampleModel.ConfirmEmail)));
        }

        [Fact]
        public void ImplicitRequired_ForNonNullableReference_ReturnsJapanese()
        {
            Assert.Equal("区分は必須です。", Format<RequiredAttribute>(nameof(SampleModel.Kind)));
        }

        [Fact]
        public void MinLength_ReturnsJapanese()
        {
            Assert.Equal("パスコードは4文字以上で入力してください。", Format<MinLengthAttribute>(nameof(SampleModel.PassCode)));
        }

        [Fact]
        public void Phone_ReturnsJapanese()
        {
            Assert.Equal("電話番号は電話番号の形式で入力してください。", Format<PhoneAttribute>(nameof(SampleModel.Tel)));
        }

        [Fact]
        public void Url_ReturnsJapanese()
        {
            Assert.Equal("サイトはURLの形式で入力してください。", Format<UrlAttribute>(nameof(SampleModel.Site)));
        }

        [Fact]
        public void RegularExpression_ReturnsJapanese()
        {
            Assert.Equal("郵便番号の形式が正しくありません。", Format<RegularExpressionAttribute>(nameof(SampleModel.Zip)));
        }

        [Fact]
        public void Required_WithErrorMessageResourceName_KeepsResource()
        {
            Assert.Equal("リソースの文言", Format<RequiredAttribute>(nameof(SampleModel.ResourceBased)));
        }

        [Fact]
        public void ModelBinding_NullForNonNullable_DoesNotEmbedValue()
        {
            // 引数には入力値（空文字）が渡るため、値を埋め込むと「は必須です。」のように欠ける。その回帰確認
            var (_, options) = BuildMvc();
            Assert.Equal("値は必須です。", options.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(""));
        }

        [Fact]
        public void ModelBinding_NonPropertyAndMissingBindRequired_ReturnJapanese()
        {
            var (_, options) = BuildMvc();
            var provider = options.ModelBindingMessageProvider;
            Assert.Equal("値「abc」は正しくありません。", provider.NonPropertyAttemptedValueIsInvalidAccessor("abc"));
            Assert.Equal("数値で入力してください。", provider.NonPropertyValueMustBeANumberAccessor());
            Assert.Equal("数量は必須です。", provider.MissingBindRequiredValueAccessor("数量"));
        }

        [Fact]
        public void ModelBinding_InvalidNumber_ReturnsJapanese()
        {
            var (_, options) = BuildMvc();
            Assert.Equal("数量の値「abc」は正しくありません。",
                options.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor("abc", "数量"));
            Assert.Equal("数量は数値で入力してください。",
                options.ModelBindingMessageProvider.ValueMustBeANumberAccessor("数量"));
        }
    }
}
