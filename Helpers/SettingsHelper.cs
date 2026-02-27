using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Blog_Manager.Helpers
{
    /// <summary>
    /// 基于 JSON 文件的设置存储，替代 ApplicationData.Current.LocalSettings
    /// 存储路径: %APPDATA%/BlogManager/settings.json
    /// </summary>
    public static class SettingsHelper
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlogManager");

        private static readonly string SettingsFile =
            Path.Combine(SettingsDir, "settings.json");

        private static Dictionary<string, object?> _cache = new();
        private static readonly object _lock = new();
        private static bool _loaded = false;

        /// <summary>
        /// 获取设置值
        /// </summary>
        public static object? GetValue(string key)
        {
            EnsureLoaded();
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    // JValue 需要转为 .NET 基础类型
                    if (value is JValue jv)
                        return jv.Value;
                    return value;
                }
                return null;
            }
        }

        /// <summary>
        /// 获取字符串类型设置值
        /// </summary>
        public static string? GetString(string key)
        {
            return GetValue(key)?.ToString();
        }

        /// <summary>
        /// 尝试获取设置值
        /// </summary>
        public static bool TryGetValue(string key, out object? value)
        {
            EnsureLoaded();
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var raw))
                {
                    value = raw is JValue jv ? jv.Value : raw;
                    return true;
                }
                value = null;
                return false;
            }
        }

        /// <summary>
        /// 设置值
        /// </summary>
        public static void SetValue(string key, object? value)
        {
            EnsureLoaded();
            lock (_lock)
            {
                _cache[key] = value;
                Save();
            }
        }

        /// <summary>
        /// 移除设置项
        /// </summary>
        public static void RemoveValue(string key)
        {
            EnsureLoaded();
            lock (_lock)
            {
                if (_cache.Remove(key))
                {
                    Save();
                }
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (_lock)
            {
                if (_loaded) return;
                Load();
                _loaded = true;
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    _cache = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json)
                             ?? new Dictionary<string, object?>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsHelper.Load failed: {ex.Message}");
                _cache = new Dictionary<string, object?>();
            }
        }

        private static void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsHelper.Save failed: {ex.Message}");
            }
        }
    }
}
