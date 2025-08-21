// Assets/Scripts/Content/ContentManager.cs
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] public class RootIndex { public int schemaVersion; public string updatedAt; public SubjectIndex[] subjects; }
[Serializable] public class SubjectIndex { public string id; public string title; public int order; }

public class ContentManager : MonoBehaviour
{
    public static ContentManager Instance { get; private set; }
    public RootIndex Index { get; private set; }
    public bool IsReady => _isReady;
    public string BaseUrl = "https://firebasestorage.googleapis.com/v0/b/first-5b828.firebasestorage.app/o/";
    [TextArea] public string IndexUrlEncoded = "content%2Findex.json?alt=media&token=850dfd3f-a4fb-4aa7-8550-29d13fe087af";

    private bool _isReady;
    private string LocalIndexPath => Path.Combine(Application.persistentDataPath, "content/index.json");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Path.GetDirectoryName(LocalIndexPath));
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // 1) грузим из кэша, если есть
        if (File.Exists(LocalIndexPath))
        {
            try
            {
                Index = JsonUtility.FromJson<RootIndex>(File.ReadAllText(LocalIndexPath));
                _isReady = true;
            }
            catch (Exception e) { Debug.LogWarning("[Content] Cache parse failed: " + e.Message); }
        }

        // 2) параллельно т€нем свежий
        string url = BaseUrl + IndexUrlEncoded;
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var json = req.downloadHandler.text;
                var fresh = JsonUtility.FromJson<RootIndex>(json);

                // если нет кэша или updatedAt изменилс€ Ч обновл€ем
                if (Index == null || fresh.updatedAt != Index.updatedAt)
                {
                    Index = fresh;
                    File.WriteAllText(LocalIndexPath, json, Encoding.UTF8);
                    Debug.Log("[Content] Index updated from remote.");
                }
                _isReady = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Content] Remote JSON parse error: " + e.Message);
                // если кэш уже был Ч работаем с ним; если нет Ч остаЄмс€ без данных
                _isReady = Index != null;
            }
        }
        else
        {
            Debug.LogWarning("[Content] Remote load failed: " + req.error);
            _isReady = Index != null; // офлайн Ч работаем с кэшем
        }
    }
}
