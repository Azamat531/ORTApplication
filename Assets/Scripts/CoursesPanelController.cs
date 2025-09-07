//// ============================================
//// File: Assets/Scripts/CoursesPanelController.cs
//// FINAL layout with plurals (subjects / topics / subtopics)
////   content/subjects.json
////   content/{subjectId}/topics.json
////   content/{subjectId}/{topicId}/subtopics.json  (id,title,answers[])
////   content/{subjectId}/{topicId}/{subtopicId}/v1 (video)
////   content/{subjectId}/{topicId}/{subtopicId}/q1..qN (images)
//// Uses: CacheService, ContentVersion (optional), PrefetchController (optional prefetch)
//// No SubtopicData anymore — replaced by SubtopicIndex
//// ============================================
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using System;

//public class CoursesPanelController : MonoBehaviour
//{
//    [Header("Firebase Storage")]
//    public string bucketName = "first-5b828.firebasestorage.app";
//    public string contentFolder = "content";

//    [Header("UI: Subjects")]
//    public GameObject subjectsScrollView;
//    public GameObject subjectButtonPrefab;
//    public Transform subjectsContent;

//    [Header("UI: Topics")]
//    public GameObject topicsContainer;
//    public GameObject topicButtonPrefab;
//    public Transform topicsContent;

//    [Header("UI: Subtopics")]
//    public GameObject subtopicsPanel;
//    public GameObject subtopicButtonPrefab;
//    public Transform subtopicsContent;
//    public Button backFromSubtopicsButton;

//    [Header("Video/Test")]
//    public GameObject videoScreenPanel;
//    public VideoStreamPlayer videoStreamPlayer;
//    public Button testButton;
//    public TestController testController;

//    [Header("Header")]
//    public TMP_Text headerText;

//    [Header("Prefetch (optional)")]
//    public PrefetchController prefetchController; // можно не указывать; добавим на лету

//    private List<SubjectData> subjects = new();
//    private List<TopicData> topics = new();
//    private List<SubtopicIndex> subtopics = new();

//    private string currentSubjectId, currentSubjectName;
//    private string currentTopicId, currentTopicName;
//    private SubtopicIndex currentSubtopic; // вместо SubtopicData

//    void OnEnable()
//    {
//        if (!prefetchController)
//            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();
//        prefetchController.bucketName = bucketName;
//        prefetchController.contentFolder = contentFolder;

//        if (backFromSubtopicsButton)
//        {
//            backFromSubtopicsButton.onClick.RemoveAllListeners();
//            backFromSubtopicsButton.onClick.AddListener(() => ShowOnly(topics: true));
//        }

//        if (testButton)
//        {
//            testButton.onClick.RemoveAllListeners();
//            testButton.onClick.AddListener(OnTestButtonPressed);
//            testButton.interactable = false;
//        }

//        if (testController)
//        {
//            testController.ExitRequested -= OnExitTest;
//            testController.ExitRequested += OnExitTest;
//        }

//        ShowOnly(subjects: true);
//        StartCoroutine(LoadSubjects());
//    }

//    IEnumerator LoadSubjects()
//    {
//        string rel = $"{contentFolder}/subjects.json";
//        string url = MakeUrl(rel);

//        yield return CacheService.GetText(url, "json:" + rel, text =>
//        {
//            // 1) Парсим список предметов
//            subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();

//            // 2) Версия контента (опционально)
//            string ver = ContentVersion.Extract(text);
//            bool needPrefetch = ContentVersion.ShouldPrefetch(ver);

//            // 3) Если версия новая → тихо префетчим всё (качаются только новые файлы)
//            if (needPrefetch && prefetchController && subjects.Count > 0)
//            {
//                StartCoroutine(prefetchController.PrefetchAllSubjects(subjects));
//                ContentVersion.Save(ver);
//            }

//            ShowSubjects();
//        },
//        err => Debug.LogError($"[Subjects] {err} url={url}"));
//    }

//    void ShowSubjects()
//    {
//        ShowOnly(subjects: true);
//        if (headerText) headerText.text = "Предметы";
//        ClearChildren(subjectsContent);

//        foreach (var s in subjects)
//        {
//            var btn = Instantiate(subjectButtonPrefab, subjectsContent).GetComponent<Button>();
//            btn.GetComponentInChildren<TMP_Text>().text = s.name;
//            btn.onClick.AddListener(() =>
//            {
//                currentSubjectId = s.id; currentSubjectName = s.name;
//                StartCoroutine(LoadTopics(currentSubjectId, currentSubjectName));
//            });
//        }
//    }

//    IEnumerator LoadTopics(string subjectId, string subjectName)
//    {
//        string rel = $"{contentFolder}/{subjectId}/topics.json";
//        string url = MakeUrl(rel);

//        yield return CacheService.GetText(url, "json:" + rel, text =>
//        {
//            topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
//            ShowTopics(subjectName);
//        },
//        err => Debug.LogError($"[Topics] {err} url={url}"));
//    }

//    void ShowTopics(string subjectName)
//    {
//        ShowOnly(topics: true);
//        if (headerText) headerText.text = subjectName;
//        ClearChildren(topicsContent);

//        foreach (var t in topics)
//        {
//            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
//            btn.GetComponentInChildren<TMP_Text>().text = t.name;
//            btn.onClick.AddListener(() =>
//            {
//                currentTopicId = t.id; currentTopicName = t.name;
//                StartCoroutine(LoadSubtopics(currentSubjectId, currentTopicId, currentTopicName));
//            });
//        }
//    }

//    IEnumerator LoadSubtopics(string subjectId, string topicId, string topicName)
//    {
//        string rel = $"{contentFolder}/{subjectId}/{topicId}/subtopics.json";
//        string url = MakeUrl(rel);

//        yield return CacheService.GetText(url, "json:" + rel, text =>
//        {
//            subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();
//            ShowSubtopics(topicName);
//        },
//        err => Debug.LogError($"[Subtopics] {err} url={url}"));
//    }

//    void ShowSubtopics(string topicName)
//    {
//        ShowOnly(subtopics: true);
//        if (headerText) headerText.text = topicName;
//        ClearChildren(subtopicsContent);

//        foreach (var s in subtopics)
//        {
//            var btn = Instantiate(subtopicButtonPrefab, subtopicsContent).GetComponent<Button>();
//            btn.GetComponentInChildren<TMP_Text>().text = s.title;
//            btn.onClick.AddListener(() =>
//            {
//                currentSubtopic = s;

//                // Включаем видео-панель (v1) — видео всегда из кэша (Prefetch скачал заранее)
//                if (videoScreenPanel) videoScreenPanel.SetActive(true);
//                if (videoStreamPlayer)
//                {
//                    string baseRel = $"{contentFolder}/{currentSubjectId}/{currentTopicId}/{s.id}/v1";
//                    string v1Url = MakeUrl(baseRel); // без расширения — VideoStreamPlayer сам подставит .mp4
//                    videoStreamPlayer.SetVideoURL(v1Url);
//                }

//                // Кнопка теста — активна, если есть answers
//                if (testButton) testButton.interactable = (s.answers != null && s.answers.Count > 0);
//            });
//        }
//    }

//    void OnTestButtonPressed()
//    {
//        if (currentSubtopic == null || currentSubtopic.answers == null || currentSubtopic.answers.Count == 0)
//        {
//            Debug.LogWarning("Подтема не выбрана или нет вопросов");
//            return;
//        }
//        if (videoScreenPanel) videoScreenPanel.SetActive(false);

//        if (testController)
//        {
//            testController.gameObject.SetActive(true);

//            // Формируем список вопросов q1..qN
//            var list = new List<TestController.RemoteQuestion>(currentSubtopic.answers.Count);
//            for (int i = 0; i < currentSubtopic.answers.Count; i++)
//            {
//                string qRel = $"{contentFolder}/{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/q{i + 1}";
//                string qUrl = MakeUrl(qRel); // без расширения — CacheService сам подберёт
//                list.Add(new TestController.RemoteQuestion
//                {
//                    imageUrl = qUrl,
//                    correctAnswer = currentSubtopic.answers[i]
//                });
//            }

//            // Ссылка на решение v2
//            string v2Rel = $"{contentFolder}/{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/v2";
//            string v2Url = MakeUrl(v2Rel);

//            testController.StartTestFromRemote(list, v2Url);
//        }
//    }

//    private void OnExitTest()
//    {
//        if (videoStreamPlayer) videoStreamPlayer.Pause();
//        if (videoScreenPanel) videoScreenPanel.SetActive(false);
//        if (subtopicsPanel) subtopicsPanel.SetActive(true);
//        if (testButton) testButton.interactable = currentSubtopic != null && currentSubtopic.answers != null && currentSubtopic.answers.Count > 0;
//    }

//    string MakeUrl(string rel)
//    {
//        var enc = UnityEngine.Networking.UnityWebRequest.EscapeURL(rel).Replace("+", "%20");
//        return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{enc}?alt=media";
//    }

//    void ShowOnly(bool subjects = false, bool topics = false, bool subtopics = false)
//    {
//        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
//        if (topicsContainer) topicsContainer.SetActive(topics);
//        if (subtopicsPanel) subtopicsPanel.SetActive(subtopics);
//        if (videoScreenPanel && (subjects || topics)) videoScreenPanel.SetActive(false);
//        if (testButton) testButton.interactable = false;
//        currentSubtopic = null;
//    }

//    void ClearChildren(Transform parent)
//    {
//        if (!parent) return;
//        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
//    }

//    // Из видео-экрана назад в список подтем (если у тебя есть отдельная кнопка «Меню»)
//    public void ShowSubtopicsPanel()
//    {
//        ShowOnly(subtopics: true);
//    }
//}
// ===============================
// CoursesPanelController.cs — updated to use StoragePaths
// ===============================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CoursesPanelController : MonoBehaviour
{
    [Header("UI: Subjects")]
    public GameObject subjectsScrollView;
    public GameObject subjectButtonPrefab;
    public Transform subjectsContent;

    [Header("UI: Topics")]
    public GameObject topicsContainer;
    public GameObject topicButtonPrefab;
    public Transform topicsContent;

    [Header("UI: Subtopics")]
    public GameObject subtopicsPanel;
    public GameObject subtopicButtonPrefab;
    public Transform subtopicsContent;
    public Button backFromSubtopicsButton;

    [Header("Video/Test")]
    public GameObject videoScreenPanel;
    public VideoStreamPlayer videoStreamPlayer;
    public Button testButton;
    public TestController testController;

    [Header("Header")]
    public TMP_Text headerText;

    [Header("Prefetch (optional)")]
    public PrefetchController prefetchController;

    private List<SubjectData> subjects = new();
    private List<TopicData> topics = new();
    private List<SubtopicIndex> subtopics = new();

    private string currentSubjectId, currentSubjectName;
    private string currentTopicId, currentTopicName;
    private SubtopicIndex currentSubtopic;

    void OnEnable()
    {
        if (!prefetchController)
            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();

        if (backFromSubtopicsButton)
        {
            backFromSubtopicsButton.onClick.RemoveAllListeners();
            backFromSubtopicsButton.onClick.AddListener(() => ShowOnly(topics: true));
        }

        if (testButton)
        {
            testButton.onClick.RemoveAllListeners();
            testButton.onClick.AddListener(OnTestButtonPressed);
            testButton.interactable = false;
        }

        if (testController)
        {
            testController.ExitRequested -= OnExitTest;
            testController.ExitRequested += OnExitTest;
        }

        ShowOnly(subjects: true);
        StartCoroutine(LoadSubjects());
    }

    IEnumerator LoadSubjects()
    {
        string rel = $"subjects.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(url, "json:" + StoragePaths.ContentRoot + "/" + rel, text =>
        {
            subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();
            string ver = ContentVersion.Extract(text);
            if (ContentVersion.ShouldPrefetch(ver) && prefetchController && subjects.Count > 0)
            {
                StartCoroutine(prefetchController.PrefetchAllSubjects(subjects));
                ContentVersion.Save(ver);
            }
            ShowSubjects();
        },
        err => Debug.LogError($"[Subjects] {err} url={url}"));
    }

    void ShowSubjects()
    {
        ShowOnly(subjects: true);
        if (headerText) headerText.text = "Предметы";
        ClearChildren(subjectsContent);

        foreach (var s in subjects)
        {
            var btn = Instantiate(subjectButtonPrefab, subjectsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = s.name;
            btn.onClick.AddListener(() =>
            {
                currentSubjectId = s.id; currentSubjectName = s.name;
                StartCoroutine(LoadTopics(currentSubjectId, currentSubjectName));
            });
        }
    }

    IEnumerator LoadTopics(string subjectId, string subjectName)
    {
        string rel = $"{subjectId}/topics.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(url, "json:" + StoragePaths.ContentRoot + "/" + rel, text =>
        {
            topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
            ShowTopics(subjectName);
        },
        err => Debug.LogError($"[Topics] {err} url={url}"));
    }

    void ShowTopics(string subjectName)
    {
        ShowOnly(topics: true);
        if (headerText) headerText.text = subjectName;
        ClearChildren(topicsContent);

        foreach (var t in topics)
        {
            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = t.name;
            btn.onClick.AddListener(() =>
            {
                currentTopicId = t.id; currentTopicName = t.name;
                StartCoroutine(LoadSubtopics(currentSubjectId, currentTopicId, currentTopicName));
            });
        }
    }

    IEnumerator LoadSubtopics(string subjectId, string topicId, string topicName)
    {
        string rel = $"{subjectId}/{topicId}/subtopics.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(url, "json:" + StoragePaths.ContentRoot + "/" + rel, text =>
        {
            subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();
            ShowSubtopics(topicName);
        },
        err => Debug.LogError($"[Subtopics] {err} url={url}"));
    }

    void ShowSubtopics(string topicName)
    {
        ShowOnly(subtopics: true);
        if (headerText) headerText.text = topicName;
        ClearChildren(subtopicsContent);

        foreach (var s in subtopics)
        {
            var btn = Instantiate(subtopicButtonPrefab, subtopicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = s.title;
            btn.onClick.AddListener(() =>
            {
                currentSubtopic = s;
                if (videoScreenPanel) videoScreenPanel.SetActive(true);
                if (videoStreamPlayer)
                {
                    string v1Rel = $"{currentSubjectId}/{currentTopicId}/{s.id}/v1";
                    string v1Url = StoragePaths.Content(v1Rel);
                    videoStreamPlayer.SetVideoURL(v1Url);
                }
                if (testButton) testButton.interactable = (s.answers != null && s.answers.Count > 0);
            });
        }
    }

    void OnTestButtonPressed()
    {
        if (currentSubtopic == null || currentSubtopic.answers == null || currentSubtopic.answers.Count == 0) return;

        if (videoScreenPanel) videoScreenPanel.SetActive(false);
        if (!testController) return;

        testController.gameObject.SetActive(true);

        var list = new List<TestController.RemoteQuestion>(currentSubtopic.answers.Count);
        for (int i = 0; i < currentSubtopic.answers.Count; i++)
        {
            string qRel = $"{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/q{i + 1}";
            string qUrl = StoragePaths.Content(qRel);
            list.Add(new TestController.RemoteQuestion { imageUrl = qUrl, correctAnswer = currentSubtopic.answers[i] });
        }

        string v2Rel = $"{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/v2";
        string v2Url = StoragePaths.Content(v2Rel);
        testController.StartTestFromRemote(list, v2Url);
    }

    void OnExitTest()
    {
        if (videoStreamPlayer) videoStreamPlayer.Pause();
        if (videoScreenPanel) videoScreenPanel.SetActive(false);
        if (subtopicsPanel) subtopicsPanel.SetActive(true);
        if (testButton) testButton.interactable = currentSubtopic != null && currentSubtopic.answers != null && currentSubtopic.answers.Count > 0;
    }

    void ShowOnly(bool subjects = false, bool topics = false, bool subtopics = false)
    {
        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
        if (topicsContainer) topicsContainer.SetActive(topics);
        if (subtopicsPanel) subtopicsPanel.SetActive(subtopics);
        if (videoScreenPanel && (subjects || topics)) videoScreenPanel.SetActive(false);
        if (testButton) testButton.interactable = false;
    }

    void ClearChildren(Transform parent)
    {
        if (!parent) return;
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }
}
