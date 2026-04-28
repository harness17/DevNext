using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiClientSample.Models;

namespace ApiClientSample.Services;

/// <summary>ApiSample REST API を呼び出すクライアントサービス</summary>
public class ApiSampleClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // ─────────────────────────────────────────────────────────────
    // 認証
    // ─────────────────────────────────────────────────────────────

    /// <summary>JWT トークンとロール一覧を取得する。認証失敗時は null を返す。</summary>
    public async Task<LoginResult?> LoginAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try { response = await httpClient.PostAsync("/api/auth/login", content); }
        catch (HttpRequestException) { return null; }

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var token = root.TryGetProperty("token", out var t) ? t.GetString() : null;
        if (token is null) return null;

        var roles = new List<string>();
        if (root.TryGetProperty("roles", out var rolesEl))
            foreach (var r in rolesEl.EnumerateArray())
                if (r.GetString() is string role) roles.Add(role);

        return new LoginResult(token, roles);
    }

    // ─────────────────────────────────────────────────────────────
    // 商品 CRUD
    // ─────────────────────────────────────────────────────────────

    /// <summary>商品一覧を取得する。</summary>
    public async Task<List<ItemViewModel>?> GetItemsAsync(string token)
    {
        using var req = BuildRequest(HttpMethod.Get, "/api/items", token);
        var (ok, json) = await SendAsync(req);
        return ok ? JsonSerializer.Deserialize<List<ItemViewModel>>(json!, _json) : null;
    }

    /// <summary>商品詳細を取得する。</summary>
    public async Task<ItemViewModel?> GetItemAsync(long id, string token)
    {
        using var req = BuildRequest(HttpMethod.Get, $"/api/items/{id}", token);
        var (ok, json) = await SendAsync(req);
        return ok ? JsonSerializer.Deserialize<ItemViewModel>(json!, _json) : null;
    }

    /// <summary>商品を登録する（Admin のみ）。成功時は登録した商品を返す。</summary>
    public async Task<ItemViewModel?> CreateItemAsync(ItemFormViewModel form, string token)
    {
        using var req = BuildRequest(HttpMethod.Post, "/api/items", token, form);
        var (ok, json) = await SendAsync(req);
        return ok ? JsonSerializer.Deserialize<ItemViewModel>(json!, _json) : null;
    }

    /// <summary>商品を更新する（Admin のみ）。成功時は更新後の商品を返す。</summary>
    public async Task<ItemViewModel?> UpdateItemAsync(long id, ItemFormViewModel form, string token)
    {
        using var req = BuildRequest(HttpMethod.Put, $"/api/items/{id}", token, form);
        var (ok, json) = await SendAsync(req);
        return ok ? JsonSerializer.Deserialize<ItemViewModel>(json!, _json) : null;
    }

    /// <summary>商品を削除する（Admin のみ）。成功時は true を返す。</summary>
    public async Task<bool> DeleteItemAsync(long id, string token)
    {
        using var req = BuildRequest(HttpMethod.Delete, $"/api/items/{id}", token);
        var (ok, _) = await SendAsync(req);
        return ok;
    }

    // ─────────────────────────────────────────────────────────────
    // 内部ヘルパー
    // ─────────────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return req;
    }

    private async Task<(bool Ok, string? Json)> SendAsync(HttpRequestMessage req)
    {
        try
        {
            var res = await httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode) return (false, null);
            return (true, await res.Content.ReadAsStringAsync());
        }
        catch (HttpRequestException) { return (false, null); }
    }
}
