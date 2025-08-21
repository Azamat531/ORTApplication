// ============================================
// File: Assets/Scripts/CoursesPanelController.cs
// Update (Aug 16, 2025): Версионный глобальный префетч
//  - При загрузке subjects.json читаем поле "version" и сравниваем с сохранённым
//  - Если версии РАЗНЫЕ (или нет сохранённой) → тихо префетчим ВСЕ предметы (скачиваются только НОВЫЕ файлы)
//  - Если версии СОВПАДАЮТ → ничего не качаем заранее (экономим трафик). Воспроизведение/тесты берут кэш «по требованию»
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CoursesPanelController : MonoBehaviour
{
    [Header("Firebase Storage")]
    public string bucketName = "first-5b828.firebasestorage.app";
    public string contentFolder = "content";

    [Header("UI: Subjects")] public GameObject subjectsScrollView; public GameObject subjectButtonPrefab; public Transform subjectsContent;
    [Header("UI: Topics")] public GameObject topicsContainer; public GameObject topicButtonPrefab; public Transform topicsContent;
    [Header("UI: Subtopics")] public GameObject subtopicsPanel; public GameObject subtopicButtonPrefab; public Transform subtopicsContent; public Button backFromSubtopicsButton;

    [Header("Video/Test")] public GameObject videoScreenPanel; public VideoStreamPlayer videoStreamPlayer; public Button testButton; public TestController testController;
    [Header("Header")] public TMP_Text headerText;

    [Header("Prefetch (silent)")]
    public PrefetchController prefetchController; // auto-add в OnEnable

    private List<SubjectData> subjects = new(); private List<TopicData> topics = new(); private List<SubtopicData> subtopics = new();

    private string currentSubjectId, currentSubjectName; private string currentTopicId, currentTopicName; private SubtopicData currentSubtopicData;

    void OnEnable()
    {
        if (!prefetchController)
            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();
        prefetchController.bucketName = bucketName; prefetchController.contentFolder = contentFolder;

        if (backFromSubtopicsButton) { backFromSubtopicsButton.onClick.RemoveAllListeners(); backFromSubtopicsButton.onClick.AddListener(() => ShowOnly(topics: true)); }
        if (testButton) { testButton.onClick.RemoveAllListeners(); testButton.onClick.AddListener(OnTestButtonPressed); testButton.interactable = false; }
        if (testController) { testController.ExitRequested -= OnExitTest; testController.ExitRequested += OnExitTest; }
        ShowOnly(subjects: true); StartCoroutine(LoadSubjects());
    }

    IEnumerator LoadSubjects()
    {
        string rel = $"{contentFolder}/subjects.json"; string url = MakeUrl(rel);
        yield return CacheService.GetText(url, "json:" + rel, text =>
        {
            // 1) Парсим список предметов
            subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();
            // 2) Читаем версию из subjects.json
            string ver = ContentVersion.Extract(text);
            bool needPrefetch = ContentVersion.ShouldPrefetch(ver);
            // 3) Если версия новая → тихо префетчим все предметы (скачиваются только новые файлы)
            if (needPrefetch && prefetchController && subjects.Count > 0)
            {
                StartCoroutine(prefetchController.PrefetchAllSubjects(subjects));
                ContentVersion.Save(ver); // фиксируем новую версию сразу, чтобы не триггерить повторно
            }
            ShowSubjects();
        }, err => Debug.LogError($"[Subjects] {err} url={url}"));
    }

    void ShowSubjects()
    {
        ShowOnly(subjects: true); if (headerText) headerText.text = "Предметы"; ClearChildren(subjectsContent);
        foreach (var s in subjects)
        {
            var btn = Instantiate(subjectButtonPrefab, subjectsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = s.name;
            btn.onClick.AddListener(() => { currentSubjectId = s.id; currentSubjectName = s.name; StartCoroutine(LoadTopics(currentSubjectId, currentSubjectName)); });
        }
    }

    IEnumerator LoadTopics(string subjectId, string subjectName)
    {
        string rel = $"{contentFolder}/topics_{subjectId}.json"; string url = MakeUrl(rel);
        yield return CacheService.GetText(url, "json:" + rel, text => { topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>(); ShowTopics(subjectName); }, err => Debug.LogError($"[Topics] {err} url={url}"));
    }

    void ShowTopics(string subjectName)
    {
        ShowOnly(topics: true); if (headerText) headerText.text = subjectName; ClearChildren(topicsContent);
        foreach (var t in topics)
        {
            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = t.name;
            btn.onClick.AddListener(() => { currentTopicId = t.id; currentTopicName = t.name; StartCoroutine(LoadSubtopics(currentSubjectId, currentTopicId, currentTopicName)); });
        }
    }

    IEnumerator LoadSubtopics(string subjectId, string topicId, string topicName)
    {
        string rel = $"{contentFolder}/{subjectId}/topic_{topicId}.json"; string url = MakeUrl(rel);
        yield return CacheService.GetText(url, "json:" + rel, text => { subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicData>(); ShowSubtopics(topicName); }, err => Debug.LogError($"[Subtopics] {err} url={url}"));
    }

    void ShowSubtopics(string topicName)
    {
        ShowOnly(subtopics: true); if (headerText) headerText.text = topicName; ClearChildren(subtopicsContent);
        foreach (var s in subtopics)
        {
            var btn = Instantiate(subtopicButtonPrefab, subtopicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = s.title;
            btn.onClick.AddListener(() =>
            {
                currentSubtopicData = s;
                if (videoScreenPanel) videoScreenPanel.SetActive(true);
                if (videoStreamPlayer && !string.IsNullOrEmpty(s.videoURL)) videoStreamPlayer.SetVideoURL(s.videoURL);
                if (testButton) testButton.interactable = (s.questions != null && s.questions.Count > 0);
            });
        }
    }

    void OnTestButtonPressed()
    {
        if (currentSubtopicData == null || currentSubtopicData.questions == null || currentSubtopicData.questions.Count == 0) { Debug.LogWarning("Подтема не выбрана или нет вопросов"); return; }
        if (videoScreenPanel) videoScreenPanel.SetActive(false);
        if (testController)
        {
            testController.gameObject.SetActive(true);
            var list = new List<TestController.RemoteQuestion>(currentSubtopicData.questions.Count);
            foreach (var q in currentSubtopicData.questions) list.Add(new TestController.RemoteQuestion { imageUrl = q.imageUrl, correctAnswer = q.correctAnswer });
            testController.StartTestFromRemote(list);
        }
    }

    private void OnExitTest()
    {
        if (videoStreamPlayer) videoStreamPlayer.Pause();
        if (videoScreenPanel) videoScreenPanel.SetActive(false);
        if (subtopicsPanel) subtopicsPanel.SetActive(true);
        if (testButton) testButton.interactable = currentSubtopicData != null && currentSubtopicData.questions != null && currentSubtopicData.questions.Count > 0;
    }

    string MakeUrl(string rel)
    { var enc = UnityEngine.Networking.UnityWebRequest.EscapeURL(rel).Replace("+", "%20"); return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{enc}?alt=media"; }

    void ShowOnly(bool subjects = false, bool topics = false, bool subtopics = false)
    {
        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
        if (topicsContainer) topicsContainer.SetActive(topics);
        if (subtopicsPanel) subtopicsPanel.SetActive(subtopics);
        if (videoScreenPanel && (subjects || topics)) videoScreenPanel.SetActive(false);
        if (testButton) testButton.interactable = false; currentSubtopicData = null;
    }

    void ClearChildren(Transform parent)
    { if (!parent) return; for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject); }
}

