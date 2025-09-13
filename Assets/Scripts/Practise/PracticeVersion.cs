//using System;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.Networking;

//public static class PracticeVersion
//{
//    // Было private — сделал public, чтобы использовать ключ извне при необходимости.
//    public const string PrefKey = "practice_content_version_v1";

//    /// <summary>
//    /// Вернуть локально сохранённую версию контента практики.
//    /// </summary>
//    public static int GetLocalVersion()
//    {
//        return PlayerPrefs.GetInt(PrefKey, 0);
//    }

//    /// <summary>
//    /// Обновляет локальную версию, если на сервере выше, и затем вызывает after().
//    /// Логику очистки кэша оставляю прежней (как у тебя): очистка зашифрованного кэша и temp.
//    /// </summary>
//    public static IEnumerator RefreshAndThen(Action after)
//    {
//        string url = StoragePaths.Practise("subjects.json");

//        int serverVer = -1;
//        yield return FetchServerVersionNoCache(url, v => serverVer = v);

//        int localVer = GetLocalVersion();
//        if (serverVer <= 0)
//        {
//            Debug.LogWarning($"[PracticeVersion] Can't fetch remote version. Keep local={localVer}");
//        }
//        else
//        {
//            if (serverVer > localVer)
//            {
//                Debug.Log($"[PracticeVersion] New {serverVer} > {localVer}. Clearing cache...");
//                PlayerPrefs.SetInt(PrefKey, serverVer);
//                PlayerPrefs.Save();

//                // Сохраняю твою прежнюю очистку: вероятно чистит зашифрованный кэш и временные файлы.
//                CacheService.ClearAllEncrypted();
//                CacheService.CleanupTemp(0);
//            }
//            else
//            {
//                Debug.Log($"[PracticeVersion] Up-to-date. server={serverVer}, local={localVer}");
//            }
//        }

//        after?.Invoke();
//    }

//    /// <summary>
//    /// Тянем subjects.json без кэша и вытаскиваем "version".
//    /// </summary>
//    private static IEnumerator FetchServerVersionNoCache(string url, Action<int> onDone)
//    {
//        using (var req = UnityWebRequest.Get(url))
//        {
//            req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
//            req.SetRequestHeader("Pragma", "no-cache");
//            req.SetRequestHeader("Expires", "0");

//            yield return req.SendWebRequest();

//#if UNITY_2020_3_OR_NEWER
//            bool ok = req.result == UnityWebRequest.Result.Success;
//#else
//            bool ok = !req.isNetworkError && !req.isHttpError;
//#endif
//            if (!ok)
//            {
//                Debug.LogWarning($"[PracticeVersion] fetch failed: {req.error} url={url}");
//                onDone?.Invoke(0);
//                yield break;
//            }

//            var json = req.downloadHandler.text;
//            int v = ExtractVersion(json);
//            onDone?.Invoke(v);
//        }
//    }

//    /// <summary>
//    /// Очень простой парсер версии из JSON по ключу "version".
//    /// </summary>
//    private static int ExtractVersion(string json)
//    {
//        if (string.IsNullOrEmpty(json)) return 0;
//        int i = json.IndexOf("\"version\"", StringComparison.OrdinalIgnoreCase);
//        if (i < 0) return 0;
//        int colon = json.IndexOf(':', i);
//        if (colon < 0) return 0;
//        int j = colon + 1;
//        while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
//        int start = j;
//        while (j < json.Length && char.IsDigit(json[j])) j++;
//        return int.TryParse(json.Substring(start, j - start), out var v) ? v : 0;
//    }
//}

using System;
using System.Collections;
using UnityEngine;

public static class PracticeVersion
{
    private const string PrefKey = "practice_content_version_v1";

    public static int GetLocalVersion()
    {
        return PlayerPrefs.GetInt(PrefKey, 0);
    }

    public static void SetLocalVersion(int v)
    {
        PlayerPrefs.SetInt(PrefKey, v);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Проверяет subjects.json онлайн. Если версия выросла — обновляет и запускает префетч.
    /// После завершения вызывает onReady().
    /// </summary>
    public static IEnumerator RefreshAndThen(Action onReady)
    {
        // оффлайн — сразу готово
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onReady?.Invoke();
            yield break;
        }

        string rel = "subjects.json";
        string url = StoragePaths.Practise(rel);
        string key = "json:" + StoragePaths.PractiseRoot + "/" + rel;

        string json = null;
        bool ok = false;

        // Берём свежак (с no-cache)
        yield return CacheService.GetText(
            url, key,
            t => { json = t; ok = true; },
            e => { ok = false; }
        );

        if (ok && !string.IsNullOrEmpty(json))
        {
            int remoteVer = ExtractVersion(json);
            int localVer = GetLocalVersion();
            if (remoteVer > localVer)
            {
                Debug.Log($"[PracticeVersion] Новая версия {remoteVer} (старая {localVer}), обновляем…");
                SetLocalVersion(remoteVer);

                // Запуск префетча JSON по всем темам/вопросам
                if (PrefetchController.Instance != null)
                {
                    yield return PrefetchController.Instance.PrefetchPracticeJsonOnly();
                }
            }
        }

        onReady?.Invoke();
    }

    private static int ExtractVersion(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        if (json.Contains("\"version\""))
        {
            int i = json.IndexOf("\"version\"");
            if (i >= 0)
            {
                int c = json.IndexOf(':', i);
                if (c > 0)
                {
                    string sub = json.Substring(c + 1);
                    string num = "";
                    foreach (char ch in sub)
                    {
                        if (char.IsDigit(ch)) num += ch;
                        else if (num.Length > 0) break;
                    }
                    if (int.TryParse(num, out int v)) return v;
                }
            }
        }
        return 0;
    }
}
