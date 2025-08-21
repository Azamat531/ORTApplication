// ============================================
// File: Assets/Scripts/Infra/PrefetchController.cs
// Purpose: Тихая автоподкачка контента. Добавлен PrefetchAllSubjects(...)
//  - PrefetchAllSubjects(List<SubjectData>) → по каждому предмету вызывает PrefetchSubject
//  - PrefetchSubject(subjectId)             → topics_{id}.json → для каждой темы скачивает подтемы
//  - PrefetchTopic(subjectId, topicId)
//  - PrefetchSubtopic(SubtopicData)         → видео + картинки вопросов (пропуская кэш)
// ============================================
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PrefetchController : MonoBehaviour
{
    [Header("Storage")] public string bucketName = "first-5b828.firebasestorage.app"; public string contentFolder = "content";
    [Header("Behavior")] public bool skipIfOffline = true; public int maxImageSide = 2048;

    private readonly HashSet<string> _subjectsInProgress = new HashSet<string>();

    public IEnumerator PrefetchAllSubjects(List<SubjectData> subjects)
    {
        if (subjects == null || subjects.Count == 0) yield break;
        if (skipIfOffline && Application.internetReachability == NetworkReachability.NotReachable) yield break;
        foreach (var s in subjects)
        {
            if (s == null || string.IsNullOrEmpty(s.id)) continue;
            yield return PrefetchSubject(s.id);
            yield return null;
        }
    }

    public IEnumerator PrefetchSubject(string subjectId)
    {
        if (string.IsNullOrEmpty(subjectId)) yield break;
        if (skipIfOffline && Application.internetReachability == NetworkReachability.NotReachable) yield break;
        if (_subjectsInProgress.Contains(subjectId)) yield break;
        _subjectsInProgress.Add(subjectId);

        string relTopics = $"{contentFolder}/topics_{subjectId}.json"; string urlTopics = MakeUrl(relTopics);
        List<TopicData> topics = null; bool topicsDone = false;
        yield return CacheService.GetText(urlTopics, "json:" + relTopics,
            text => { topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>(); topicsDone = true; },
            err => { Debug.LogWarning($"[PrefetchSubject] {err}"); topicsDone = true; }
        );
        if (!topicsDone) yield return null;
        if (topics == null || topics.Count == 0) { _subjectsInProgress.Remove(subjectId); yield break; }

        foreach (var t in topics)
        { if (t == null || string.IsNullOrEmpty(t.id)) continue; yield return PrefetchTopic(subjectId, t.id); yield return null; }

        _subjectsInProgress.Remove(subjectId);
    }

    public IEnumerator PrefetchTopic(string subjectId, string topicId)
    {
        if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(topicId)) yield break;
        if (skipIfOffline && Application.internetReachability == NetworkReachability.NotReachable) yield break;

        string rel = $"{contentFolder}/{subjectId}/topic_{topicId}.json"; string url = MakeUrl(rel);
        List<SubtopicData> subs = null; bool done = false;
        yield return CacheService.GetText(url, "json:" + rel,
            text => { subs = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicData>(); done = true; },
            err => { Debug.LogWarning($"[PrefetchTopic] {err}"); done = true; }
        );
        if (!done) yield return null;
        if (subs == null || subs.Count == 0) yield break;

        foreach (var s in subs)
        { yield return PrefetchSubtopic(s); yield return null; }
    }

    public IEnumerator PrefetchSubtopic(SubtopicData s)
    {
        if (s == null) yield break;
        if (skipIfOffline && Application.internetReachability == NetworkReachability.NotReachable) yield break;

        // Видео
        if (!string.IsNullOrEmpty(s.videoURL))
        {
            string vext = GuessExt(s.videoURL, ".mp4");
            if (!CacheService.HasCached("video:" + s.videoURL, vext))
            {
                bool vdone = false;
                yield return CacheService.GetFile(
                    s.videoURL,
                    "video:" + s.videoURL,
                    _ => { vdone = true; },
                    forcedExt: vext,
                    onError: _ => { vdone = true; }
                );
                if (!vdone) yield return null;
            }
        }

        // Картинки вопросов
        if (s.questions != null)
        {
            foreach (var q in s.questions)
            {
                if (q == null || string.IsNullOrEmpty(q.imageUrl)) continue;
                if (!CacheService.HasCached("img:" + q.imageUrl, ".png"))
                {
                    bool fin = false;
                    yield return CacheService.GetTexture(
                        q.imageUrl,
                        "img:" + q.imageUrl,
                        sprite =>
                        {
                            if (sprite != null)
                            {
                                var tex = sprite.texture; Object.Destroy(sprite); if (tex) Object.Destroy(tex);
                            }
                            fin = true;
                        },
                        maxSide: maxImageSide,
                        onError: _ => { fin = true; }
                    );
                    if (!fin) yield return null;
                }
            }
        }
    }

    private string MakeUrl(string rel)
    { var enc = UnityEngine.Networking.UnityWebRequest.EscapeURL(rel).Replace("+", "%20"); return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{enc}?alt=media"; }

    private static string GuessExt(string url, string def)
    { try { var pure = url.Split('?')[0]; var ext = Path.GetExtension(pure); return string.IsNullOrEmpty(ext) ? def : ext; } catch { return def; } }
}
