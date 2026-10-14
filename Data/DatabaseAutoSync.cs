using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Backend.Data;

/// <summary>
/// Sincronizador automático de modelos con la base de datos SQL Server.
/// Detecta tablas y columnas nuevas definidas en C# y las crea en la base de datos al iniciar la aplicación.
/// </summary>
public static class DatabaseAutoSync
{
    public static async Task SynchronizeAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            // 1. Obtener todas las tablas y columnas existentes actualmente en SQL Server
            var existingTablesAndColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT TABLE_NAME, COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'dbo';";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var table = reader.GetString(0);
                    var column = reader.GetString(1);

                    if (!existingTablesAndColumns.ContainsKey(table))
                    {
                        existingTablesAndColumns[table] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                    existingTablesAndColumns[table].Add(column);
                }
            }

            // 2. Recorrer todas las entidades mapeadas en EF Core
            var entityTypes = context.Model.GetEntityTypes();

            int tablasCreadas = 0;
            int columnasAgregadas = 0;

            foreach (var entity in entityTypes)
            {
                var tableName = entity.GetTableName();
                if (string.IsNullOrEmpty(tableName)) continue;

                // CASO A: La tabla completa no existe en SQL Server -> Crear Tabla
                if (!existingTablesAndColumns.ContainsKey(tableName))
                {
                    await CreateTableAsync(entity, tableName, connection);
                    tablasCreadas++;

                    existingTablesAndColumns[tableName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in entity.GetProperties())
                    {
                        var colName = prop.GetColumnName();
                        if (!string.IsNullOrEmpty(colName))
                        {
                            existingTablesAndColumns[tableName].Add(colName);
                        }
                    }
                    continue;
                }

                // CASO B: La tabla ya existe -> Verificar si hay columnas nuevas
                var existingColumns = existingTablesAndColumns[tableName];
                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    if (string.IsNullOrEmpty(columnName)) continue;

                    if (!existingColumns.Contains(columnName))
                    {
                        await AddColumnAsync(property, tableName, columnName, connection);
                        existingColumns.Add(columnName);
                        columnasAgregadas++;
                    }
                }
            }

            if (tablasCreadas > 0 || columnasAgregadas > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"--> [AutoSync] Sincronización completada: {tablasCreadas} tabla(s) nueva(s), {columnasAgregadas} columna(s) nueva(s) agregada(s).");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "--> [AutoSync] Error durante la sincronización automática de esquema.");
        }
    }

    private static async Task CreateTableAsync(IEntityType entity, string tableName, DbConnection connection)
    {
        var primaryKey = entity.FindPrimaryKey();
        var pkProperties = primaryKey?.Properties ?? Array.Empty<IProperty>();
        var pkColumnNames = pkProperties.Select(p => $"[{p.GetColumnName()}]").ToList();

        var columnDefinitions = new List<string>();

        foreach (var property in entity.GetProperties())
        {
            var colName = property.GetColumnName();
            if (string.IsNullOrEmpty(colName)) continue;

            var sqlType = GetSqlType(property);
            var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            var isPk = pkProperties.Contains(property);
            var isIdentity = property.ValueGenerated == ValueGenerated.OnAdd && 
                             (clrType == typeof(int) || clrType == typeof(long));

            var identityClause = isIdentity ? "IDENTITY(1,1)" : "";
            var nullableClause = (!isPk && property.IsNullable) ? "NULL" : "NOT NULL";

            columnDefinitions.Add($"[{colName}] {sqlType} {identityClause} {nullableClause}".Replace("  ", " ").Trim());
        }

        if (pkColumnNames.Any())
        {
            columnDefinitions.Add($"CONSTRAINT [PK_{tableName}] PRIMARY KEY CLUSTERED ({string.Join(", ", pkColumnNames)})");
        }

        var sql = $"CREATE TABLE [dbo].[{tableName}] (\n  " + string.Join(",\n  ", columnDefinitions) + "\n);";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"--> [AutoSync] Nueva tabla '[dbo].[{tableName}]' creada automáticamente en Azure SQL.");
        Console.ResetColor();
    }

    private static async Task AddColumnAsync(IProperty property, string tableName, string columnName, DbConnection connection)
    {
        var sqlType = GetSqlType(property);

        // Al agregar a tabla con datos existentes, siempre se añade NULL para evitar error en registros previos
        var sql = $"ALTER TABLE [dbo].[{tableName}] ADD [{columnName}] {sqlType} NULL;";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"--> [AutoSync] Nueva columna '[{columnName}]' ({sqlType}) agregada a tabla '[dbo].[{tableName}]'.");
        Console.ResetColor();
    }

    private static string GetSqlType(IProperty property)
    {
        var explicitType = property.GetColumnType();
        if (!string.IsNullOrWhiteSpace(explicitType))
        {
            return explicitType;
        }

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

        if (clrType.IsEnum) return "INT";
        if (clrType == typeof(int)) return "INT";
        if (clrType == typeof(long)) return "BIGINT";
        if (clrType == typeof(short)) return "SMALLINT";
        if (clrType == typeof(byte)) return "TINYINT";
        if (clrType == typeof(bool)) return "BIT";
        if (clrType == typeof(string))
        {
            var maxLen = property.GetMaxLength();
            return maxLen.HasValue ? $"NVARCHAR({maxLen.Value})" : "NVARCHAR(MAX)";
        }
        if (clrType == typeof(decimal))
        {
            var precision = property.GetPrecision() ?? 18;
            var scale = property.GetScale() ?? 2;
            return $"DECIMAL({precision},{scale})";
        }
        if (clrType == typeof(double) || clrType == typeof(float)) return "FLOAT";
        if (clrType == typeof(DateTime)) return "DATETIME2";
        if (clrType == typeof(DateOnly)) return "DATE";
        if (clrType == typeof(TimeOnly) || clrType == typeof(TimeSpan)) return "TIME";
        if (clrType == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (clrType == typeof(byte[])) return "VARBINARY(MAX)";

        return "NVARCHAR(MAX)";
    }
}
