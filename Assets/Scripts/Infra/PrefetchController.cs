//// ============================================
//// File: Assets/Scripts/Infra/PrefetchController.cs
//// Purpose: Sequential prefetch for offline (subjects → topics → subtopics → v1/v2/q1..qN)
//// Changes (2025-08-25):
////  • Add _running guard to avoid double runs
////  • Add autoRunOnStart flag (default: true)
////  • Use CacheService.GetTextureToCache() (no Sprite allocations)
//// ============================================
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;

//public class PrefetchController : MonoBehaviour
//{
//    [Header("Storage (Firebase)")]
//    [Tooltip("Хост бакета Firebase Storage (без https и /o)")]
//    public string bucketHost = "first-5b828.firebasestorage.app";
//    [Tooltip("Корневая папка с контентом в бакете")] public string contentRoot = "content";

//    [Header("Behavior")]
//    public bool autoRunOnStart = true;
//    public bool verbose = false;
//    public int maxImageSide = 2048;

//    // --- Back-compat aliases for old code ---
//    public string bucketName { get => bucketHost; set => bucketHost = value; }
//    public string contentFolder { get => contentRoot; set => contentRoot = value; }

//    private bool _running;

//    void Start()
//    {
//        if (autoRunOnStart) StartCoroutine(RunPrefetch());
//    }

//    // Back-compat: старый вызов StartCoroutine(prefetch.PrefetchAllSubjects(...))
//    public IEnumerator PrefetchAllSubjects(System.Action onDone = null)
//    {
//        yield return RunPrefetch();
//        onDone?.Invoke();
//    }
//    public IEnumerator PrefetchAllSubjects(object _) { yield return RunPrefetch(); }

//    // ===== Основной префетч =====
//    public IEnumerator RunPrefetch()
//    {
//        if (_running) { if (verbose) Debug.Log("[Prefetch] already running"); yield break; }
//        if (Application.internetReachability == NetworkReachability.NotReachable)
//        { if (verbose) Debug.Log("[Prefetch] offline → skip"); yield break; }
//        _running = true;

//        // 1) subjects.json
//        string subjectsRel = $"{contentRoot}/subjects.json";
//        string subjectsUrl = BuildStorageUrl(subjectsRel);

//        string subjectsText = null; bool done = false;
//        yield return CacheService.GetText(
//            subjectsUrl,
//            cacheKey: "json:" + subjectsRel,
//            onDone: t => { subjectsText = t; done = true; },
//            onError: e => { if (verbose) Debug.LogWarning("[Prefetch] subjects.json: " + e); done = true; }
//        );
//        if (!done) yield return null;
//        if (string.IsNullOrEmpty(subjectsText)) { _running = false; yield break; }

//        var subjects = ParseSubjects(subjectsText);
//        if (subjects?.subjects == null || subjects.subjects.Count == 0) { _running = false; yield break; }
//        subjects.subjects.Sort(CompareById);

//        // 2) По предметам строго последовательно
//        foreach (var s in subjects.subjects)
//        {
//            if (s == null || string.IsNullOrEmpty(s.id)) continue;
//            if (verbose) Debug.Log($"[Prefetch] Subject {s.id}");

//            // topics.json
//            string topicsRel = $"{contentRoot}/{s.id}/topics.json";
//            string topicsUrl = BuildStorageUrl(topicsRel);

//            string topicsText = null; done = false;
//            yield return CacheService.GetText(
//                topicsUrl,
//                cacheKey: "json:" + topicsRel,
//                onDone: t => { topicsText = t; done = true; },
//                onError: e => { if (e != null && e.Contains("404")) { if (verbose) Debug.Log($"[Prefetch] skip missing topics: {topicsRel}"); done = true; } else { if (verbose) Debug.LogWarning("[Prefetch] topics.json: " + e); done = true; } }
//            );
//            if (!done) yield return null;
//            if (string.IsNullOrEmpty(topicsText)) continue;

//            var topics = ParseTopics(topicsText);
//            if (topics?.topics == null || topics.topics.Count == 0) continue;
//            topics.topics.Sort(CompareById);

//            // 3) По темам строго последовательно
//            foreach (var t in topics.topics)
//            {
//                if (t == null || string.IsNullOrEmpty(t.id)) continue;
//                if (verbose) Debug.Log($"[Prefetch]  Topic {s.id}/{t.id}");

//                // subtopics.json
//                string subsRel = $"{contentRoot}/{s.id}/{t.id}/subtopics.json";
//                string subsUrl = BuildStorageUrl(subsRel);

//                string subsText = null; done = false;
//                yield return CacheService.GetText(
//                    subsUrl,
//                    cacheKey: "json:" + subsRel,
//                    onDone: txt => { subsText = txt; done = true; },
//                    onError: e => { if (e != null && e.Contains("404")) { if (verbose) Debug.Log($"[Prefetch]  skip missing subtopics: {subsRel}"); done = true; } else { if (verbose) Debug.LogWarning("[Prefetch] subtopics.json: " + e); done = true; } }
//                );
//                if (!done) yield return null;
//                if (string.IsNullOrEmpty(subsText)) continue;

//                var subtopics = ParseSubtopics(subsText);
//                if (subtopics?.subtopics == null || subtopics.subtopics.Count == 0) continue;
//                subtopics.subtopics.Sort(CompareById);

//                // 4) По подтемам: v1 → v2 → q1..qN
//                foreach (var st in subtopics.subtopics)
//                {
//                    if (st == null || string.IsNullOrEmpty(st.id)) continue;
//                    if (verbose) Debug.Log($"[Prefetch]   Subtopic {s.id}/{t.id}/{st.id}");

//                    // v1.mp4 и v2.mp4
//                    yield return PrefetchVideo($"{contentRoot}/{s.id}/{t.id}/{st.id}/v1");
//                    yield return PrefetchVideo($"{contentRoot}/{s.id}/{t.id}/{st.id}/v2");

//                    // q1..qN
//                    int qCount = st.answers != null ? st.answers.Count : 0;
//                    for (int i = 1; i <= qCount; i++)
//                    { yield return PrefetchImage($"{contentRoot}/{s.id}/{t.id}/{st.id}/q{i}"); }
//                }
//            }
//        }
//        if (verbose) Debug.Log("[Prefetch] done (sequential)");
//        _running = false;
//    }

//    // === Helpers ===
//    private IEnumerator PrefetchVideo(string relativePathWithoutExt)
//    {
//        string url = BuildStorageUrl(relativePathWithoutExt);
//        bool ok = false;
//        yield return CacheService.GetFile(
//            url,
//            cacheKey: "video:" + url,
//            onDone: _ => { ok = true; if (verbose) Debug.Log("[Prefetch] video OK: " + relativePathWithoutExt); },
//            forcedExt: ".mp4",
//            onError: e => { if (verbose) Debug.LogWarning("[Prefetch] video FAIL: " + relativePathWithoutExt + " → " + e); }
//        );
//        if (!ok) yield return null; // keep strict order
//    }

//    private IEnumerator PrefetchImage(string relativePathNoExt)
//    {
//        string url = BuildStorageUrl(relativePathNoExt);
//        bool done = false;
//        yield return CacheService.GetTextureToCache(
//            url,
//            cacheKey: "img:" + url,
//            maxSide: maxImageSide,
//            onDone: ok => { done = true; if (verbose) Debug.Log(ok ? "[Prefetch] image OK: " + relativePathNoExt : "[Prefetch] image FAIL: " + relativePathNoExt); }
//        );
//        if (!done) yield return null;
//    }

//    private string BuildStorageUrl(string relativePath)
//    {
//        string encoded = UnityWebRequest.EscapeURL(relativePath).Replace("+", "%20");
//        return $"https://firebasestorage.googleapis.com/v0/b/{bucketHost}/o/{encoded}?alt=media";
//    }

//    // ===== JSON parsing =====
//    [Serializable] private class SubjectsRoot { public string version; public List<SubjectRow> subjects; }
//    [Serializable] private class SubjectRow { public string id; public string name; }
//    [Serializable] private class TopicsRoot { public List<TopicRow> topics; }
//    [Serializable] private class TopicRow { public string id; public string name; }
//    [Serializable] private class SubtopicsRoot { public List<SubtopicRow> subtopics; }
//    [Serializable] private class SubtopicRow { public string id; public string title; public List<string> answers; }

//    private static SubjectsRoot ParseSubjects(string json) { try { return JsonUtility.FromJson<SubjectsRoot>(json); } catch { return null; } }
//    private static TopicsRoot ParseTopics(string json) { try { return JsonUtility.FromJson<TopicsRoot>(json); } catch { return null; } }
//    private static SubtopicsRoot ParseSubtopics(string json) { try { return JsonUtility.FromJson<SubtopicsRoot>(json); } catch { return null; } }

//    private static int CompareById(object aObj, object bObj)
//    {
//        string a = null, b = null;
//        switch (aObj)
//        {
//            case SubjectRow sa: a = sa.id; break;
//            case TopicRow ta: a = ta.id; break;
//            case SubtopicRow sba: a = sba.id; break;
//        }
//        switch (bObj)
//        {
//            case SubjectRow sb: b = sb.id; break;
//            case TopicRow tb: b = tb.id; break;
//            case SubtopicRow sbb: b = sbb.id; break;
//        }
//        if (int.TryParse(a, out var ai) && int.TryParse(b, out var bi)) return ai.CompareTo(bi);
//        return string.Compare(a, b, StringComparison.Ordinal);
//    }
//}

// ===============================
// PrefetchController.cs — updated to use StoragePaths
// ===============================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefetchController : MonoBehaviour
{
    [Header("Verbose logs")] public bool verbose = false;

    public IEnumerator PrefetchAllSubjects(List<SubjectData> subjects)
    {
        foreach (var s in subjects)
        {
            string topicsRel = $"{s.id}/topics.json";
            string topicsUrl = StoragePaths.Content(topicsRel);
            if (verbose) Debug.Log("[Prefetch] topics.json: " + topicsUrl);

            string topicsJson = null;
            yield return CacheService.GetText(topicsUrl, "json:" + StoragePaths.ContentRoot + "/" + topicsRel,
                text => topicsJson = text,
                err => Debug.LogWarning("[Prefetch] topics.json error: " + err));
            if (string.IsNullOrEmpty(topicsJson)) continue;

            var topics = JsonFlex.ParseTopics(topicsJson) ?? new List<TopicData>();
            foreach (var t in topics)
            {
                string subRel = $"{s.id}/{t.id}/subtopics.json";
                string subUrl = StoragePaths.Content(subRel);
                if (verbose) Debug.Log("[Prefetch] subtopics.json: " + subUrl);

                string subsJson = null;
                yield return CacheService.GetText(subUrl, "json:" + StoragePaths.ContentRoot + "/" + subRel,
                    text => subsJson = text,
                    err => Debug.LogWarning("[Prefetch] subtopics.json error: " + err));
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
}
