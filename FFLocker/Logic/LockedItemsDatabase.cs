using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FFLocker.Logic
{
    public class LockedItemInfo
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string LockedPath { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
    }

    public static class LockedItemsDatabase
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "locked_items.db");
        private static readonly string KeyPath = Path.Combine(AppContext.BaseDirectory, "db.key");
        private static string? _dbKey;

        static LockedItemsDatabase()
        {
            SQLitePCL.Batteries.Init();
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher());
        }

        private static string GetOrCreateKey()
        {
            if (_dbKey != null) return _dbKey;

            try
            {
                if (File.Exists(KeyPath))
                {
                    var encryptedKey = File.ReadAllBytes(KeyPath);
                    var keyBytes = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
                    _dbKey = Convert.ToBase64String(keyBytes);
                }
                else
                {
                    var keyBytes = new byte[32];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(keyBytes);
                    }
                    var encryptedKey = ProtectedData.Protect(keyBytes, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(KeyPath, encryptedKey);
                    _dbKey = Convert.ToBase64String(keyBytes);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., logging)
                throw new Exception("Failed to get or create database key.", ex);
            }
            
            return _dbKey;
        }

        private static SqliteConnection GetConnection()
        {
            var key = GetOrCreateKey();
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                var keyBytes = Convert.FromBase64String(key);
                command.CommandText = $"PRAGMA KEY = \"x'{ToHex(keyBytes)}'\"";
                command.ExecuteNonQuery();
            }

            InitializeDatabase(connection);
            return connection;
        }

        private static void InitializeDatabase(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS LockedItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OriginalPath TEXT NOT NULL UNIQUE,
                        LockedPath TEXT NOT NULL,
                        IsFolder INTEGER NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        public static void Add(LockedItemInfo item)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT OR REPLACE INTO LockedItems (OriginalPath, LockedPath, IsFolder) VALUES (@OriginalPath, @LockedPath, @IsFolder);";
                command.Parameters.AddWithValue("@OriginalPath", item.OriginalPath);
                command.Parameters.AddWithValue("@LockedPath", item.LockedPath);
                command.Parameters.AddWithValue("@IsFolder", item.IsFolder ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        public static void Remove(string originalPath)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM LockedItems WHERE OriginalPath = @OriginalPath;";
                command.Parameters.AddWithValue("@OriginalPath", originalPath);
                command.ExecuteNonQuery();
            }
        }

        public static List<LockedItemInfo> GetLockedItems()
        {
            var items = new List<LockedItemInfo>();
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OriginalPath, LockedPath, IsFolder FROM LockedItems;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new LockedItemInfo
                        {
                            OriginalPath = reader.GetString(0),
                            LockedPath = reader.GetString(1),
                            IsFolder = reader.GetInt32(2) == 1
                        });
                    }
                }
            }
            return items;
        }
        
        public static void Reload()
        {
            // This method was part of the old implementation.
            // With SQLite, data is read on-demand, so this can be a no-op.
        }

        private static string ToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}