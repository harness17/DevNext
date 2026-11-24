using Microsoft.AspNetCore.Mvc;

namespace Site.Common
{
    /// <summary>
    /// 検証・モデルバインドのエラーメッセージを日本語化する MvcOptions 拡張
    /// </summary>
    public static class ValidationMessageSetup
    {
        /// <summary>
        /// DataAnnotations の既定メッセージとモデルバインド失敗時のメッセージを日本語に差し替える
        /// </summary>
        public static MvcOptions UseJapaneseValidationMessages(this MvcOptions options)
        {
            // 末尾に追加するのは、DataAnnotations プロバイダーが暗黙の [Required] を付与した後に処理するため
            options.ModelMetadataDetailsProviders.Add(new JapaneseValidationMetadataProvider());

            // 型変換に失敗した入力（数値欄に文字など）は DataAnnotations を通らず、ここの文言が使われる
            var provider = options.ModelBindingMessageProvider;
            provider.SetAttemptedValueIsInvalidAccessor((value, field) => $"{field}の値「{value}」は正しくありません。");
            provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"値「{value}」は正しくありません。");
            provider.SetUnknownValueIsInvalidAccessor(field => $"{field}の値が正しくありません。");
            provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "値が正しくありません。");
            provider.SetValueIsInvalidAccessor(value => $"値「{value}」は正しくありません。");
            provider.SetValueMustBeANumberAccessor(field => $"{field}は数値で入力してください。");
            provider.SetNonPropertyValueMustBeANumberAccessor(() => "数値で入力してください。");
            provider.SetValueMustNotBeNullAccessor(value => $"{value}は必須です。");
            provider.SetMissingBindRequiredValueAccessor(field => $"{field}は必須です。");
            provider.SetMissingKeyOrValueAccessor(() => "値は必須です。");
            provider.SetMissingRequestBodyRequiredValueAccessor(() => "リクエスト本文は必須です。");

            return options;
        }
    }
}
