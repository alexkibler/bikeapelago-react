using Bikeapelago.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Services;

public class SchemaDiscoveryService(BikeapelagoDbContext context) : ISchemaDiscoveryService
{
    private readonly BikeapelagoDbContext _context = context;

    public List<TableGroup> GetSchema()
    {
        return GetCoreSchema();
    }

    private List<TableGroup> GetCoreSchema()
    {
        var entityTypes = _context.Model.GetEntityTypes();
        var allTables = new List<TableMetadata>();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName() ?? entityType.GetViewName() ?? entityType.DisplayName();
            var columns = entityType.GetProperties().Select(p => new ColumnMetadata
            {
                Name = GetJsonPropertyName(p.PropertyInfo!),
                Type = MapToFrontendType(p.ClrType),
                IsNullable = p.IsNullable,
                IsPrimaryKey = p.IsPrimaryKey(),
                IsSpatial = typeof(Geometry).IsAssignableFrom(p.ClrType)
            }).ToList();

            columns = columns.Where(c => !IsSensitiveField(c.Name)).ToList();
            allTables.Add(new TableMetadata { Table = tableName, Columns = columns });
        }

        return new List<TableGroup>
        {
            new TableGroup 
            { 
                Name = "Identity", 
                Tables = allTables.Where(t => t.Table.StartsWith("AspNet")).OrderBy(t => t.Table).ToList() 
            },
            new TableGroup 
            { 
                Name = "Game Core", 
                Tables = allTables.Where(t => !t.Table.StartsWith("AspNet")).OrderBy(t => t.Table).ToList() 
            }
        };
    }

    private static string MapToFrontendType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string)) return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal)) return "number";
        if (underlyingType == typeof(bool)) return "boolean";
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset)) return "date";
        if (underlyingType == typeof(Guid)) return "uuid";
        if (typeof(Geometry).IsAssignableFrom(underlyingType)) return "geometry";

        return "string"; // Default
    }

    private static bool IsSensitiveField(string name)
    {
        string[] sensitive = ["PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Password"];
        return sensitive.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetJsonPropertyName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (attribute != null) return attribute.Name;

        // Default to camelCase if no attribute
        var name = property.Name;
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0])) return name;
        return char.ToLower(name[0]) + name.Substring(1);
    }
}

public class TableGroup
{
    public string Name { get; set; } = string.Empty;
    public List<TableMetadata> Tables { get; set; } = [];
}

public class TableMetadata
{
    public string Table { get; set; } = string.Empty;
    public List<ColumnMetadata> Columns { get; set; } = [];
}

public class ColumnMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsSpatial { get; set; }
}
