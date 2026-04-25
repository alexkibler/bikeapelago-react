using Bikeapelago.Api.Authorization;
using Bikeapelago.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bikeapelago.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/data")]
[AdminAuthorize]
public class GenericDataController(BikeapelagoDbContext context) : ControllerBase
{
    private readonly BikeapelagoDbContext _context = context;

    [HttpGet("{tableName}")]
    public async Task<IActionResult> GetList(string tableName, [FromQuery] int page = 1, [FromQuery] int perPage = 30)
    {
        if (page < 1)
            return BadRequest(new { message = "page must be at least 1." });

        if (perPage < 1 || perPage > 200)
            return BadRequest(new { message = "perPage must be between 1 and 200." });

        var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
        if (entityType == null) return NotFound(new { message = $"Table {tableName} not found." });

        var clrType = entityType.ClrType;
        var setMethod = typeof(DbContext).GetMethods()
            .FirstOrDefault(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0);
        
        var dbSet = setMethod?.MakeGenericMethod(clrType).Invoke(_context, null) as IQueryable;
        if (dbSet == null) return BadRequest();

        // Count total
        var countMethod = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .FirstOrDefault(m => m.Name == "CountAsync" && m.IsGenericMethod && m.GetParameters().Length == 2);
        
        var total = await (Task<int>)countMethod!.MakeGenericMethod(clrType).Invoke(null, [dbSet, null])!;

        // Page data
        var skipMethod = typeof(Queryable).GetMethods()
            .FirstOrDefault(m => m.Name == "Skip" && m.IsGenericMethod && m.GetParameters().Length == 2);
        var takeMethod = typeof(Queryable).GetMethods()
            .FirstOrDefault(m => m.Name == "Take" && m.IsGenericMethod && m.GetParameters().Length == 2);

        var pagedSet = skipMethod!.MakeGenericMethod(clrType).Invoke(null, [dbSet, (page - 1) * perPage]) as IQueryable;
        pagedSet = takeMethod!.MakeGenericMethod(clrType).Invoke(null, [pagedSet, perPage]) as IQueryable;

        var toListMethod = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .FirstOrDefault(m => m.Name == "ToListAsync" && m.IsGenericMethod && m.GetParameters().Length == 2);
        
        var toListTask = (Task)toListMethod!.MakeGenericMethod(clrType).Invoke(null, [pagedSet, null])!;
        await toListTask;
        var itemsProperty = toListTask.GetType().GetProperty("Result");
        var itemsList = itemsProperty?.GetValue(toListTask);

        return Ok(new
        {
            page,
            perPage,
            totalItems = total,
            totalPages = (int)Math.Ceiling((double)total / perPage),
            items = itemsList
        });
    }

    [HttpGet("{tableName}/{id}")]
    public async Task<IActionResult> GetOne(string tableName, string id)
    {
        var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
        if (entityType == null) return NotFound();

        var clrType = entityType.ClrType;
        
        // Try to parse ID (Guid or Long)
        object? parsedId = id;
        if (Guid.TryParse(id, out var guidId)) parsedId = guidId;
        else if (long.TryParse(id, out var longId)) parsedId = longId;

        var item = await _context.FindAsync(clrType, parsedId);
        if (item == null) return NotFound();

        return Ok(item);
    }

    [HttpPost("{tableName}")]
    public async Task<IActionResult> Create(string tableName, [FromBody] JsonElement body)
    {
        var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
        if (entityType == null) return NotFound();

        var clrType = entityType.ClrType;
        var item = JsonSerializer.Deserialize(body.GetRawText(), clrType);
        if (item == null) return BadRequest();

        _context.Add(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPut("{tableName}/{id}")]
    public async Task<IActionResult> Update(string tableName, string id, [FromBody] JsonElement body)
    {
        var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
        if (entityType == null) return NotFound();

        var clrType = entityType.ClrType;
        object? parsedId = id;
        if (Guid.TryParse(id, out var guidId)) parsedId = guidId;
        else if (long.TryParse(id, out var longId)) parsedId = longId;

        var existing = await _context.FindAsync(clrType, parsedId);
        if (existing == null) return NotFound();

        // Basic property merging
        // In a real app we'd use something more robust than reflection but for admin it's fine
        var updated = JsonSerializer.Deserialize(body.GetRawText(), clrType);
        if (updated == null) return BadRequest();

        _context.Entry(existing).CurrentValues.SetValues(updated);
        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{tableName}/{id}")]
    public async Task<IActionResult> Delete(string tableName, string id)
    {
        var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
        if (entityType == null) return NotFound();

        var clrType = entityType.ClrType;
        object? parsedId = id;
        if (Guid.TryParse(id, out var guidId)) parsedId = guidId;
        else if (long.TryParse(id, out var longId)) parsedId = longId;

        var item = await _context.FindAsync(clrType, parsedId);
        if (item == null) return NotFound();

        _context.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
