using ApiSample.Models;
using ApiSample.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiSample.Controllers;

/// <summary>
/// 商品 REST API コントローラー。
/// 一覧・詳細取得は認証済みユーザー（Admin/Member）が利用可能。
/// 登録・更新・削除は Admin ロールのみ実行可能。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // すべてのアクションに認証必須（401 Unauthorized）
public class ItemsController(ApiItemService service) : ControllerBase
{
    private readonly ApiItemService _service = service;

    // ─────────────────────────────────────────────────────────────
    // GET /api/items
    // ─────────────────────────────────────────────────────────────
    /// <summary>商品一覧を取得する</summary>
    /// <returns>商品一覧（認証済みユーザーのみ）</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ApiItemResponse>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /api/items/{id}
    // ─────────────────────────────────────────────────────────────
    /// <summary>指定 ID の商品を取得する</summary>
    /// <param name="id">商品 ID</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiItemResponse>> GetById(long id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { message = $"ID={id} の商品が見つかりません。" });
        return Ok(item);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /api/items
    // ─────────────────────────────────────────────────────────────
    /// <summary>商品を登録する（Admin のみ）</summary>
    /// <param name="request">登録内容</param>
    [HttpPost]
    [Authorize(Roles = "Admin")] // Admin のみ（Member は 403 Forbidden）
    [ProducesResponseType(typeof(ApiItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiItemResponse>> Create([FromBody] ApiItemRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ─────────────────────────────────────────────────────────────
    // PUT /api/items/{id}
    // ─────────────────────────────────────────────────────────────
    /// <summary>商品を更新する（Admin のみ）</summary>
    /// <param name="id">商品 ID</param>
    /// <param name="request">更新内容</param>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiItemResponse>> Update(long id, [FromBody] ApiItemRequest request)
    {
        var updated = await _service.UpdateAsync(id, request);
        if (updated is null) return NotFound(new { message = $"ID={id} の商品が見つかりません。" });
        return Ok(updated);
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE /api/items/{id}
    // ─────────────────────────────────────────────────────────────
    /// <summary>商品を削除する（Admin のみ）</summary>
    /// <param name="id">商品 ID</param>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = $"ID={id} の商品が見つかりません。" });
        return NoContent();
    }
}
