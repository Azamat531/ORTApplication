//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;

//public class PrefetchController : MonoBehaviour
//{
//    [Header("Verbose logs")] public bool verbose = false;

//    // ==== Массовый префетч (как было) ====
//    public IEnumerator PrefetchAllSubjects(List<SubjectData> subjects)
//    {
//        foreach (var s in subjects)
//        {
//            string topicsRel = $"{s.id}/topics.json";
//            string topicsUrl = StoragePaths.Content(topicsRel);

//            string topicsJson = null;
//            yield return CacheService.GetText(
//                topicsUrl,
//                "json:" + StoragePaths.ContentRoot + "/" + topicsRel,
//                text => topicsJson = text,
//                err => Debug.LogWarning("[Prefetch] topics.json error: " + err)
//            );
//            if (string.IsNullOrEmpty(topicsJson)) continue;

//            var topics = JsonFlex.ParseTopics(topicsJson) ?? new List<TopicData>();
//            foreach (var t in topics)
//            {
//                string subRel = $"{s.id}/{t.id}/subtopics.json";
//                string subUrl = StoragePaths.Content(subRel);

//                string subsJson = null;
//                yield return CacheService.GetText(
//                    subUrl,
//                    "json:" + StoragePaths.ContentRoot + "/" + subRel,
//                    text => subsJson = text,
//                    err => Debug.LogWarning("[Prefetch] subtopics.json error: " + err)
//                );
//                if (string.IsNullOrEmpty(subsJson)) continue;

//                var subtopics = JsonFlex.ParseSubtopics(subsJson) ?? new List<SubtopicIndex>();
//                foreach (var st in subtopics)
//                {
//                    string v1Url = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/v1");
//                    string v2Url = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/v2");

//                    yield return CacheService.GetFile(v1Url, cacheKey: "video:" + v1Url, onDone: _ => { }, forcedExt: ".mp4");
//                    yield return CacheService.GetFile(v2Url, cacheKey: "video:" + v2Url, onDone: _ => { }, forcedExt: ".mp4");

//                    int qCount = st.answers != null ? st.answers.Count : 0;
//                    for (int i = 0; i < qCount; i++)
//                    {
//                        string qUrl = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/q{i + 1}");
//                        yield return CacheService.GetTextureToCache(qUrl, cacheKey: "img:" + qUrl, onDone: _ => { });
//                    }
//                }
//            }
//        }
//    }

//    // ==== Плавный прогресс по БАЙТАМ для одной подтемы ====
//    [Serializable] private class SubtopicRow { public string id; public string title; public List<string> answers; }
//    [Serializable] private class SubtopicsRoot { public List<SubtopicRow> subtopics; }

//    private struct Asset
//    {
//        public string url;       // без расширения (как в проекте)
//        public string cacheKey;  // "video:..." или "img:..."
//        public string ext;       // ".mp4" / ".png"
//        public bool isImage;
//        public long sizeBytes; // из HEAD (если 0 — поставим дефолт)
//    }

//    public IEnumerator PrefetchSubtopicCoursesProgress(
//        string subjectId,
//        string topicId,
//        string subtopicId,
//        Action<float> onProgress,      // 0..1
//        Action<bool> onDone = null    // success
//    )
//    {
//        bool ok = true;

//        // 1) Узнаём кол-во вопросов (для списка картинок)
//        string subsRel = $"{subjectId}/{topicId}/subtopics.json";
//        string subsUrl = StoragePaths.Content(subsRel);
//        string subsText = null;
//        yield return CacheService.GetText(
//            subsUrl,
//            "json:" + StoragePaths.ContentRoot + "/" + subsRel,
//            t => subsText = t,
//            _ => subsText = null
//        );

//        int qCount = 0;
//        if (!string.IsNullOrEmpty(subsText))
//        {
//            try
//            {
//                var root = JsonUtility.FromJson<SubtopicsRoot>(subsText);
//                if (root?.subtopics != null)
//                {
//                    foreach (var st in root.subtopics)
//                        if (st != null && st.id == subtopicId)
//                        { qCount = st.answers != null ? st.answers.Count : 0; break; }
//                }
//            }
//            catch { }
//        }

//        // 2) Составляем список недостающих ассетов
//        var assets = new List<Asset>();

//        string v1Url = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/v1");
//        if (string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v1Url, ".mp4")))
//            assets.Add(new Asset { url = v1Url, cacheKey = "video:" + v1Url, ext = ".mp4", isImage = false });

//        string v2Url = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/v2");
//        if (string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v2Url, ".mp4")))
//            assets.Add(new Asset { url = v2Url, cacheKey = "video:" + v2Url, ext = ".mp4", isImage = false });

//        for (int i = 0; i < qCount; i++)
//        {
//            string qUrl = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/q{i + 1}");
//            if (string.IsNullOrEmpty(CacheService.GetCachedPath("img:" + qUrl, ".png")))
//                assets.Add(new Asset { url = qUrl, cacheKey = "img:" + qUrl, ext = ".png", isImage = true });
//        }

//        if (assets.Count == 0) { onProgress?.Invoke(1f); onDone?.Invoke(true); yield break; }

//        // 3) HEAD — узнаём размеры в байтах (если сервер не даёт — подставим оценки)
//        for (int i = 0; i < assets.Count; i++)
//        {
//            long len = 0;
//            yield return HeadContentLength(WithExt(assets[i].url, assets[i].ext), v => len = v);

//            if (len <= 0)
//            {
//                // приблизительные веса: видео 25 МБ, картинка 200 КБ (подбери под свои данные)
//                len = assets[i].isImage ? 200L * 1024L : 25L * 1024L * 1024L;
//            }

//            var a = assets[i];
//            a.sizeBytes = len;
//            assets[i] = a;
//        }

//        long totalBytes = 0;
//        foreach (var a in assets) totalBytes += a.sizeBytes;
//        long doneBytes = 0;

//        onProgress?.Invoke(0f);

//        // 4) Качаем последовательно, обновляя прогресс по байтам
//        foreach (var a in assets)
//        {
//            if (a.isImage)
//            {
//                bool okImg = false;
//                yield return CacheService.GetTextureToCache(a.url, a.cacheKey, o => okImg = o);
//                ok &= okImg;
//                doneBytes += a.sizeBytes;
//                onProgress?.Invoke(Mathf.Clamp01((float)doneBytes / totalBytes));
//            }
//            else
//            {
//                bool okVid = false;
//                yield return CacheService.GetFile(
//                    a.url,
//                    a.cacheKey,
//                    _ => { okVid = true; },
//                    forcedExt: a.ext,
//                    onError: _ => { okVid = false; },
//                    onProgress: p =>
//                    {
//                        // p = 0..1 для текущего файла → переводим в байты
//                        long curBytes = (long)(Mathf.Clamp01(p) * a.sizeBytes);
//                        float totalP = (float)(doneBytes + curBytes) / totalBytes;
//                        onProgress?.Invoke(Mathf.Clamp01(totalP));
//                    }
//                );
//                ok &= okVid;
//                doneBytes += a.sizeBytes;
//                onProgress?.Invoke(Mathf.Clamp01((float)doneBytes / totalBytes));
//            }
//        }

//        onProgress?.Invoke(1f);
//        onDone?.Invoke(ok);
//    }

//    // ==== HELPERS ====

//    // HEAD-запрос: вернуть Content-Length (байты). 0 — если нет заголовка.
//    private IEnumerator HeadContentLength(string urlWithExt, Action<long> onResult)
//    {
//        using (var req = UnityWebRequest.Head(urlWithExt))
//        {
//            yield return req.SendWebRequest();
//            if (!RequestSucceeded(req)) { onResult?.Invoke(0); yield break; }

//            string len = req.GetResponseHeader("Content-Length");
//            if (long.TryParse(len, out long v) && v > 0) onResult?.Invoke(v);
//            else onResult?.Invoke(0);
//        }
//    }

//    private string WithExt(string urlMaybeNoExt, string ext)
//    {
//        if (string.IsNullOrEmpty(ext) || ext.StartsWith(".") == false) ext = "." + ext;
//        int q = urlMaybeNoExt.IndexOf('?');
//        string baseUrl = (q >= 0) ? urlMaybeNoExt.Substring(0, q) : urlMaybeNoExt;
//        string query = (q >= 0) ? urlMaybeNoExt.Substring(q) : "";
//        if (System.IO.Path.GetExtension(baseUrl).Length == 0) return baseUrl + ext + query;
//        return urlMaybeNoExt;
//    }

//    private bool RequestSucceeded(UnityWebRequest req)
//    {
//#if UNITY_2020_2_OR_NEWER
//        return req.result == UnityWebRequest.Result.Success;
//#else
//        return !req.isNetworkError && !req.isHttpError;
//#endif
//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PrefetchController : MonoBehaviour
{
    public static PrefetchController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [Header("Verbose logs")] public bool verbose = false;

    // ==== Массовый префетч (как было) ====
    public IEnumerator PrefetchAllSubjects(List<SubjectData> subjects)
    {
        foreach (var s in subjects)
        {
            string topicsRel = $"{s.id}/topics.json";
            string topicsUrl = StoragePaths.Content(topicsRel);

            string topicsJson = null;
            yield return CacheService.GetText(
                topicsUrl,
                "json:" + StoragePaths.ContentRoot + "/" + topicsRel,
                text => topicsJson = text,
                err => Debug.LogWarning("[Prefetch] topics.json error: " + err)
            );
            if (string.IsNullOrEmpty(topicsJson)) continue;

            var topics = JsonFlex.ParseTopics(topicsJson) ?? new List<TopicData>();
            foreach (var t in topics)
            {
                string subRel = $"{s.id}/{t.id}/subtopics.json";
                string subUrl = StoragePaths.Content(subRel);

                string subsJson = null;
                yield return CacheService.GetText(
                    subUrl,
                    "json:" + StoragePaths.ContentRoot + "/" + subRel,
                    text => subsJson = text,
                    err => Debug.LogWarning("[Prefetch] subtopics.json error: " + err)
                );
                if (string.IsNullOrEmpty(subsJson)) continue;

                var subtopics = JsonFlex.ParseSubtopics(subsJson) ?? new List<SubtopicIndex>();
                foreach (var st in subtopics)
                {
                    string v1Url = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/v1");
                    string v2Url = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/v2");

                    yield return CacheService.GetFile(v1Url, cacheKey: "video:" + v1Url, onDone: _ => { }, forcedExt: ".mp4");
                    yield return CacheService.GetFile(v2Url, cacheKey: "video:" + v2Url, onDone: _ => { }, forcedExt: ".mp4");

                    int qCount = st.answers != null ? st.answers.Count : 0;
                    for (int i = 0; i < qCount; i++)
                    {
                        string qUrl = StoragePaths.Content($"{s.id}/{t.id}/{st.id}/q{i + 1}");
                        yield return CacheService.GetTextureToCache(qUrl, cacheKey: "img:" + qUrl, onDone: _ => { });
                    }
                }
            }
        }
    }

    // ==== Плавный прогресс по БАЙТАМ для одной подтемы ====
    [Serializable] private class SubtopicRow { public string id; public string title; public List<string> answers; }
    [Serializable] private class SubtopicsRoot { public List<SubtopicRow> subtopics; }

    private struct Asset
    {
        public string url;
        public string cacheKey;
        public string ext;
        public bool isImage;
        public long sizeBytes;
    }

    public IEnumerator PrefetchSubtopicCoursesProgress(
        string subjectId,
        string topicId,
        string subtopicId,
        Action<float> onProgress,
        Action<bool> onDone = null
    )
    {
        bool ok = true;

        string subsRel = $"{subjectId}/{topicId}/subtopics.json";
        string subsUrl = StoragePaths.Content(subsRel);
        string subsText = null;
        yield return CacheService.GetText(
            subsUrl,
            "json:" + StoragePaths.ContentRoot + "/" + subsRel,
            t => subsText = t,
            _ => subsText = null
        );

        int qCount = 0;
        if (!string.IsNullOrEmpty(subsText))
        {
            try
            {
                var root = JsonUtility.FromJson<SubtopicsRoot>(subsText);
                if (root?.subtopics != null)
                {
                    foreach (var st in root.subtopics)
                        if (st != null && st.id == subtopicId)
                        { qCount = st.answers != null ? st.answers.Count : 0; break; }
                }
            }
            catch { }
        }

        var assets = new List<Asset>();

        string v1Url = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/v1");
        if (string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v1Url, ".mp4")))
            assets.Add(new Asset { url = v1Url, cacheKey = "video:" + v1Url, ext = ".mp4", isImage = false });

        string v2Url = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/v2");
        if (string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v2Url, ".mp4")))
            assets.Add(new Asset { url = v2Url, cacheKey = "video:" + v2Url, ext = ".mp4", isImage = false });

        for (int i = 0; i < qCount; i++)
        {
            string qUrl = StoragePaths.Content($"{subjectId}/{topicId}/{subtopicId}/q{i + 1}");
            if (string.IsNullOrEmpty(CacheService.GetCachedPath("img:" + qUrl, ".png")))
                assets.Add(new Asset { url = qUrl, cacheKey = "img:" + qUrl, ext = ".png", isImage = true });
        }

        if (assets.Count == 0) { onProgress?.Invoke(1f); onDone?.Invoke(true); yield break; }

        for (int i = 0; i < assets.Count; i++)
        {
            long len = 0;
            yield return HeadContentLength(WithExt(assets[i].url, assets[i].ext), v => len = v);

            if (len <= 0)
                len = assets[i].isImage ? 200L * 1024L : 25L * 1024L * 1024L;

            var a = assets[i];
            a.sizeBytes = len;
            assets[i] = a;
        }

        long totalBytes = 0;
        foreach (var a in assets) totalBytes += a.sizeBytes;
        long doneBytes = 0;

        onProgress?.Invoke(0f);

        foreach (var a in assets)
        {
            if (a.isImage)
            {
                bool okImg = false;
                yield return CacheService.GetTextureToCache(a.url, a.cacheKey, o => okImg = o);
                ok &= okImg;
                doneBytes += a.sizeBytes;
                onProgress?.Invoke(Mathf.Clamp01((float)doneBytes / totalBytes));
            }
            else
            {
                bool okVid = false;
                yield return CacheService.GetFile(
                    a.url,
                    a.cacheKey,
                    _ => { okVid = true; },
                    forcedExt: a.ext,
                    onError: _ => { okVid = false; },
                    onProgress: p =>
                    {
                        long curBytes = (long)(Mathf.Clamp01(p) * a.sizeBytes);
                        float totalP = (float)(doneBytes + curBytes) / totalBytes;
                        onProgress?.Invoke(Mathf.Clamp01(totalP));
                    }
                );
                ok &= okVid;
                doneBytes += a.sizeBytes;
                onProgress?.Invoke(Mathf.Clamp01((float)doneBytes / totalBytes));
            }
        }

        onProgress?.Invoke(1f);
        onDone?.Invoke(ok);
    }

    // ==== HELPERS ====

    private IEnumerator HeadContentLength(string urlWithExt, Action<long> onResult)
    {
        using (var req = UnityWebRequest.Head(urlWithExt))
        {
            yield return req.SendWebRequest();
            if (!RequestSucceeded(req)) { onResult?.Invoke(0); yield break; }

            string len = req.GetResponseHeader("Content-Length");
            if (long.TryParse(len, out long v) && v > 0) onResult?.Invoke(v);
            else onResult?.Invoke(0);
        }
    }

    private string WithExt(string urlMaybeNoExt, string ext)
    {
        if (string.IsNullOrEmpty(ext) || ext.StartsWith(".") == false) ext = "." + ext;
        int q = urlMaybeNoExt.IndexOf('?');
        string baseUrl = (q >= 0) ? urlMaybeNoExt.Substring(0, q) : urlMaybeNoExt;
        string query = (q >= 0) ? urlMaybeNoExt.Substring(q) : "";
        if (System.IO.Path.GetExtension(baseUrl).Length == 0) return baseUrl + ext + query;
        return urlMaybeNoExt;
    }

    private bool RequestSucceeded(UnityWebRequest req)
    {
#if UNITY_2020_2_OR_NEWER
        return req.result == UnityWebRequest.Result.Success;
#else
        return !req.isNetworkError && !req.isHttpError;
#endif
    }

    // ==== ДОБАВЛЕННЫЙ МЕТОД ====

    public IEnumerator PrefetchPracticeJsonOnly()
    {
        string subjectsRel = "subjects.json";
        string subjectsUrl = StoragePaths.Practise(subjectsRel);
        string subjectsKey = "json:" + StoragePaths.PractiseRoot + "/" + subjectsRel;

        string subjectsRaw = null;
        bool subjOk = false;

        yield return CacheService.GetText(
            subjectsUrl, subjectsKey,
            t => { subjectsRaw = t; subjOk = true; },
            e => { subjOk = false; }
        );

        if (!subjOk || string.IsNullOrEmpty(subjectsRaw))
        {
            Debug.LogWarning("[PrefetchController] Не удалось получить subjects.json");
            yield break;
        }

        var subjects = JsonFlex.ParseSubjects(subjectsRaw) ?? new List<SubjectData>();
        foreach (var s in subjects)
        {
            string topicsRel = $"{s.id}/topics.json";
            string topicsUrl = StoragePaths.Practise(topicsRel);
            string topicsKey = "json:" + StoragePaths.PractiseRoot + "/" + topicsRel;

            string topicsRaw = null;
            bool topOk = false;

            yield return CacheService.GetText(
                topicsUrl, topicsKey,
                t => { topicsRaw = t; topOk = true; },
                e => { topOk = false; }
            );

            if (!topOk || string.IsNullOrEmpty(topicsRaw)) continue;

            var topics = JsonFlex.ParseTopics(topicsRaw) ?? new List<TopicData>();
            foreach (var t in topics)
            {
                string ansRel = $"{s.id}/{t.id}/answers.json";
                string ansUrl = StoragePaths.Practise(ansRel);
                string ansKey = "json:" + StoragePaths.PractiseRoot + "/" + ansRel;

                yield return CacheService.GetText(
                    ansUrl, ansKey,
                    _ => { },
                    _ => { }
                );
            }
        }

        Debug.Log("[PrefetchController] PrefetchPracticeJsonOnly завершён");
    }
}
