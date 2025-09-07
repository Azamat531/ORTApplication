//// ============================================
//// File: Assets/Scripts/VideoLessonPanelController.cs
//// Small UX hardening: disable Test button during load
//// Loads subtopics.json ? builds q1..qN + passes v2 to TestController
//// ============================================
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Networking;

//public class VideoLessonPanelController : MonoBehaviour
//{
//    [Header("UI")]
//    [SerializeField] private Button testButton;
//    [SerializeField] private GameObject testPanelParent;

//    [Header("Logic")]
//    [SerializeField] private TestController testController;

//    [Header("Storage (Firebase)")]
//    public string bucketHost = "first-5b828.firebasestorage.app";
//    public string contentRoot = "content";

//    [Header("Current Selection (IDs)")]
//    public string subjectId;
//    public string topicId;
//    public string subtopicId;

//    private void Reset()
//    {
//        if (!testController && testPanelParent)
//            testController = testPanelParent.GetComponentInChildren<TestController>(true);
//    }

//    private void OnEnable()
//    {
//        if (testButton)
//        {
//            testButton.onClick.RemoveAllListeners();
//            testButton.onClick.AddListener(OnTestButtonClicked);
//        }
//    }

//    private void OnDisable()
//    {
//        if (testButton) testButton.onClick.RemoveAllListeners();
//    }

//    public void SetSelection(string sId, string tId, string stId)
//    {
//        subjectId = sId; topicId = tId; subtopicId = stId;
//    }

//    private void OnTestButtonClicked()
//    {
//        if (!testPanelParent || !testController)
//        {
//            Debug.LogError("[VideoLessonPanelController] Не назначены ссылки: testPanelParent/testController");
//            return;
//        }
//        if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(topicId) || string.IsNullOrEmpty(subtopicId))
//        {
//            Debug.LogError("[VideoLessonPanelController] Не задан subjectId/topicId/subtopicId");
//            return;
//        }

//        testPanelParent.SetActive(true);
//        if (testButton) testButton.interactable = false;
//        StartCoroutine(LoadSubtopicAndStartTest());
//    }

//    private IEnumerator LoadSubtopicAndStartTest()
//    {
//        // subtopics.json
//        string rel = $"{contentRoot}/{subjectId}/{topicId}/subtopics.json";
//        string url = BuildStorageUrl(rel);

//        List<string> answers = null; bool done = false;
//        yield return CacheService.GetText(
//            url,
//            cacheKey: "json:" + rel,
//            onDone: text => { answers = ExtractAnswersForCurrent(text); done = true; },
//            onError: e => { Debug.LogError("[VideoLessonPanelController] Load subtopics.json failed: " + e); done = true; }
//        );
//        if (!done) yield return null;

//        if (answers == null || answers.Count == 0)
//        {
//            Debug.LogError("[VideoLessonPanelController] answers не найдены для subtopicId=" + subtopicId);
//            if (testButton) testButton.interactable = true;
//            yield break;
//        }

//        // Вопросы q1..qN (без расширения — CacheService сам подберёт .png/.jpg/.jpeg/.webp)
//        var rq = new List<TestController.RemoteQuestion>(answers.Count);
//        for (int i = 0; i < answers.Count; i++)
//        {
//            string qRel = $"{contentRoot}/{subjectId}/{topicId}/{subtopicId}/q{i + 1}";
//            string qUrl = BuildStorageUrl(qRel);
//            rq.Add(new TestController.RemoteQuestion { imageUrl = qUrl, correctAnswer = answers[i] });
//        }

//        // v2 (решение) — mp4
//        string v2Rel = $"{contentRoot}/{subjectId}/{topicId}/{subtopicId}/v2";
//        string v2Url = BuildStorageUrl(v2Rel);

//        testController.StartTestFromRemote(rq, v2Url);
//        if (testButton) testButton.interactable = true;
//    }

//    private string BuildStorageUrl(string relativePath)
//    {
//        string encoded = UnityWebRequest.EscapeURL(relativePath).Replace("+", "%20");
//        return $"https://firebasestorage.googleapis.com/v0/b/{bucketHost}/o/{encoded}?alt=media";
//    }

//    [Serializable] private class SubtopicRow { public string id; public string title; public List<string> answers; }
//    [Serializable] private class SubtopicsRoot { public List<SubtopicRow> subtopics; }

//    private List<string> ExtractAnswersForCurrent(string subtopicsJson)
//    {
//        try
//        {
//            var root = JsonUtility.FromJson<SubtopicsRoot>(subtopicsJson);
//            if (root?.subtopics == null) return null;
//            foreach (var st in root.subtopics)
//                if (st != null && st.id == subtopicId) return st.answers;
//        }
//        catch (Exception e) { Debug.LogError("[VideoLessonPanelController] Parse error: " + e.Message); }
//        return null;
//    }
//}

// ===============================
// VideoLessonPanelController.cs — updated to use StoragePaths
// ===============================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoLessonPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button testButton;
    [SerializeField] private GameObject testPanelParent;

    [Header("Logic")]
    [SerializeField] private TestController testController;

    [Header("Current Selection (IDs)")]
    public string subjectId;
    public string topicId;
    public string subtopicId;

    private void Reset()
    {
        if (!testController && testPanelParent)
            testController = testPanelParent.GetComponentInChildren<TestController>(true);
    }

    private void OnEnable()
    {
        if (testButton)
        {
            testButton.onClick.RemoveAllListeners();
            testButton.onClick.AddListener(OnTestButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (testButton) testButton.onClick.RemoveAllListeners();
    }

    public void SetSelection(string sId, string tId, string stId)
    {
        subjectId = sId; topicId = tId; subtopicId = stId;
    }

    private void OnTestButtonClicked()
    {
        if (!testPanelParent || !testController)
        { Debug.LogError("[VideoLessonPanelController] Нет ссылок: testPanelParent/testController"); return; }
        if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(topicId) || string.IsNullOrEmpty(subtopicId))
        { Debug.LogError("[VideoLessonPanelController] Не задан subjectId/topicId/subtopicId"); return; }

        testPanelParent.SetActive(true);
        if (testButton) testButton.interactable = false;
        StartCoroutine(LoadSubtopicAndStartTest());
    }

    private IEnumerator LoadSubtopicAndStartTest()
    {
        string rel = $"{subjectId}/{topicId}/subtopics.json";
        string url = StoragePaths.Content(rel);

        List<string> answers = null; bool done = false;
        yield return CacheService.GetText(
            url,
            cacheKey: "json:" + StoragePaths.ContentRoot + "/" + rel,
            onDone: text => { answers = ExtractAnswersForCurrent(text); done = true; },
            onError: e => { Debug.LogError("[VideoLessonPanelController] Load subtopics.json failed: " + e); done = true; }
        );
        if (!done) yield return null;

        if (answers == null || answers.Count == 0)
        {
            Debug.LogError("[VideoLessonPanelController] answers не найдены для subtopicId=" + subtopicId);
            if (testButton) testButton.interactable = true;
            yield break;
        }

        var rq = new List<TestController.RemoteQuestion>(answers.Count);
        for (int i = 0; i < answers.Count; i++)
        {
            string qRel = $"{subjectId}/{topicId}/{subtopicId}/q{i + 1}";
            string qUrl = StoragePaths.Content(qRel);
            rq.Add(new TestController.RemoteQuestion { imageUrl = qUrl, correctAnswer = answers[i] });
        }

        string v2Rel = $"{subjectId}/{topicId}/{subtopicId}/v2";
        string v2Url = StoragePaths.Content(v2Rel);

        testController.StartTestFromRemote(rq, v2Url);
        if (testButton) testButton.interactable = true;
    }

    [Serializable] private class SubtopicRow { public string id; public string title; public List<string> answers; }
    [Serializable] private class SubtopicsRoot { public List<SubtopicRow> subtopics; }

    private List<string> ExtractAnswersForCurrent(string subtopicsJson)
    {
        try
        {
            var root = JsonUtility.FromJson<SubtopicsRoot>(subtopicsJson);
            if (root?.subtopics == null) return null;
            foreach (var st in root.subtopics)
                if (st != null && st.id == subtopicId) return st.answers;
        }
        catch (Exception e)
        {
            Debug.LogError("[VideoLessonPanelController] Parse error: " + e.Message);
        }
        return null;
    }
}
