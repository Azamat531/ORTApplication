//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;

//public class CoursesPanelController : MonoBehaviour
//{
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
//    public VideoStreamPlayer videoStreamPlayer;     // v1 (теория)
//    public Button testButton;
//    public TestController testController;           // тест + solutionVideoPlayer (v2)

//    [Header("Header")]
//    public TMP_Text headerText;

//    [Header("Prefetch (optional)")]
//    public PrefetchController prefetchController;

//    [Header("Download Icons")]
//    public Sprite downloadIdleIcon;
//    public Sprite downloadDoneIcon;

//    [Header("Popup Image (сообщения)")]
//    [SerializeField] private Image popupImage;
//    [SerializeField] private float popupDuration = 2f;
//    private Coroutine popupRoutine;

//    // ---- Данные ----
//    private List<SubjectData> subjects = new();
//    private List<TopicData> topics = new();
//    private List<SubtopicIndex> subtopics = new();

//    private string currentSubjectId, currentSubjectName;
//    private string currentTopicId, currentTopicName;
//    private SubtopicIndex currentSubtopic;

//    // ждём просмотр v2 почти до конца
//    private bool waitingForSolution = false;

//    void OnEnable()
//    {
//        CacheService.LogPersistentPath();

//        if (!prefetchController)
//            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();

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

//            // результат теста
//            testController.TestFinished -= OnTestFinished;
//            testController.TestFinished += OnTestFinished;

//            // почти конец решения (v2)
//            if (testController.solutionVideoPlayer)
//            {
//                testController.solutionVideoPlayer.nearEndSeconds = 10f;
//                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
//                testController.solutionVideoPlayer.NearEndReached += OnSolutionNearEnd;
//            }
//        }

//        if (popupImage) popupImage.gameObject.SetActive(false);

//        ShowOnly(subjects: true);
//        StartCoroutine(LoadSubjects());
//    }

//    void OnDisable()
//    {
//        if (testController)
//        {
//            testController.TestFinished -= OnTestFinished;
//            if (testController.solutionVideoPlayer)
//                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
//        }
//    }

//    // =================== Subjects ===================
//    IEnumerator LoadSubjects()
//    {
//        string rel = $"subjects.json";
//        string url = StoragePaths.Content(rel);

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.ContentRoot + "/" + rel,
//            onDone: text =>
//            {
//                subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();
//                string ver = ContentVersion.Extract(text);

//                if (ContentVersion.ShouldPrefetch(ver) && prefetchController && subjects.Count > 0)
//                {
//                    StartCoroutine(prefetchController.PrefetchAllSubjects(subjects));
//                    ContentVersion.Save(ver);
//                }

//                ShowSubjects();
//            },
//            onError: err => Debug.LogError($"[Subjects] {err} url={url}")
//        );
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

//    // =================== Topics ===================
//    IEnumerator LoadTopics(string subjectId, string subjectName)
//    {
//        string rel = $"{subjectId}/topics.json";
//        string url = StoragePaths.Content(rel);

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.ContentRoot + "/" + rel,
//            onDone: text =>
//            {
//                topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
//                ShowTopics(subjectName);
//            },
//            onError: err => Debug.LogError($"[Topics] {err} url={url}")
//        );
//    }

//    void ShowTopics(string subjectName)
//    {
//        ShowOnly(topics: true);
//        if (headerText) headerText.text = subjectName;
//        ClearChildren(topicsContent);

//        for (int i = 0; i < topics.Count; i++)
//        {
//            var t = topics[i];
//            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
//            btn.GetComponentInChildren<TMP_Text>().text = t.name;

//            bool unlocked = (i == 0) || CourseProgress.IsTopicDone(currentSubjectId, topics[i - 1].id);
//            bool locked = !unlocked;

//            // приглушаем вместо CanvasGroup
//            DimHierarchy(btn.transform, locked ? 0.6f : 1f);

//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() =>
//            {
//                if (locked) { ShowPopupImage(); return; }

//                currentTopicId = t.id; currentTopicName = t.name;
//                StartCoroutine(LoadSubtopics(currentSubjectId, currentTopicId, currentTopicName));
//            });
//        }
//    }

//    // =================== Subtopics ===================
//    IEnumerator LoadSubtopics(string subjectId, string topicId, string topicName)
//    {
//        string rel = $"{subjectId}/{topicId}/subtopics.json";
//        string url = StoragePaths.Content(rel);

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.ContentRoot + "/" + rel,
//            onDone: text =>
//            {
//                subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();
//                ShowSubtopics(topicName);
//            },
//            onError: err => Debug.LogError($"[Subtopics] {err} url={url}")
//        );
//    }

//    void ShowSubtopics(string topicName)
//    {
//        ShowOnly(subtopics: true);
//        if (headerText) headerText.text = topicName;
//        ClearChildren(subtopicsContent);

//        for (int i = 0; i < subtopics.Count; i++)
//        {
//            var s = subtopics[i];
//            var go = Instantiate(subtopicButtonPrefab, subtopicsContent);
//            var btn = go.GetComponent<Button>();
//            go.GetComponentInChildren<TMP_Text>().text = s.title;

//            bool unlocked = (i == 0) || CourseProgress.IsSubtopicDone(currentSubjectId, currentTopicId, subtopics[i - 1].id);
//            bool locked = !unlocked;

//            // приглушаем вместо CanvasGroup
//            DimHierarchy(go.transform, locked ? 0.6f : 1f);

//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() =>
//            {
//                if (locked) { ShowPopupImage(); return; }

//                currentSubtopic = s;
//                waitingForSolution = false;

//                string v1Rel = $"{currentSubjectId}/{currentTopicId}/{s.id}/v1";
//                string v1Url = StoragePaths.Content(v1Rel);

//                if (!CanOpenVideo(v1Url))
//                {
//                    ShowPopupImage();
//                    return;
//                }

//                if (videoScreenPanel) videoScreenPanel.SetActive(true);
//                if (videoStreamPlayer)
//                {
//                    videoStreamPlayer.streamIfNotCached = true;
//                    videoStreamPlayer.SetVideoURL(EnsureMp4(v1Url));
//                }
//                if (testButton) testButton.interactable = (s.answers != null && s.answers.Count > 0);
//            });

//            // Кнопка «Скачать»
//            var dlBtn = go.transform.Find("DownloadButton")?.GetComponent<Button>();
//            if (dlBtn && prefetchController)
//            {
//                var icon = dlBtn.GetComponent<Image>();
//                var label = dlBtn.GetComponentInChildren<TMP_Text>(true);

//                SetDownloadIdle(icon, label);
//                if (IsSubtopicFullyCached(s)) SetDownloadDone(icon, label);

//                dlBtn.onClick.RemoveAllListeners();
//                dlBtn.onClick.AddListener(() =>
//                {
//                    if (!dlBtn.interactable) return;
//                    dlBtn.interactable = false;
//                    SetDownloadIdle(icon, label);
//                    if (label) { label.gameObject.SetActive(true); label.text = "0%"; }

//                    StartCoroutine(prefetchController.PrefetchSubtopicCoursesProgress(
//                        currentSubjectId, currentTopicId, s.id,
//                        onProgress: p => { if (label) label.text = Mathf.RoundToInt(Mathf.Clamp01(p) * 100f) + "%"; },
//                        onDone: ok =>
//                        {
//                            dlBtn.interactable = true;
//                            if (ok) SetDownloadDone(icon, label);
//                            else if (label) label.text = "!";
//                        }));
//                });
//            }
//        }
//    }

//    // === События логики 60% / v2 ===
//    private void OnTestFinished(int correct, int total)
//    {
//        float pct = (total > 0) ? (float)correct / total : 0f;
//        if (pct >= 0.6f)
//        {
//            MarkSubtopicComplete();
//        }
//        else
//        {
//            waitingForSolution = true;
//            if (testController && testController.solutionVideoPlayer)
//                testController.solutionVideoPlayer.nearEndSeconds = 10f;
//        }
//    }

//    private void OnSolutionNearEnd(string url)
//    {
//        if (!waitingForSolution || currentSubtopic == null) return;

//        // На всякий случай проверим, что это v2
//        if (url != null && url.EndsWith("/v2") == false && !url.Contains("/v2"))
//            return;

//        waitingForSolution = false;
//        MarkSubtopicComplete();

//        // обновим список подтем (разблокируем следующую)
//        ShowSubtopics(currentTopicName);
//    }

//    private void MarkSubtopicComplete()
//    {
//        if (currentSubtopic == null) return;

//        CourseProgress.MarkSubtopicDone(currentSubjectId, currentTopicId, currentSubtopic.id);

//        if (AreAllSubtopicsDone(currentSubjectId, currentTopicId, subtopics))
//            CourseProgress.MarkTopicDone(currentSubjectId, currentTopicId);
//    }

//    private bool AreAllSubtopicsDone(string subjectId, string topicId, List<SubtopicIndex> list)
//    {
//        if (list == null || list.Count == 0) return false;
//        foreach (var s in list)
//            if (!CourseProgress.IsSubtopicDone(subjectId, topicId, s.id))
//                return false;
//        return true;
//    }

//    // === helpers for download button visuals ===
//    private void SetDownloadIdle(Image icon, TMP_Text label)
//    {
//        if (icon && downloadIdleIcon) icon.sprite = downloadIdleIcon;
//        if (label) label.gameObject.SetActive(false);
//    }
//    private void SetDownloadDone(Image icon, TMP_Text label)
//    {
//        if (icon && downloadDoneIcon) icon.sprite = downloadDoneIcon;
//        if (label) label.gameObject.SetActive(false);
//    }

//    private bool IsSubtopicFullyCached(SubtopicIndex s)
//    {
//        string v1 = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/v1");
//        string v2 = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/v2");
//        bool v1ok = !string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v1, ".mp4"));
//        bool v2ok = !string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v2, ".mp4"));
//        if (!v1ok || !v2ok) return false;

//        int qCount = s.answers != null ? s.answers.Count : 0;
//        for (int i = 0; i < qCount; i++)
//        {
//            string q = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/q{i + 1}");
//            if (string.IsNullOrEmpty(CacheService.GetCachedPath("img:" + q, ".png"))) return false;
//        }
//        return true;
//    }

//    private bool CanOpenVideo(string urlNoExt)
//    {
//        string urlMp4 = EnsureMp4(urlNoExt);
//        string encPath = CacheService.GetCachedPath("video:" + urlMp4, ".mp4");
//        bool hasCache = !string.IsNullOrEmpty(encPath);
//        bool online = Application.internetReachability != NetworkReachability.NotReachable;
//        if (!hasCache && !online) return false;
//        return true;
//    }

//    private string EnsureMp4(string url)
//    {
//        if (string.IsNullOrEmpty(url)) return url;
//        if (url.StartsWith("file:")) return url;
//        int q = url.IndexOf('?');
//        string baseUrl = q >= 0 ? url.Substring(0, q) : url;
//        string query = q >= 0 ? url.Substring(q) : string.Empty;
//        string ext = Path.GetExtension(baseUrl);
//        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
//        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase))
//            baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4";
//        return baseUrl + query;
//    }

//    // === Test ===
//    void OnTestButtonPressed()
//    {
//        if (currentSubtopic == null || currentSubtopic.answers == null || currentSubtopic.answers.Count == 0) return;

//        if (videoScreenPanel) videoScreenPanel.SetActive(false);
//        if (!testController) return;
//        testController.gameObject.SetActive(true);

//        var list = new List<TestController.RemoteQuestion>(currentSubtopic.answers.Count);
//        for (int i = 0; i < currentSubtopic.answers.Count; i++)
//        {
//            string qRel = $"{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/q{i + 1}";
//            string qUrl = StoragePaths.Content(qRel);
//            list.Add(new TestController.RemoteQuestion { imageUrl = qUrl, correctAnswer = currentSubtopic.answers[i] });
//        }
//        string v2Rel = $"{currentSubjectId}/{currentTopicId}/{currentSubtopic.id}/v2";
//        string v2Url = StoragePaths.Content(v2Rel);
//        testController.SetSolutionVideoUrl(EnsureMp4(v2Url));
//        testController.StartTestFromRemote(list);
//    }

//    void OnExitTest()
//    {
//        if (videoStreamPlayer) videoStreamPlayer.Pause();
//        if (videoScreenPanel) videoScreenPanel.SetActive(false);
//        if (subtopicsPanel) subtopicsPanel.SetActive(true);

//        if (testButton) testButton.interactable = currentSubtopic != null && currentSubtopic.answers != null && currentSubtopic.answers.Count > 0;

//        // Перерисуем (на случай, если подтема уже засчиталась)
//        if (!string.IsNullOrEmpty(currentTopicId))
//            ShowSubtopics(currentTopicName);
//    }

//    // === Общие утилиты ===
//    void ShowOnly(bool subjects = false, bool topics = false, bool subtopics = false)
//    {
//        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
//        if (topicsContainer) topicsContainer.SetActive(topics);
//        if (subtopicsPanel) subtopicsPanel.SetActive(subtopics);
//        if (videoScreenPanel && (subjects || topics)) videoScreenPanel.SetActive(false);
//        if (testButton) testButton.interactable = false;
//    }

//    void ClearChildren(Transform parent)
//    {
//        if (!parent) return;
//        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
//    }

//    private void ShowPopupImage()
//    {
//        if (popupImage == null) return;
//        if (popupRoutine != null) StopCoroutine(popupRoutine);
//        popupRoutine = StartCoroutine(PopupRoutine());
//    }

//    private IEnumerator PopupRoutine()
//    {
//        popupImage.gameObject.SetActive(true);
//        yield return new WaitForSeconds(popupDuration);
//        popupImage.gameObject.SetActive(false);
//        popupRoutine = null;
//    }

//    // ==== НОВОЕ: приглушение без CanvasGroup ====
//    private static void DimHierarchy(Transform root, float alpha)
//    {
//        if (!root) return;
//        var graphics = root.GetComponentsInChildren<Graphic>(true); // Image, TMP_Text и т.п.
//        for (int i = 0; i < graphics.Length; i++)
//        {
//            var g = graphics[i];
//            var c = g.color;
//            c.a = alpha;
//            g.color = c;
//        }
//    }
//}

///* ============================
//   ПРОСТОЙ ЛОКАЛЬНЫЙ ПРОГРЕСС
//   ============================ */
//static class CourseProgress
//{
//    private static string KeySubtopic(string subjectId, string topicId, string subtopicId)
//        => $"course.subtopic.done:{subjectId}:{topicId}:{subtopicId}";

//    private static string KeyTopic(string subjectId, string topicId)
//        => $"course.topic.done:{subjectId}:{topicId}";

//    public static bool IsSubtopicDone(string subjectId, string topicId, string subtopicId)
//        => PlayerPrefs.GetInt(KeySubtopic(subjectId, topicId, subtopicId), 0) == 1;

//    public static void MarkSubtopicDone(string subjectId, string topicId, string subtopicId)
//    {
//        PlayerPrefs.SetInt(KeySubtopic(subjectId, topicId, subtopicId), 1);
//        PlayerPrefs.Save();
//    }

//    public static bool IsTopicDone(string subjectId, string topicId)
//        => PlayerPrefs.GetInt(KeyTopic(subjectId, topicId), 0) == 1;

//    public static void MarkTopicDone(string subjectId, string topicId)
//    {
//        PlayerPrefs.SetInt(KeyTopic(subjectId, topicId), 1);
//        PlayerPrefs.Save();
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
    public VideoStreamPlayer videoStreamPlayer;     // v1 (теория)
    public Button testButton;
    public TestController testController;           // тест + solutionVideoPlayer (v2)

    [Header("Header")]
    public TMP_Text headerText;

    [Header("Prefetch (optional)")]
    public PrefetchController prefetchController;

    [Header("Download Icons")]
    public Sprite downloadIdleIcon;
    public Sprite downloadDoneIcon;

    [Header("Popup Image (сообщения)")]
    [SerializeField] private Image popupImage;
    [SerializeField] private float popupDuration = 2f;
    private Coroutine popupRoutine;

    // ---- Данные ----
    private List<SubjectData> subjects = new List<SubjectData>();
    private List<TopicData> topics = new List<TopicData>();
    private List<SubtopicIndex> subtopics = new List<SubtopicIndex>();

    private string currentSubjectId, currentSubjectName;
    private string currentTopicId, currentTopicName;
    private SubtopicIndex currentSubtopic;

    // ждём просмотр v2 почти до конца
    private bool waitingForSolution = false;

    void OnEnable()
    {
        CacheService.LogPersistentPath();

        if (!prefetchController)
            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();

        if (backFromSubtopicsButton)
        {
            backFromSubtopicsButton.onClick.RemoveAllListeners();
            backFromSubtopicsButton.onClick.AddListener(() =>
            {
                // раньше мы только показывали контейнер тем — теперь ещё и перерисовываем список
                ShowOnly(topics: true);
                ShowTopics(currentSubjectName);
            });
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

            // результат теста
            testController.TestFinished -= OnTestFinished;
            testController.TestFinished += OnTestFinished;

            // почти конец решения (v2)
            if (testController.solutionVideoPlayer)
            {
                testController.solutionVideoPlayer.nearEndSeconds = 10f;
                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
                testController.solutionVideoPlayer.NearEndReached += OnSolutionNearEnd;
            }
        }

        if (popupImage) popupImage.gameObject.SetActive(false);

        ShowOnly(subjects: true);
        StartCoroutine(LoadSubjects());
    }

    void OnDisable()
    {
        if (testController)
        {
            testController.TestFinished -= OnTestFinished;
            if (testController.solutionVideoPlayer)
                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
        }
    }

    // =================== Subjects ===================
    IEnumerator LoadSubjects()
    {
        string rel = $"subjects.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(
            url,
            "json:" + StoragePaths.ContentRoot + "/" + rel,
            onDone: text =>
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
            onError: err => Debug.LogError($"[Subjects] {err} url={url}")
        );
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

    // =================== Topics ===================
    IEnumerator LoadTopics(string subjectId, string subjectName)
    {
        string rel = $"{subjectId}/topics.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(
            url,
            "json:" + StoragePaths.ContentRoot + "/" + rel,
            onDone: text =>
            {
                topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
                ShowTopics(subjectName);
            },
            onError: err => Debug.LogError($"[Topics] {err} url={url}")
        );
    }

    void ShowTopics(string subjectName)
    {
        ShowOnly(topics: true);
        if (headerText) headerText.text = subjectName;
        ClearChildren(topicsContent);

        for (int i = 0; i < topics.Count; i++)
        {
            var t = topics[i];
            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = t.name;

            bool unlocked = (i == 0) || CourseProgress.IsTopicDone(currentSubjectId, topics[i - 1].id);
            bool locked = !unlocked;

            // приглушаем без CanvasGroup
            DimHierarchy(btn.transform, locked ? 0.6f : 1f);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (locked) { ShowPopupImage(); return; }

                currentTopicId = t.id; currentTopicName = t.name;
                StartCoroutine(LoadSubtopics(currentSubjectId, currentTopicId, currentTopicName));
            });
        }
    }

    // =================== Subtopics ===================
    IEnumerator LoadSubtopics(string subjectId, string topicId, string topicName)
    {
        string rel = $"{subjectId}/{topicId}/subtopics.json";
        string url = StoragePaths.Content(rel);

        yield return CacheService.GetText(
            url,
            "json:" + StoragePaths.ContentRoot + "/" + rel,
            onDone: text =>
            {
                subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();

                // ⬇️ ВАЖНО: если тема пустая — сразу считаем её пройденной и возвращаемся к списку тем
                if (subtopics.Count == 0)
                {
                    CourseProgress.MarkTopicDone(subjectId, topicId);
                    ShowOnly(topics: true);
                    ShowTopics(currentSubjectName);
                    return;
                }

                ShowSubtopics(topicName);
            },
            onError: err => Debug.LogError($"[Subtopics] {err} url={url}")
        );
    }

    void ShowSubtopics(string topicName)
    {
        ShowOnly(subtopics: true);
        if (headerText) headerText.text = topicName;
        ClearChildren(subtopicsContent);

        for (int i = 0; i < subtopics.Count; i++)
        {
            var s = subtopics[i];
            var go = Instantiate(subtopicButtonPrefab, subtopicsContent);
            var btn = go.GetComponent<Button>();
            go.GetComponentInChildren<TMP_Text>().text = s.title;

            bool unlocked = (i == 0) || CourseProgress.IsSubtopicDone(currentSubjectId, currentTopicId, subtopics[i - 1].id);
            bool locked = !unlocked;

            DimHierarchy(go.transform, locked ? 0.6f : 1f);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (locked) { ShowPopupImage(); return; }

                currentSubtopic = s;
                waitingForSolution = false;

                string v1Rel = $"{currentSubjectId}/{currentTopicId}/{s.id}/v1";
                string v1Url = StoragePaths.Content(v1Rel);

                if (!CanOpenVideo(v1Url))
                {
                    ShowPopupImage();
                    return;
                }

                if (videoScreenPanel) videoScreenPanel.SetActive(true);
                if (videoStreamPlayer)
                {
                    videoStreamPlayer.streamIfNotCached = true;
                    videoStreamPlayer.SetVideoURL(EnsureMp4(v1Url));
                }
                if (testButton) testButton.interactable = (s.answers != null && s.answers.Count > 0);
            });

            // Кнопка «Скачать»
            var dlBtn = go.transform.Find("DownloadButton")?.GetComponent<Button>();
            if (dlBtn && prefetchController)
            {
                var icon = dlBtn.GetComponent<Image>();
                var label = dlBtn.GetComponentInChildren<TMP_Text>(true);

                SetDownloadIdle(icon, label);
                if (IsSubtopicFullyCached(s)) SetDownloadDone(icon, label);

                dlBtn.onClick.RemoveAllListeners();
                dlBtn.onClick.AddListener(() =>
                {
                    if (!dlBtn.interactable) return;
                    dlBtn.interactable = false;
                    SetDownloadIdle(icon, label);
                    if (label) { label.gameObject.SetActive(true); label.text = "0%"; }

                    StartCoroutine(prefetchController.PrefetchSubtopicCoursesProgress(
                        currentSubjectId, currentTopicId, s.id,
                        onProgress: p => { if (label) label.text = Mathf.RoundToInt(Mathf.Clamp01(p) * 100f) + "%"; },
                        onDone: ok =>
                        {
                            dlBtn.interactable = true;
                            if (ok) SetDownloadDone(icon, label);
                            else if (label) label.text = "!";
                        }));
                });
            }
        }
    }

    // === События логики 60% / v2 ===
    private void OnTestFinished(int correct, int total)
    {
        float pct = (total > 0) ? (float)correct / total : 0f;
        if (pct >= 0.6f)
        {
            MarkSubtopicComplete();
        }
        else
        {
            waitingForSolution = true;
            if (testController && testController.solutionVideoPlayer)
                testController.solutionVideoPlayer.nearEndSeconds = 10f;
        }
    }

    private void OnSolutionNearEnd(string url)
    {
        if (!waitingForSolution || currentSubtopic == null) return;

        // На всякий случай проверим, что это v2
        if (url != null && url.EndsWith("/v2") == false && !url.Contains("/v2"))
            return;

        waitingForSolution = false;
        MarkSubtopicComplete();

        // обновим список подтем (разблокируем следующую)
        ShowSubtopics(currentTopicName);
    }

    private void MarkSubtopicComplete()
    {
        if (currentSubtopic == null) return;

        CourseProgress.MarkSubtopicDone(currentSubjectId, currentTopicId, currentSubtopic.id);

        if (AreAllSubtopicsDone(currentSubjectId, currentTopicId, subtopics))
            CourseProgress.MarkTopicDone(currentSubjectId, currentTopicId);
    }

    private bool AreAllSubtopicsDone(string subjectId, string topicId, List<SubtopicIndex> list)
    {
        if (list == null || list.Count == 0) return false;
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (!CourseProgress.IsSubtopicDone(subjectId, topicId, s.id))
                return false;
        }
        return true;
    }

    // === helpers for download button visuals ===
    private void SetDownloadIdle(Image icon, TMP_Text label)
    {
        if (icon && downloadIdleIcon) icon.sprite = downloadIdleIcon;
        if (label) label.gameObject.SetActive(false);
    }
    private void SetDownloadDone(Image icon, TMP_Text label)
    {
        if (icon && downloadDoneIcon) icon.sprite = downloadDoneIcon;
        if (label) label.gameObject.SetActive(false);
    }

    private bool IsSubtopicFullyCached(SubtopicIndex s)
    {
        string v1 = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/v1");
        string v2 = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/v2");
        bool v1ok = !string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v1, ".mp4"));
        bool v2ok = !string.IsNullOrEmpty(CacheService.GetCachedPath("video:" + v2, ".mp4"));
        if (!v1ok || !v2ok) return false;

        int qCount = s.answers != null ? s.answers.Count : 0;
        for (int i = 0; i < qCount; i++)
        {
            string q = StoragePaths.Content($"{currentSubjectId}/{currentTopicId}/{s.id}/q{i + 1}");
            if (string.IsNullOrEmpty(CacheService.GetCachedPath("img:" + q, ".png"))) return false;
        }
        return true;
    }

    private bool CanOpenVideo(string urlNoExt)
    {
        string urlMp4 = EnsureMp4(urlNoExt);
        string encPath = CacheService.GetCachedPath("video:" + urlMp4, ".mp4");
        bool hasCache = !string.IsNullOrEmpty(encPath);
        bool online = Application.internetReachability != NetworkReachability.NotReachable;
        if (!hasCache && !online) return false;
        return true;
    }

    private string EnsureMp4(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.StartsWith("file:")) return url;
        int q = url.IndexOf('?');
        string baseUrl = q >= 0 ? url.Substring(0, q) : url;
        string query = q >= 0 ? url.Substring(q) : string.Empty;
        string ext = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(ext)) return baseUrl + ".mp4" + query;
        if (!ext.Equals(".mp4", System.StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl.Substring(0, baseUrl.Length - ext.Length) + ".mp4";
        return baseUrl + query;
    }

    // === Test ===
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
        testController.SetSolutionVideoUrl(EnsureMp4(v2Url));
        testController.StartTestFromRemote(list);
    }

    void OnExitTest()
    {
        if (videoStreamPlayer) videoStreamPlayer.Pause();
        if (videoScreenPanel) videoScreenPanel.SetActive(false);
        if (subtopicsPanel) subtopicsPanel.SetActive(true);

        if (testButton) testButton.interactable = currentSubtopic != null && currentSubtopic.answers != null && currentSubtopic.answers.Count > 0;

        // Перерисуем список подтем (на случай, если подтема уже засчиталась)
        if (!string.IsNullOrEmpty(currentTopicId))
            ShowSubtopics(currentTopicName);
    }

    // === Общие утилиты ===
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

    private void ShowPopupImage()
    {
        if (popupImage == null) return;
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        popupImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(popupDuration);
        popupImage.gameObject.SetActive(false);
        popupRoutine = null;
    }

    // приглушение без CanvasGroup
    private static void DimHierarchy(Transform root, float alpha)
    {
        if (!root) return;
        var graphics = root.GetComponentsInChildren<Graphic>(true); // Image, TMP_Text и т.п.
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            var c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }
}

