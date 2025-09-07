// ===============================
// StoragePaths.cs Ч centralized Firebase Storage URL builder
// ===============================
using UnityEngine;
using UnityEngine.Networking;

public static class StoragePaths
{
    // Defaults Ч можно помен€ть в рантайме через SetConfig(...)
    public static string BucketHost { get; private set; } = "first-5b828.firebasestorage.app";
    public static string ContentRoot { get; private set; } = "content";   // курсы
    public static string PractiseRoot { get; private set; } = "practise"; // практика (оставлено как на бакете)

    /// <summary>ѕереопределить конфиг (например, из ScriptableObject в начале сцены).</summary>
    public static void SetConfig(string bucketHost, string contentRoot = "content", string practiseRoot = "practise")
    {
        if (!string.IsNullOrEmpty(bucketHost)) BucketHost = bucketHost;
        if (!string.IsNullOrEmpty(contentRoot)) ContentRoot = contentRoot;
        if (!string.IsNullOrEmpty(practiseRoot)) PractiseRoot = practiseRoot;
    }

    /// <summary>—обрать абсолютный URL к файлу в Firebase Storage по относительному пути.</summary>
    public static string Build(string relative)
    {
        string encoded = UnityWebRequest.EscapeURL(relative).Replace("+", "%20");
        return $"https://firebasestorage.googleapis.com/v0/b/{BucketHost}/o/{encoded}?alt=media";
    }

    // –азделители
    public static string Content(string rel) => Build($"{ContentRoot}/{rel}");
    public static string Practise(string rel) => Build($"{PractiseRoot}/{rel}");
}
