using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Site.Common
{
    /// <summary>
    /// ErrorMessage 未指定の検証属性に日本語の既定メッセージを割り当てるメタデータプロバイダー
    /// </summary>
    /// <remarks>
    /// 属性ごとに ErrorMessage を書かなくても日本語で表示させるため、メタデータ生成時に一度だけ設定する。
    /// 属性側で ErrorMessage / ErrorMessageResourceName を明示している場合はそちらを優先する。
    /// </remarks>
    public class JapaneseValidationMetadataProvider : IValidationMetadataProvider
    {
        /// <summary>
        /// 属性型ごとの既定メッセージ。プレースホルダの意味は各属性の FormatErrorMessage の引数順に従う
        /// </summary>
        private static readonly Dictionary<Type, string> Messages = new()
        {
            [typeof(RequiredAttribute)] = "{0}は必須です。",
            [typeof(MaxLengthAttribute)] = "{0}は{1}文字以内で入力してください。",
            [typeof(MinLengthAttribute)] = "{0}は{1}文字以上で入力してください。",
            [typeof(RangeAttribute)] = "{0}は{1}～{2}の範囲で入力してください。",
            [typeof(EmailAddressAttribute)] = "{0}はメールアドレスの形式で入力してください。",
            [typeof(PhoneAttribute)] = "{0}は電話番号の形式で入力してください。",
            [typeof(UrlAttribute)] = "{0}はURLの形式で入力してください。",
            [typeof(CompareAttribute)] = "{0}と{1}が一致しません。",
            [typeof(RegularExpressionAttribute)] = "{0}の形式が正しくありません。",
        };

        /// <summary>StringLength は MinimumLength の有無で文言を切り替えるため個別に持つ</summary>
        internal const string StringLengthMax = "{0}は{1}文字以内で入力してください。";
        internal const string StringLengthRange = "{0}は{2}文字以上{1}文字以内で入力してください。";

        /// <inheritdoc />
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                // 明示指定を上書きすると ErrorMessage と ResourceName の二重指定で例外になるため、未指定時のみ設定する
                var hasCustomMessage = !string.IsNullOrEmpty(attribute.ErrorMessage)
                    && attribute.ErrorMessage != FrameworkDefaultMessage(attribute);
                if (hasCustomMessage || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
                {
                    continue;
                }

                var message = ResolveMessage(attribute);
                if (message != null)
                {
                    attribute.ErrorMessage = message;
                }
            }
        }

        /// <summary>
        /// DataType 系属性は未指定でも ErrorMessage が .NET 既定文言を返すため、その文言を「未指定」とみなす判定に使う
        /// </summary>
        /// <remarks>
        /// 既定文言は UI カルチャで変わるため、起動時に固定せず判定のたびに新しいインスタンスから取得する
        /// </remarks>
        private static string? FrameworkDefaultMessage(ValidationAttribute attribute)
        {
            // 派生型はコンストラクタ引数が不明なため、既定文言を持つ3型そのものだけを対象にする
            return attribute.GetType() switch
            {
                var t when t == typeof(EmailAddressAttribute) => new EmailAddressAttribute().ErrorMessage,
                var t when t == typeof(PhoneAttribute) => new PhoneAttribute().ErrorMessage,
                var t when t == typeof(UrlAttribute) => new UrlAttribute().ErrorMessage,
                _ => null,
            };
        }

        /// <summary>
        /// 属性に対応する既定メッセージを返す。対象外の属性は null（.NET 既定のまま）
        /// </summary>
        internal static string? ResolveMessage(ValidationAttribute attribute)
        {
            if (attribute is StringLengthAttribute stringLength)
            {
                return stringLength.MinimumLength > 0 ? StringLengthRange : StringLengthMax;
            }

            // 派生属性（独自の RequiredAttribute 継承など）にも適用するため、継承階層をさかのぼって探す。
            // 派生属性がコンストラクタで持たせた既定文言も日本語既定で上書きする（文言を固定したい場合は ErrorMessage を明示する）
            for (var type = attribute.GetType(); type != null && type != typeof(ValidationAttribute); type = type.BaseType)
            {
                if (Messages.TryGetValue(type, out var message))
                {
                    return message;
                }
            }

            return null;
        }
    }
}
