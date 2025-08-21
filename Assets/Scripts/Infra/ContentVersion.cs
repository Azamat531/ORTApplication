// ============================================
// File: Assets/Scripts/Infra/ContentVersion.cs
// Purpose: ���������� ������ �������� �� ���� "version" � subjects.json
//  - Extract(text)        -> ����������� ������ (string/number) �� JSON
//  - GetSaved()/Save(v)   -> �������� ��������� ������ �� ����������
//  - ShouldPrefetch(newV) -> true, ���� ������ ���������� (��� ��� ����������)
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
    /// �������� �������� ������ �� subjects.json.
    /// ������������ ��� ��������:
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

        // ���� ������ ��� � ������ �� ������ (�������, ��� ���������� �� �����������)
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
    /// true -> ������ ���������� (��� ������ �� ���������), ����� ��������� �������.
    /// </summary>
    public static bool ShouldPrefetch(string newVersion)
    {
        if (string.IsNullOrEmpty(newVersion)) return false; // ���� ���� ��� � ������ �� ������
        var saved = GetSaved();
        if (string.IsNullOrEmpty(saved)) return true;       // ������ ������ ����� ������ �������
        return !string.Equals(saved, newVersion, StringComparison.Ordinal);
    }
}
