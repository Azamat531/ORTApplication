//// PracticePanelController.cs
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class PracticePanelController : MonoBehaviour
//{
//    [Header("UI: Subjects")]
//    public GameObject subjectsScrollView;
//    public GameObject subjectButtonPrefab;
//    public Transform subjectsContent;

//    [Header("UI: Topics")]
//    public GameObject topicsContainer;
//    public GameObject topicProgressButtonPrefab;
//    public Transform topicsContent;

//    [Header("Header")]
//    public TMP_Text headerText;

//    [Header("Practice: One Question Panel")]
//    public PracticeOneQuestionPanel oneQuestionPanel;

//    private List<SubjectData> subjects = new();
//    private List<TopicData> topics = new();
//    private string currentSubjectId, currentSubjectName;

//    private readonly Dictionary<string, ProgressTopicButton> _topicBtns = new();

//    void OnEnable()
//    {
//        // Привязываем локальное хранилище очков к текущему пользователю
//        var user = AuthManager.Instance ? AuthManager.Instance.CurrentUser : null;
//        PointsService.SetCurrentUid(user != null ? user.UserId : null);

//        StartCoroutine(PointsService.SyncOnEnter());

//        if (oneQuestionPanel != null)
//            oneQuestionPanel.onPointsChanged = (sid, tid, newPts) =>
//            {
//                if (_topicBtns.TryGetValue(tid, out var btn) && btn != null)
//                    btn.SetPointsImmediate(newPts);
//            };

//        ShowOnly(subjects: true);
//        //StartCoroutine(LoadSubjects());
//        StartCoroutine(PracticeVersion.RefreshAndThen(() => StartCoroutine(LoadSubjects())));
//    }

//    void OnApplicationPause(bool pause)
//    {
//        if (pause) StartCoroutine(PointsService.SyncOnExit());
//    }

//    IEnumerator LoadSubjects()
//    {
//        string rel = $"subjects.json";
//        string url = StoragePaths.Practise(rel);

//        yield return CacheService.GetText(url, "json:" + StoragePaths.PractiseRoot + "/" + rel,
//            text => { subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>(); ShowSubjects(); },
//            err => Debug.LogError($"[Practice/Subjects] {err} url={url}"));
//    }

//    void ShowSubjects()
//    {
//        ShowOnly(subjects: true);
//        if (headerText) headerText.text = "Предметтер (Практика)";
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
//        string rel = $"{subjectId}/topics.json";
//        string url = StoragePaths.Practise(rel);

//        yield return CacheService.GetText(
//            url,
//            "json:" + StoragePaths.PractiseRoot + "/" + rel,
//            text =>
//            {
//                topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
//                if (topics.Count == 0)
//                {
//                    // Нет тем — просто возвращаемся к списку предметов
//                    ShowOnly(subjects: true, topics: false);
//                }
//                else
//                {
//                    ShowTopics(subjectName);
//                }
//            },
//            err =>
//            {
//                Debug.LogError($"[Practice/Topics] {err} url={url}");
//                // Ошибка / файла нет — тоже на список предметов
//                ShowOnly(subjects: true, topics: false);
//            }
//        );
//    }

//    void ShowTopics(string subjectName)
//    {
//        ShowOnly(topics: true);
//        if (headerText) headerText.text = subjectName + " (Практика)";
//        ClearChildren(topicsContent);
//        _topicBtns.Clear();

//        foreach (var t in topics)
//        {
//            var go = Instantiate(topicProgressButtonPrefab, topicsContent);
//            var pb = go.GetComponent<ProgressTopicButton>();
//            if (!pb) continue;

//            pb.Setup(currentSubjectId, t.id, t.name, onClick: () =>
//            {
//                if (oneQuestionPanel)
//                    oneQuestionPanel.Open(currentSubjectId, t.id, t.name);
//            });

//            _topicBtns[t.id] = pb;
//        }
//    }

//    void ShowOnly(bool subjects = false, bool topics = false)
//    {
//        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
//        if (topicsContainer) topicsContainer.SetActive(topics);
//    }

//    void ClearChildren(Transform parent)
//    {
//        if (!parent) return;
//        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
//    }
//}

// PracticePanelController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PracticePanelController : MonoBehaviour
{
    [Header("UI: Subjects")]
    public GameObject subjectsScrollView;
    public GameObject subjectButtonPrefab;
    public Transform subjectsContent;

    [Header("UI: Topics")]
    public GameObject topicsContainer;
    public GameObject topicProgressButtonPrefab;
    public Transform topicsContent;

    [Header("Header")]
    public TMP_Text headerText;

    [Header("Practice: One Question Panel")]
    public PracticeOneQuestionPanel oneQuestionPanel;

    private List<SubjectData> subjects = new();
    private List<TopicData> topics = new();
    private string currentSubjectId, currentSubjectName;

    private readonly Dictionary<string, ProgressTopicButton> _topicBtns = new();

    void OnEnable()
    {
        // Привязываем локальное хранилище очков к текущему пользователю
        var user = AuthManager.Instance ? AuthManager.Instance.CurrentUser : null;
        PointsService.SetCurrentUid(user != null ? user.UserId : null);

        StartCoroutine(PointsService.SyncOnEnter());

        if (oneQuestionPanel != null)
            oneQuestionPanel.onPointsChanged = (sid, tid, newPts) =>
            {
                if (_topicBtns.TryGetValue(tid, out var btn) && btn != null)
                    btn.SetPointsImmediate(newPts);
            };

        ShowOnly(subjects: true);
        // проверка версии + загрузка предметов
        StartCoroutine(PracticeVersion.RefreshAndThen(() => StartCoroutine(LoadSubjects())));
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) StartCoroutine(PointsService.SyncOnExit());
    }

    IEnumerator LoadSubjects()
    {
        string rel = $"subjects.json";
        string url = StoragePaths.Practise(rel);

        yield return CacheService.GetText(url, "json:" + StoragePaths.PractiseRoot + "/" + rel,
            text => { subjects = JsonFlex.ParseSubjects(text) ?? new List<SubjectData>(); ShowSubjects(); },
            err => Debug.LogError($"[Practice/Subjects] {err} url={url}"));
    }

    void ShowSubjects()
    {
        ShowOnly(subjects: true);
        if (headerText) headerText.text = "Предметтер (Практика)";
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
        string url = StoragePaths.Practise(rel);

        yield return CacheService.GetText(
            url,
            "json:" + StoragePaths.PractiseRoot + "/" + rel,
            text =>
            {
                topics = JsonFlex.ParseTopics(text) ?? new List<TopicData>();
                if (topics.Count == 0)
                {
                    // Нет тем — просто возвращаемся к списку предметов
                    ShowOnly(subjects: true, topics: false);
                }
                else
                {
                    ShowTopics(subjectName);
                }
            },
            err =>
            {
                Debug.LogError($"[Practice/Topics] {err} url={url}");
                // Ошибка / файла нет — тоже на список предметов
                ShowOnly(subjects: true, topics: false);
            }
        );
    }

    void ShowTopics(string subjectName)
    {
        ShowOnly(topics: true);
        if (headerText) headerText.text = subjectName + " (Практика)";
        ClearChildren(topicsContent);
        _topicBtns.Clear();

        foreach (var t in topics)
        {
            var go = Instantiate(topicProgressButtonPrefab, topicsContent);
            var pb = go.GetComponent<ProgressTopicButton>();
            if (!pb) continue;

            pb.Setup(currentSubjectId, t.id, t.name, onClick: () =>
            {
                if (oneQuestionPanel)
                    oneQuestionPanel.Open(currentSubjectId, t.id, t.name);
            });

            _topicBtns[t.id] = pb;
        }
    }

    void ShowOnly(bool subjects = false, bool topics = false)
    {
        if (subjectsScrollView) subjectsScrollView.SetActive(subjects);
        if (topicsContainer) topicsContainer.SetActive(topics);
    }

    void ClearChildren(Transform parent)
    {
        if (!parent) return;
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }
}
