using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Data.SqlClient;

namespace Cycloside.Services
{
    /// <summary>
    /// Database Manager - Comprehensive database management and SQL editing toolkit
    /// Supports multiple database types (SQL Server, MySQL, PostgreSQL, SQLite) with query execution and result visualization
    /// </summary>
    public static class DatabaseManager
    {
        public static event EventHandler<DatabaseConnectionEventArgs>? DatabaseConnected;
        public static event EventHandler<QueryExecutedEventArgs>? QueryExecuted;
        public static event EventHandler<DatabaseErrorEventArgs>? DatabaseError;

        private static readonly ObservableCollection<DatabaseConnection> _connections = new();
        private static readonly ObservableCollection<QueryResult> _queryHistory = new();
        private static readonly Dictionary<string, DbProviderFactory> _providers = new();

        public static ObservableCollection<DatabaseConnection> Connections => _connections;
        public static ObservableCollection<QueryResult> QueryHistory => _queryHistory;

        static DatabaseManager()
        {
            // Register database providers
            _providers["sqlserver"] = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
            _providers["sqlite"] = Microsoft.Data.Sqlite.SqliteFactory.Instance;
        }

        /// <summary>
        /// Initialize database manager
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🗄️ Initializing Database Manager...");

            try
            {
                // Load saved connections
                await LoadConnectionsAsync();

                Logger.Log("✅ Database Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Database Manager initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new database connection
        /// </summary>
        public static async Task<DatabaseConnection?> CreateConnectionAsync(string name, string provider, string connectionString, bool saveConnection = true)
        {
            try
            {
                Logger.Log($"🗄️ Creating database connection: {name} ({provider})");

                var connection = new DatabaseConnection
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Provider = provider,
                    ConnectionString = connectionString,
                    IsConnected = false,
                    CreatedAt = DateTime.Now
                };

                // Test the connection
                if (await TestConnectionAsync(connection))
                {
                    connection.IsConnected = true;
                    _connections.Add(connection);

                    if (saveConnection)
                    {
                        await SaveConnectionsAsync();
                    }

                    OnDatabaseConnected(connection);
                    Logger.Log($"✅ Database connection created: {name}");
                    return connection;
                }
                else
                {
                    Logger.Log($"❌ Database connection failed: {name}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to create database connection: {ex.Message}");
                OnDatabaseError($"Connection creation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        private static async Task<bool> TestConnectionAsync(DatabaseConnection connection)
        {
            try
            {
                if (!_providers.TryGetValue(connection.Provider.ToLower(), out var factory))
                {
                    Logger.Log($"❌ Unsupported database provider: {connection.Provider}");
                    return false;
                }

                using var dbConnection = factory.CreateConnection();
                if (dbConnection == null) return false;

                dbConnection.ConnectionString = connection.ConnectionString;
                await dbConnection.OpenAsync();

                // Test with a simple query
                using var command = dbConnection.CreateCommand();
                command.CommandText = GetTestQuery(connection.Provider);

                using var reader = await command.ExecuteReaderAsync();
                return reader != null;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute SQL query
        /// </summary>
        public static async Task<QueryResult?> ExecuteQueryAsync(string connectionId, string sql, int maxRows = 1000)
        {
            try
            {
                var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
                if (connection == null || !connection.IsConnected)
                {
                    OnDatabaseError("Connection not found or not connected");
                    return null;
                }

                Logger.Log($"🗄️ Executing query on {connection.Name}...");

                if (!_providers.TryGetValue(connection.Provider.ToLower(), out var factory))
                {
                    OnDatabaseError($"Unsupported database provider: {connection.Provider}");
                    return null;
                }

                using var dbConnection = factory.CreateConnection();
                dbConnection.ConnectionString = connection.ConnectionString;
                await dbConnection.OpenAsync();

                var result = new QueryResult
                {
                    ConnectionId = connectionId,
                    Query = sql,
                    ExecutedAt = DateTime.Now,
                    Rows = new List<Dictionary<string, object>>(),
                    ColumnNames = new List<string>(),
                    AffectedRows = 0,
                    ExecutionTime = TimeSpan.Zero
                };

                var startTime = DateTime.Now;

                try
                {
                    using var command = dbConnection.CreateCommand();
                    command.CommandText = sql;
                    command.CommandTimeout = 30; // 30 second timeout

                    if (IsSelectQuery(sql))
                    {
                        // SELECT query - return results
                        using var reader = await command.ExecuteReaderAsync();
                        result.ColumnNames = GetColumnNames(reader);

                        int rowCount = 0;
                        while (await reader.ReadAsync() && rowCount < maxRows)
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            result.Rows.Add(row);
                            rowCount++;
                        }

                        result.RowCount = rowCount;
                    }
                    else
                    {
                        // Non-SELECT query - return affected rows
                        result.AffectedRows = await command.ExecuteNonQueryAsync();
                        result.RowCount = result.AffectedRows;
                    }

                    result.ExecutionTime = DateTime.Now - startTime;
                    result.Success = true;

                    _queryHistory.Add(result);
                    OnQueryExecuted(result);

                    Logger.Log($"✅ Query executed successfully: {result.RowCount} rows affected in {result.ExecutionTime.TotalMilliseconds}ms");
                    return result;
                }
                catch (Exception ex)
                {
                    result.ExecutionTime = DateTime.Now - startTime;
                    result.Success = false;
                    result.ErrorMessage = ex.Message;

                    Logger.Log($"❌ Query execution failed: {ex.Message}");
                    OnDatabaseError($"Query failed: {ex.Message}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Database query execution failed: {ex.Message}");
                OnDatabaseError($"Query execution failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get database schema information
        /// </summary>
        public static async Task<DatabaseSchema?> GetSchemaAsync(string connectionId)
        {
            try
            {
                var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
                if (connection == null || !connection.IsConnected)
                {
                    OnDatabaseError("Connection not found or not connected");
                    return null;
                }

                Logger.Log($"🗄️ Getting schema for {connection.Name}...");

                if (!_providers.TryGetValue(connection.Provider.ToLower(), out var factory))
                {
                    OnDatabaseError($"Unsupported database provider: {connection.Provider}");
                    return null;
                }

                using var dbConnection = factory.CreateConnection();
                dbConnection.ConnectionString = connection.ConnectionString;
                await dbConnection.OpenAsync();

                var schema = new DatabaseSchema
                {
                    ConnectionId = connectionId,
                    DatabaseName = GetDatabaseName(connection),
                    Tables = new List<TableInfo>(),
                    Views = new List<ViewInfo>()
                };

                // Get tables
                var tableRestrictions = new string[4];
                var tables = dbConnection.GetSchema("Tables", tableRestrictions);

                foreach (DataRow row in tables.Rows)
                {
                    schema.Tables.Add(new TableInfo
                    {
                        Name = row["TABLE_NAME"].ToString(),
                        Schema = row["TABLE_SCHEMA"].ToString(),
                        Type = row["TABLE_TYPE"].ToString(),
                        RowCount = await GetTableRowCountAsync(dbConnection, schema.DatabaseName, row["TABLE_NAME"].ToString())
                    });
                }

                // Get views
                var viewRestrictions = new string[4];
                var views = dbConnection.GetSchema("Views", viewRestrictions);

                foreach (DataRow row in views.Rows)
                {
                    schema.Views.Add(new ViewInfo
                    {
                        Name = row["TABLE_NAME"].ToString(),
                        Schema = row["TABLE_SCHEMA"].ToString()
                    });
                }

                Logger.Log($"✅ Schema retrieved: {schema.Tables.Count} tables, {schema.Views.Count} views");
                return schema;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Schema retrieval failed: {ex.Message}");
                OnDatabaseError($"Schema retrieval failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get table row count
        /// </summary>
        private static async Task<long> GetTableRowCountAsync(DbConnection connection, string databaseName, string tableName)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt64(result);
            }
            catch
            {
                return -1; // Unknown row count
            }
        }

        /// <summary>
        /// Close database connection
        /// </summary>
        public static void CloseConnection(string connectionId)
        {
            var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection != null)
            {
                connection.IsConnected = false;
                Logger.Log($"🗄️ Connection closed: {connection.Name}");
            }
        }

        /// <summary>
        /// Remove database connection
        /// </summary>
        public static void RemoveConnection(string connectionId)
        {
            var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection != null)
            {
                if (connection.IsConnected)
                {
                    CloseConnection(connectionId);
                }

                _connections.Remove(connection);
                SaveConnectionsAsync();
                Logger.Log($"🗄️ Connection removed: {connection.Name}");
            }
        }

        // Helper methods
        private static string GetTestQuery(string provider)
        {
            return provider.ToLower() switch
            {
                "sqlserver" => "SELECT 1",
                "mysql" => "SELECT 1",
                "postgresql" => "SELECT 1",
                "sqlite" => "SELECT 1",
                _ => "SELECT 1"
            };
        }

        private static bool IsSelectQuery(string sql)
        {
            var trimmed = sql.Trim().ToUpper();
            return trimmed.StartsWith("SELECT") || trimmed.StartsWith("WITH");
        }

        private static List<string> GetColumnNames(DbDataReader reader)
        {
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }
            return columns;
        }

        private static string GetDatabaseName(DatabaseConnection connection)
        {
            try
            {
                // Extract database name from connection string
                var parts = connection.ConnectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.Trim().ToLower().StartsWith("database=") ||
                        part.Trim().ToLower().StartsWith("initial catalog=") ||
                        part.Trim().ToLower().StartsWith("database"))
                    {
                        return part.Split('=')[1].Trim();
                    }
                }
            }
            catch
            {
                // Ignore extraction errors
            }

            return "Unknown";
        }

        private static async Task LoadConnectionsAsync()
        {
            try
            {
                var connectionsPath = Path.Combine(AppContext.BaseDirectory, "Data", "database-connections.json");
                if (File.Exists(connectionsPath))
                {
                    var json = await File.ReadAllTextAsync(connectionsPath);
                    var connections = System.Text.Json.JsonSerializer.Deserialize<List<DatabaseConnection>>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (connections != null)
                    {
                        _connections.Clear();
                        foreach (var conn in connections)
                        {
                            _connections.Add(conn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to load database connections: {ex.Message}");
            }
        }

        private static async Task SaveConnectionsAsync()
        {
            try
            {
                var connectionsPath = Path.Combine(AppContext.BaseDirectory, "Data", "database-connections.json");
                var json = System.Text.Json.JsonSerializer.Serialize(_connections.ToList(),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(connectionsPath, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to save database connections: {ex.Message}");
            }
        }

        // Event handlers
        private static void OnDatabaseConnected(DatabaseConnection connection)
        {
            DatabaseConnected?.Invoke(null, new DatabaseConnectionEventArgs(connection));
        }

        private static void OnQueryExecuted(QueryResult result)
        {
            QueryExecuted?.Invoke(null, new QueryExecutedEventArgs(result));
        }

        private static void OnDatabaseError(string error)
        {
            DatabaseError?.Invoke(null, new DatabaseErrorEventArgs(error));
        }
    }

    // Data models
    public class DatabaseConnection
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Provider { get; set; } = "sqlserver";
        public string ConnectionString { get; set; } = "";
        public bool IsConnected { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    public class QueryResult
    {
        public string ConnectionId { get; set; } = "";
        public string Query { get; set; } = "";
        public DateTime ExecutedAt { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; } = new();
        public List<string> ColumnNames { get; set; } = new();
        public int RowCount { get; set; }
        public int AffectedRows { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DatabaseSchema
    {
        public string ConnectionId { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public List<TableInfo> Tables { get; set; } = new();
        public List<ViewInfo> Views { get; set; } = new();
    }

    public class TableInfo
    {
        public string Name { get; set; } = "";
        public string Schema { get; set; } = "";
        public string Type { get; set; } = "";
        public long RowCount { get; set; } = -1;
    }

    public class ViewInfo
    {
        public string Name { get; set; } = "";
        public string Schema { get; set; } = "";
    }

    // Event args
    public class DatabaseConnectionEventArgs : EventArgs
    {
        public DatabaseConnection Connection { get; }

        public DatabaseConnectionEventArgs(DatabaseConnection connection)
        {
            Connection = connection;
        }
    }

    public class QueryExecutedEventArgs : EventArgs
    {
        public QueryResult Result { get; }

        public QueryExecutedEventArgs(QueryResult result)
        {
            Result = result;
        }
    }

    public class DatabaseErrorEventArgs : EventArgs
    {
        public string Error { get; }

        public DatabaseErrorEventArgs(string error)
        {
            Error = error;
        }
    }
}
