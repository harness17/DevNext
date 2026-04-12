using ApiSample.Entity;
using ApiSample.Models;
using ApiSample.Repository;

namespace ApiSample.Service;

/// <summary>
/// 商品 CRUD のビジネスロジック層。
/// エンティティ ↔ レスポンスモデルの変換もここで行う。
/// </summary>
public class ApiItemService(ApiItemRepository repository)
{
    private readonly ApiItemRepository _repository = repository;

    /// <summary>全件取得してレスポンスモデルに変換</summary>
    public async Task<List<ApiItemResponse>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(ToResponse).ToList();
    }

    /// <summary>ID で1件取得。存在しない場合は null を返す</summary>
    public async Task<ApiItemResponse?> GetByIdAsync(long id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? null : ToResponse(item);
    }

    /// <summary>リクエストモデルから登録して結果を返す</summary>
    public async Task<ApiItemResponse> CreateAsync(ApiItemRequest request)
    {
        var item = new ApiItem
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
        };
        var created = await _repository.CreateAsync(item);
        return ToResponse(created);
    }

    /// <summary>既存エンティティをリクエスト内容で更新して返す。存在しない場合は null</summary>
    public async Task<ApiItemResponse?> UpdateAsync(long id, ApiItemRequest request)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.Stock = request.Stock;

        var updated = await _repository.UpdateAsync(item);
        return ToResponse(updated);
    }

    /// <summary>削除。存在しない場合は false を返す</summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;

        await _repository.DeleteAsync(item);
        return true;
    }

    // エンティティ → レスポンスモデル変換
    private static ApiItemResponse ToResponse(ApiItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        Stock = item.Stock,
        CreateDate = item.CreateDate,
        UpdateDate = item.UpdateDate,
    };
}
