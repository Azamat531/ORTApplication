// ============================================
// File: Assets/Scripts/Infra/ContentVersion.cs
// Purpose: Глобальная версия контента по полю "version" в subjects.json
//  - Extract(text)        -> вытаскивает версию (string/number) из JSON
//  - GetSaved()/Save(v)   -> хранение последней версии на устройстве
//  - ShouldPrefetch(newV) -> true, если версии отличаются (или нет сохранённой)
// ============================================
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ContentVersion
{
    private static readonly string VersionPath =
        Path.Combine(Application.persistentDataPath, "cache", "content_version.txt");

    /// <summary>
    /// Пытается вытащить версию из subjects.json.
    /// Поддерживает два варианта:
    ///   "version": "2025-08-16"
    ///   "version": 42
    /// </summary>
    public static string Extract(string subjectsJson)
    {
        if (string.IsNullOrEmpty(subjectsJson)) return null;

        // "version": "..."
        var m = Regex.Match(subjectsJson,
            @"\""version\""\s*:\s*""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.Trim();

        // "version": 123
        var m2 = Regex.Match(subjectsJson,
            @"\""version\""\s*:\s*(\d+)",
            RegexOptions.IgnoreCase);
        if (m2.Success) return m2.Groups[1].Value.Trim();

        // Поля версии нет — ничего не делаем (считаем, что обновления не отслеживаем)
        return null;
    }

    public static string GetSaved()
    {
        try
        {
            return File.Exists(VersionPath) ? File.ReadAllText(VersionPath).Trim() : null;
        }
        catch { return null; }
    }

    public static void Save(string v)
    {
        if (string.IsNullOrEmpty(v)) return;
        try
        {
            var dir = Path.GetDirectoryName(VersionPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(VersionPath, v);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// true -> версии отличаются (или раньше не сохраняли), стоит запустить префетч.
    /// </summary>
    public static bool ShouldPrefetch(string newVersion)
    {
        if (string.IsNullOrEmpty(newVersion)) return false; // если поля нет — ничего не делаем
        var saved = GetSaved();
        if (string.IsNullOrEmpty(saved)) return true;       // первый запуск после полной закачки
        return !string.Equals(saved, newVersion, StringComparison.Ordinal);
    }
}
