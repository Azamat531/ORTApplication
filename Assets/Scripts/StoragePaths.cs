// ===============================
// StoragePaths.cs � centralized Firebase Storage URL builder
// ===============================
using UnityEngine;
using UnityEngine.Networking;

public static class StoragePaths
{
    // Defaults � ����� �������� � �������� ����� SetConfig(...)
    public static string BucketHost { get; private set; } = "first-5b828.firebasestorage.app";
    public static string ContentRoot { get; private set; } = "content";   // �����
    public static string PractiseRoot { get; private set; } = "practise"; // �������� (��������� ��� �� ������)

    /// <summary>�������������� ������ (��������, �� ScriptableObject � ������ �����).</summary>
    public static void SetConfig(string bucketHost, string contentRoot = "content", string practiseRoot = "practise")
    {
        if (!string.IsNullOrEmpty(bucketHost)) BucketHost = bucketHost;
        if (!string.IsNullOrEmpty(contentRoot)) ContentRoot = contentRoot;
        if (!string.IsNullOrEmpty(practiseRoot)) PractiseRoot = practiseRoot;
    }

    /// <summary>������� ���������� URL � ����� � Firebase Storage �� �������������� ����.</summary>
    public static string Build(string relative)
    {
        string encoded = UnityWebRequest.EscapeURL(relative).Replace("+", "%20");
        return $"https://firebasestorage.googleapis.com/v0/b/{BucketHost}/o/{encoded}?alt=media";
    }

    // �����������
    public static string Content(string rel) => Build($"{ContentRoot}/{rel}");
    public static string Practise(string rel) => Build($"{PractiseRoot}/{rel}");
}
