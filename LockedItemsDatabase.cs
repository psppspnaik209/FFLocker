using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FFLocker
{
    public class LockedItemInfo
    {
        public string? OriginalPath { get; set; }
        public string? LockedPath { get; set; }
        public bool IsFolder { get; set; }
    }

    public static class LockedItemsDatabase
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locked_items.json");
        private static readonly string ErrorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fflocker_db_error.log");
        private static List<LockedItemInfo> _lockedItems = new List<LockedItemInfo>();

        static LockedItemsDatabase()
        {
            Load();
        }

        public static List<LockedItemInfo> GetLockedItems()
        {
            return new List<LockedItemInfo>(_lockedItems);
        }

        public static void Reload()
        {
            Load();
        }

        public static void Add(LockedItemInfo item)
        {
            if (!_lockedItems.Any(i => i.OriginalPath == item.OriginalPath))
            {
                _lockedItems.Add(item);
                Save();
            }
        }

        public static void Remove(string originalPath)
        {
            var itemToRemove = _lockedItems.FirstOrDefault(i => i.OriginalPath == originalPath);
            if (itemToRemove != null)
            {
                _lockedItems.Remove(itemToRemove);
                Save();
            }
        }

        public static LockedItemInfo? GetLockedItemByOriginalPath(string originalPath)
        {
            return _lockedItems.FirstOrDefault(i => i.OriginalPath == originalPath);
        }

        private static void Load()
        {
            if (!File.Exists(DbPath))
            {
                _lockedItems = new List<LockedItemInfo>();
                return;
            }

            var json = File.ReadAllText(DbPath);

            try
            {
                _lockedItems = JsonSerializer.Deserialize<List<LockedItemInfo>>(json) ?? new List<LockedItemInfo>();
            }
            catch (JsonException)
            {
                try
                {
                    var oldItems = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    _lockedItems = oldItems.Select(path => new LockedItemInfo { OriginalPath = path, LockedPath = path, IsFolder = Directory.Exists(path) }).ToList();
                    Save(); 
                }
                catch (JsonException ex)
                {
                    _lockedItems = new List<LockedItemInfo>();
                    try
                    {
                        File.Delete(DbPath);
                    }
                    catch (Exception deleteEx)
                    {
                        LogSaveError(deleteEx);
                    }
                    LogSaveError(ex);
                }
            }
        }

        private static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_lockedItems, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DbPath, json);
            }
            catch (Exception ex)
            {
                LogSaveError(ex);
            }
        }

        private static void LogSaveError(Exception ex)
        {
            try
            {
                File.AppendAllText(ErrorLogPath, $"{DateTime.UtcNow}: {ex}{Environment.NewLine}");
            }
            catch
            {
                // If we can't even write to the log, there's not much else to do.
            }
        }
    }
}
