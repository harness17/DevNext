using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiClientSample.Models;

namespace ApiClientSample.Services;

/// <summary>ApiSample REST API を呼び出すクライアントサービス</summary>
public class ApiSampleClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>JWT トークンを取得する。認証失敗時は null を返す。</summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("/api/auth/login", content);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var tokenEl) ? tokenEl.GetString() : null;
    }

    /// <summary>商品一覧を取得する。取得失敗時は null を返す。</summary>
    public async Task<List<ItemViewModel>?> GetItemsAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ItemViewModel>>(json, _json);
    }
}
