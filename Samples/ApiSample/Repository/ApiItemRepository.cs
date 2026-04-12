using ApiSample.Data;
using ApiSample.Entity;
using Microsoft.EntityFrameworkCore;

namespace ApiSample.Repository;

/// <summary>
/// 商品エンティティのリポジトリ。
/// CRUD 操作を DB アクセスから分離する。
/// </summary>
public class ApiItemRepository(ApiSampleDbContext context)
{
    private readonly ApiSampleDbContext _context = context;

    /// <summary>全件取得</summary>
    public async Task<List<ApiItem>> GetAllAsync()
        => await _context.ApiItems.OrderBy(x => x.Id).ToListAsync();

    /// <summary>ID で1件取得。存在しない場合は null を返す</summary>
    public async Task<ApiItem?> GetByIdAsync(long id)
        => await _context.ApiItems.FindAsync(id);

    /// <summary>登録（監査カラムを SetForCreate で設定する）</summary>
    public async Task<ApiItem> CreateAsync(ApiItem item)
    {
        item.SetForCreate();
        _context.ApiItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    /// <summary>更新（監査カラムを SetForUpdate で設定する）</summary>
    public async Task<ApiItem> UpdateAsync(ApiItem item)
    {
        item.SetForUpdate();
        _context.ApiItems.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    /// <summary>削除</summary>
    public async Task DeleteAsync(ApiItem item)
    {
        _context.ApiItems.Remove(item);
        await _context.SaveChangesAsync();
    }
}
