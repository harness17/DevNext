using Microsoft.Playwright;

namespace Dev.CommonLibrary.Pdf;

/// <summary>
/// HTML を Chromium の印刷機能で PDF に変換するサービス。
/// </summary>
public class PlaywrightPdfService
{
    /// <summary>
    /// 印刷用 HTML を PDF バイト列に変換する。
    /// </summary>
    public async Task<byte[]> GenerateFromHtmlAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            PreferCSSPageSize = true
        });
    }
}
