//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine.Networking;
//using System;
//using System.Linq;

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

//    [Header("Debug")]
//    [Tooltip("При входе на панель удаляем кэш subjects.json, чтобы всё тянуть из сети.")]
//    public bool forceFreshSubjectsOnEnable = false;

//    // ---- Данные ----
//    private List<SubjectData> subjects = new List<SubjectData>();
//    private List<TopicData> topics = new List<TopicData>();
//    private List<SubtopicIndex> subtopics = new List<SubtopicIndex>();

//    private string currentSubjectId, currentSubjectName;
//    private string currentTopicId, currentTopicName;
//    private SubtopicIndex currentSubtopic;

//    // ждём просмотр v2 почти до конца
//    private bool waitingForSolution = false;

//    // хранить «какую версию отрисовали»
//    private string shownSubjectsVersion = null;
//    // чтобы прямой GET не применялся дважды
//    private bool appliedDirectOnce = false;

//    // singleton-сторож (чтоб не было двух контроллеров сразу)
//    private static CoursesPanelController _active;
//    void Awake()
//    {
//        if (_active != null && _active != this)
//        {
//            Debug.LogWarning("[DBG] duplicate CoursesPanelController, disabling: " + name);
//            gameObject.SetActive(false);
//            return;
//        }
//        _active = this;
//    }
//    void OnDestroy() { if (_active == this) _active = null; }

//    void OnEnable()
//    {
//        // Сколько контроллеров в сцене
//        var all = FindObjectsOfType<CoursesPanelController>(true);
//        Debug.Log($"[DBG] CoursesPanelController instances in scene: {all.Length}. This: {name} (active={gameObject.activeInHierarchy})");

//        CacheService.LogPersistentPath();

//        if (!prefetchController)
//            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();

//        if (backFromSubtopicsButton)
//        {
//            backFromSubtopicsButton.onClick.RemoveAllListeners();
//            backFromSubtopicsButton.onClick.AddListener(() =>
//            {
//                ShowOnly(topics: true);
//                ShowTopics(currentSubjectName);
//            });
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

//            testController.TestFinished -= OnTestFinished;
//            testController.TestFinished += OnTestFinished;

//            if (testController.solutionVideoPlayer)
//            {
//                testController.solutionVideoPlayer.nearEndSeconds = 10f;
//                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
//                testController.solutionVideoPlayer.NearEndReached += OnSolutionNearEnd;
//            }
//        }

//        if (popupImage) popupImage.gameObject.SetActive(false);

//        if (forceFreshSubjectsOnEnable) RemoveSubjectsCache();   // на отладке — чистим кэш

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
//        appliedDirectOnce = false; // новая загрузка — позволим direct-ветке один раз обновить UI

//        string rel = "subjects.json";
//        string url = StoragePaths.Content(rel);
//        string cacheKey = "json:" + StoragePaths.ContentRoot + "/" + rel;

//        Debug.Log($"[DBG] LoadSubjects() url={url}  cacheKey={cacheKey}");

//        // Параллельно: прямой сетевой запрос — И ТЕПЕРЬ он не только логирует, но и ПРИМЕНЯЕТ, если список отличается
//        StartCoroutine(DirectFetchAndApplyIfDifferent(url));

//        yield return CacheService.GetText(
//            url,
//            cacheKey,
//            onDone: text =>
//            {
//                string ver = ContentVersion.Extract(text);
//                var list = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();

//                Debug.Log($"[DBG] GetText->onDone (cache or fresh) version={ver}  subjects.count={list.Count}  sample={SampleSubjects(list)}");

//                // отрисовали то, что пришло первым (часто — кэш)
//                ApplySubjectsToUI(list, ver, reason: "GetText onDone");

//                // если ContentVersion считает, что версия новая — параноидально попробуем подтянуть напрямую
//                if (ContentVersion.ShouldPrefetch(ver))
//                {
//                    Debug.Log($"[DBG] ShouldPrefetch=TRUE (saved != {ver}). Force refresh from network...");
//                    StartCoroutine(ForceReloadSubjectsFromNetwork(url, ver));
//                }
//            },
//            onError: err => Debug.LogError($"[Subjects] {err} url={url}")
//        );
//    }

//    /// Прямой сетевой запрос с cache-buster: если список/версия отличаются от уже показанных — применяем сразу.
//    private IEnumerator DirectFetchAndApplyIfDifferent(string baseUrl)
//    {
//        string freshUrl = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "_cb=" + DateTime.UtcNow.Ticks;
//        using (var req = UnityWebRequest.Get(freshUrl))
//        {
//            yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//            bool ok = req.result == UnityWebRequest.Result.Success;
//#else
//            bool ok = !req.isNetworkError && !req.isHttpError;
//#endif
//            if (!ok)
//            {
//                Debug.LogWarning($"[DBG] Direct GET FAILED: {req.responseCode} {req.error}");
//                yield break;
//            }

//            string json = req.downloadHandler.text;
//            var list = JsonFlex.ParseSubjects(json) ?? new List<SubjectData>();
//            string ver = ContentVersion.Extract(json);

//            Debug.Log($"[DBG] Direct GET OK: version={ver}  subjects.count={list.Count}  sample={SampleSubjects(list)}  url={freshUrl}");

//            // Если уже применяли direct-результат — не дублируем
//            if (appliedDirectOnce) yield break;

//            // Условие «данные другие?»
//            bool different =
//                shownSubjectsVersion == null ||
//                !string.Equals(shownSubjectsVersion, ver, StringComparison.Ordinal) ||
//                list.Count != subjects.Count ||
//                !SameIds(list, subjects);

//            if (different)
//            {
//                ApplySubjectsToUI(list, ver, reason: "Direct GET");
//                appliedDirectOnce = true;
//                ContentVersion.Save(ver);
//            }
//        }
//    }

//    /// Жёстко загружаем из сети и применяем (когда ShouldPrefetch сработал по кэш-версии).
//    private IEnumerator ForceReloadSubjectsFromNetwork(string baseUrl, string newVersion)
//    {
//        RemoveSubjectsCache();

//        string freshUrl = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "_cb=" + DateTime.UtcNow.Ticks;
//        using (var req = UnityWebRequest.Get(freshUrl))
//        {
//            yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//            if (req.result != UnityWebRequest.Result.Success)
//#else
//            if (req.isNetworkError || req.isHttpError)
//#endif
//            {
//                Debug.LogWarning("[Subjects] network refresh failed: " + req.error);
//                yield break;
//            }

//            string freshJson = req.downloadHandler.text;
//            var list = JsonFlex.ParseSubjects(freshJson) ?? new List<SubjectData>();

//            ApplySubjectsToUI(list, newVersion, reason: "Force reload");
//            ContentVersion.Save(newVersion);
//        }
//    }

//    private void ApplySubjectsToUI(List<SubjectData> list, string version, string reason)
//    {
//        subjects = list ?? new List<SubjectData>();
//        shownSubjectsVersion = version;

//        ShowSubjects();
//        Debug.Log($"[DBG] APPLY ({reason}) -> shownVersion={shownSubjectsVersion}  renderCount={subjects.Count} children={subjectsContent?.childCount ?? -1}");
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
//        Debug.Log($"[DBG] RENDER subjects: count={subjects.Count}  children={subjectsContent?.childCount ?? -1}");
//    }

//    // =================== Topics ===================
//    IEnumerator LoadTopics(string subjectId, string subjectName)
//    {
//        string rel = $"{subjectId}/topics.json";
//        string url = StoragePaths.Content(rel);
//        Debug.Log($"[DBG] LoadTopics() url={url}");

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.ContentRoot + "/" + rel,
//            onDone: text =>
//            {
//                topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
//                Debug.Log($"[DBG] topics.count={topics.Count}  first={(topics.Count > 0 ? topics[0].name : "-")}");
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
//        Debug.Log($"[DBG] LoadSubtopics() url={url}");

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.ContentRoot + "/" + rel,
//            onDone: text =>
//            {
//                subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();
//                Debug.Log($"[DBG] subtopics.count={subtopics.Count}  first={(subtopics.Count > 0 ? subtopics[0].title : "-")}");
//                if (subtopics.Count == 0)
//                {
//                    CourseProgress.MarkTopicDone(subjectId, topicId);
//                    ShowOnly(topics: true);
//                    ShowTopics(currentSubjectName);
//                    return;
//                }

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

//        if (url != null && url.EndsWith("/v2") == false && !url.Contains("/v2"))
//            return;

//        waitingForSolution = false;
//        MarkSubtopicComplete();
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
//        for (int i = 0; i < list.Count; i++)
//        {
//            var s = list[i];
//            if (!CourseProgress.IsSubtopicDone(subjectId, topicId, s.id))
//                return false;
//        }
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

//    private static void DimHierarchy(Transform root, float alpha)
//    {
//        if (!root) return;
//        var graphics = root.GetComponentsInChildren<Graphic>(true);
//        for (int i = 0; i < graphics.Length; i++)
//        {
//            var g = graphics[i];
//            var c = g.color;
//            c.a = alpha;
//            g.color = c;
//        }
//    }

//    private void RemoveSubjectsCache()
//    {
//        try
//        {
//            string rel = "subjects.json";
//            string cacheKey = "json:" + StoragePaths.ContentRoot + "/" + rel;
//            string encPath = CacheService.GetCachedPath(cacheKey, ".json");
//            if (!string.IsNullOrEmpty(encPath) && File.Exists(encPath))
//            {
//                File.Delete(encPath);
//                Debug.Log("[DBG] subjects cache removed: " + encPath);
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.LogWarning("[DBG] cache remove error: " + e.Message);
//        }
//    }

//    // ======== helpers ========
//    private string SampleSubjects(List<SubjectData> list)
//    {
//        if (list == null || list.Count == 0) return "(empty)";
//        var items = list.Take(4).Select(s => $"{s.id}:{s.name}");
//        return string.Join(" | ", items);
//    }

//    private bool SameIds(List<SubjectData> a, List<SubjectData> b)
//    {
//        if (a == null || b == null || a.Count != b.Count) return false;
//        for (int i = 0; i < a.Count; i++)
//        {
//            if (!string.Equals(a[i].id, b[i].id, StringComparison.Ordinal)) return false;
//        }
//        return true;
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Linq;

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

    [Header("Empty/Popup UI")]
    [Tooltip("Панель/окно, которое показываем, если у предмета нет тем. Содержание заполнишь сам.")]
    public GameObject noTopicsPanel;

    [Header("Popup Image (сообщения)")]
    [SerializeField] private Image popupImage;
    [SerializeField] private float popupDuration = 2f;
    private Coroutine popupRoutine;

    [Header("Debug")]
    [Tooltip("При входе на панель удаляем кэш subjects.json, чтобы всё тянуть из сети.")]
    public bool forceFreshSubjectsOnEnable = false;

    // ---- Данные ----
    private List<SubjectData> subjects = new List<SubjectData>();
    private List<TopicData> topics = new List<TopicData>();
    private List<SubtopicIndex> subtopics = new List<SubtopicIndex>();

    private string currentSubjectId, currentSubjectName;
    private string currentTopicId, currentTopicName;
    private SubtopicIndex currentSubtopic;

    // ждём просмотр v2 почти до конца
    private bool waitingForSolution = false;

    // предметы: какая версия показана и применяли ли прямой GET
    private string shownSubjectsVersion = null;
    private bool appliedSubjectsDirectOnce = false;

    // темы: защита от двойного применения
    private bool appliedTopicsDirectOnce = false;

    // singleton-сторож
    private static CoursesPanelController _active;
    void Awake()
    {
        if (_active != null && _active != this)
        {
            Debug.LogWarning("[DBG] duplicate CoursesPanelController, disabling: " + name);
            gameObject.SetActive(false);
            return;
        }
        _active = this;
    }
    void OnDestroy() { if (_active == this) _active = null; }

    void OnEnable()
    {
        var all = FindObjectsOfType<CoursesPanelController>(true);
        Debug.Log($"[DBG] CoursesPanelController instances in scene: {all.Length}. This: {name} (active={gameObject.activeInHierarchy})");

        CacheService.LogPersistentPath();

        if (!prefetchController)
            prefetchController = GetComponent<PrefetchController>() ?? gameObject.AddComponent<PrefetchController>();

        if (backFromSubtopicsButton)
        {
            backFromSubtopicsButton.onClick.RemoveAllListeners();
            backFromSubtopicsButton.onClick.AddListener(() =>
            {
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

            testController.TestFinished -= OnTestFinished;
            testController.TestFinished += OnTestFinished;

            if (testController.solutionVideoPlayer)
            {
                testController.solutionVideoPlayer.nearEndSeconds = 10f;
                testController.solutionVideoPlayer.NearEndReached -= OnSolutionNearEnd;
                testController.solutionVideoPlayer.NearEndReached += OnSolutionNearEnd;
            }
        }

        if (popupImage) popupImage.gameObject.SetActive(false);
        if (noTopicsPanel) noTopicsPanel.SetActive(false);

        if (forceFreshSubjectsOnEnable) RemoveSubjectsCache();

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
        appliedSubjectsDirectOnce = false;

        string rel = "subjects.json";
        string url = StoragePaths.Content(rel);
        string cacheKey = "json:" + StoragePaths.ContentRoot + "/" + rel;

        Debug.Log($"[DBG] LoadSubjects() url={url}  cacheKey={cacheKey}");

        // Параллельный прямой GET
        StartCoroutine(DirectSubjectsFetchAndApplyIfDifferent(url));

        yield return CacheService.GetText(
            url,
            cacheKey,
            onDone: text =>
            {
                string ver = ContentVersion.Extract(text);
                var list = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>();

                Debug.Log($"[DBG] GetText->onDone (cache or fresh) version={ver}  subjects.count={list.Count}  sample={SampleSubjects(list)}");

                ApplySubjectsToUI(list, ver, reason: "GetText onDone");

                if (ContentVersion.ShouldPrefetch(ver))
                {
                    Debug.Log($"[DBG] ShouldPrefetch=TRUE (saved != {ver}). Force refresh from network...");
                    StartCoroutine(ForceReloadSubjectsFromNetwork(url, ver));
                }
            },
            onError: err => Debug.LogError($"[Subjects] {err} url={url}")
        );
    }

    private IEnumerator DirectSubjectsFetchAndApplyIfDifferent(string baseUrl)
    {
        string freshUrl = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "_cb=" + DateTime.UtcNow.Ticks;
        using (var req = UnityWebRequest.Get(freshUrl))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok)
            {
                Debug.LogWarning($"[DBG] Direct GET (subjects) FAILED: {req.responseCode} {req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            var list = JsonFlex.ParseSubjects(json) ?? new List<SubjectData>();
            string ver = ContentVersion.Extract(json);

            Debug.Log($"[DBG] Direct GET OK (subjects): version={ver}  subjects.count={list.Count}  sample={SampleSubjects(list)}  url={freshUrl}");

            if (appliedSubjectsDirectOnce) yield break;

            bool different =
                shownSubjectsVersion == null ||
                !string.Equals(shownSubjectsVersion, ver, StringComparison.Ordinal) ||
                list.Count != subjects.Count ||
                !SameSubjectIds(list, subjects);

            if (different)
            {
                ApplySubjectsToUI(list, ver, reason: "Direct GET");
                appliedSubjectsDirectOnce = true;
                ContentVersion.Save(ver);
            }
        }
    }

    private IEnumerator ForceReloadSubjectsFromNetwork(string baseUrl, string newVersion)
    {
        RemoveSubjectsCache();

        string freshUrl = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "_cb=" + DateTime.UtcNow.Ticks;
        using (var req = UnityWebRequest.Get(freshUrl))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning("[Subjects] network refresh failed: " + req.error);
                yield break;
            }

            string freshJson = req.downloadHandler.text;
            var list = JsonFlex.ParseSubjects(freshJson) ?? new List<SubjectData>();

            ApplySubjectsToUI(list, newVersion, reason: "Force reload");
            ContentVersion.Save(newVersion);
        }
    }

    private void ApplySubjectsToUI(List<SubjectData> list, string version, string reason)
    {
        subjects = list ?? new List<SubjectData>();
        shownSubjectsVersion = version;

        ShowSubjects();
        Debug.Log($"[DBG] APPLY SUBJECTS ({reason}) -> shownVersion={shownSubjectsVersion}  renderCount={subjects.Count} children={subjectsContent?.childCount ?? -1}");
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
        Debug.Log($"[DBG] RENDER subjects: count={subjects.Count}  children={subjectsContent?.childCount ?? -1}");
    }

    // =================== Topics ===================
    IEnumerator LoadTopics(string subjectId, string subjectName)
    {
        appliedTopicsDirectOnce = false;

        string rel = $"{subjectId}/topics.json";
        string url = StoragePaths.Content(rel);
        string cacheKey = "json:" + StoragePaths.ContentRoot + "/" + rel;

        if (noTopicsPanel) noTopicsPanel.SetActive(false);
        Debug.Log($"[DBG] LoadTopics() url={url}");

        // Параллельный прямой GET
        StartCoroutine(DirectTopicsFetchAndApplyIfDifferent(url, subjectId, subjectName));

        yield return CacheService.GetText(
            url,
            cacheKey,
            onDone: text =>
            {
                var list = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
                Debug.Log($"[DBG] topics(GetText).count={list.Count}  first={(list.Count > 0 ? list[0].name : "-")}");

                ApplyTopicsToUI(list, subjectName, subjectId, reason: "GetText onDone");
            },
            onError: err => Debug.LogError($"[Topics] {err} url={url}")
        );
    }

    private IEnumerator DirectTopicsFetchAndApplyIfDifferent(string baseUrl, string subjectId, string subjectName)
    {
        string freshUrl = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "_cb=" + DateTime.UtcNow.Ticks;
        using (var req = UnityWebRequest.Get(freshUrl))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok)
            {
                Debug.LogWarning($"[DBG] Direct GET (topics) FAILED: {req.responseCode} {req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            var list = JsonFlex.ParseTopics(json) ?? new List<TopicData>();
            Debug.Log($"[DBG] Direct GET OK (topics): count={list.Count} first={(list.Count > 0 ? list[0].name : "-")} url={freshUrl}");

            if (appliedTopicsDirectOnce) yield break;
            if (!string.Equals(currentSubjectId, subjectId, StringComparison.Ordinal)) yield break;

            bool different = list.Count != topics.Count || !SameTopicIds(list, topics);
            if (different)
            {
                ApplyTopicsToUI(list, subjectName, subjectId, reason: "Direct GET");
                appliedTopicsDirectOnce = true;
            }
        }
    }

    private void ApplyTopicsToUI(List<TopicData> list, string subjectName, string subjectId, string reason)
    {
        if (!string.Equals(subjectId, currentSubjectId, StringComparison.Ordinal))
        {
            Debug.Log($"[DBG] APPLY TOPICS skipped ({reason}) — subject changed");
            return;
        }

        topics = list ?? new List<TopicData>();

        if (topics.Count == 0)
        {
            ShowOnly(topics: true);
            if (headerText) headerText.text = subjectName;
            ClearChildren(topicsContent);
            if (noTopicsPanel) noTopicsPanel.SetActive(true);
            Debug.Log($"[DBG] APPLY TOPICS ({reason}) -> EMPTY, noTopicsPanel shown");
            return;
        }
        else
        {
            if (noTopicsPanel) noTopicsPanel.SetActive(false);
        }

        ShowTopics(subjectName);
        Debug.Log($"[DBG] APPLY TOPICS ({reason}) -> renderCount={topics.Count}  children={topicsContent?.childCount ?? -1}");
    }

    void ShowTopics(string subjectName)
    {
        ShowOnly(topics: true);
        if (headerText) headerText.text = subjectName;
        if (noTopicsPanel) noTopicsPanel.SetActive(false);
        ClearChildren(topicsContent);

        for (int i = 0; i < topics.Count; i++)
        {
            var t = topics[i];
            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = t.name;

            bool unlocked = (i == 0) || CourseProgress.IsTopicDone(currentSubjectId, topics[i - 1].id);
            bool locked = !unlocked;

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
        Debug.Log($"[DBG] LoadSubtopics() url={url}");

        yield return CacheService.GetText(
            url,
            "json:" + StoragePaths.ContentRoot + "/" + rel,
            onDone: text =>
            {
                subtopics = JsonFlex.ParseSubtopics(text) ?? new List<SubtopicIndex>();
                Debug.Log($"[DBG] subtopics.count={subtopics.Count}  first={(subtopics.Count > 0 ? subtopics[0].title : "-")}");
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

        if (url != null && url.EndsWith("/v2") == false && !url.Contains("/v2"))
            return;

        waitingForSolution = false;
        MarkSubtopicComplete();
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
        if (!ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
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

    private static void DimHierarchy(Transform root, float alpha)
    {
        if (!root) return;
        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            var c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }

    private void RemoveSubjectsCache()
    {
        try
        {
            string rel = "subjects.json";
            string cacheKey = "json:" + StoragePaths.ContentRoot + "/" + rel;
            string encPath = CacheService.GetCachedPath(cacheKey, ".json");
            if (!string.IsNullOrEmpty(encPath) && File.Exists(encPath))
            {
                File.Delete(encPath);
                Debug.Log("[DBG] subjects cache removed: " + encPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DBG] cache remove error: " + e.Message);
        }
    }

    // ======== helpers ========
    private string SampleSubjects(List<SubjectData> list)
    {
        if (list == null || list.Count == 0) return "(empty)";
        var items = list.Take(4).Select(s => $"{s.id}:{s.name}");
        return string.Join(" | ", items);
    }

    private bool SameSubjectIds(List<SubjectData> a, List<SubjectData> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!string.Equals(a[i].id, b[i].id, StringComparison.Ordinal)) return false;
        return true;
    }

    private bool SameTopicIds(List<TopicData> a, List<TopicData> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!string.Equals(a[i].id, b[i].id, StringComparison.Ordinal)) return false;
        return true;
    }
}
