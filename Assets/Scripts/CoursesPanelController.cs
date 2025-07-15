//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Управление меню курсов: список предметов → тем → подтем.
///// Данные берутся из ScriptableObject CoursesData.
///// </summary>
//public class CoursesPanelController : MonoBehaviour
//{
//    [Header("Data")]
//    public CoursesData coursesData;

//    [Header("UI Elements")]
//    public GameObject subjectsScrollView;
//    public GameObject subjectButtonPrefab;
//    public Transform subjectsContent;

//    public GameObject topicsContainer;
//    public TMP_Text topicsHeaderText;
//    public GameObject topicButtonPrefab;
//    public Transform topicsContent;

//    public GameObject subtopicsContainer;
//    public TMP_Text subtopicsHeaderText;
//    public GameObject subtopicButtonPrefab;
//    public Transform subtopicsContent;

//    private CoursesData.Subject[] subjects;

//    void OnEnable()
//    {
//        if (coursesData == null)
//        {
//            Debug.LogError("[CoursesPanelController] CoursesData asset is not assigned in Inspector!");
//            return;
//        }

//        subjects = coursesData.subjects;
//        ShowSubjects();
//    }

//    void ShowSubjects()
//    {
//        subjectsScrollView.SetActive(true);
//        topicsContainer.SetActive(false);
//        subtopicsContainer.SetActive(false);
//        ClearChildren(subjectsContent);

//        foreach (var subj in subjects)
//        {
//            var btnGO = Instantiate(subjectButtonPrefab, subjectsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = subj.name;

//            var s = subj;
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() => ShowTopics(s));
//        }
//    }

//    void ShowTopics(CoursesData.Subject subj)
//    {
//        subjectsScrollView.SetActive(false);
//        topicsContainer.SetActive(true);
//        subtopicsContainer.SetActive(false);
//        ClearChildren(topicsContent);

//        topicsHeaderText.text = subj.name;

//        foreach (var topic in subj.topics)
//        {
//            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = topic.name;

//            var t = topic;
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() => ShowSubtopics(subj, t));
//        }
//    }

//    void ShowSubtopics(CoursesData.Subject subject, CoursesData.Topic topic)
//    {
//        topicsContainer.SetActive(false);
//        subtopicsContainer.SetActive(true);
//        ClearChildren(subtopicsContent);

//        subtopicsHeaderText.text = topic.name;

//        foreach (var sub in topic.subtopics)
//        {
//            var btnGO = Instantiate(subtopicButtonPrefab, subtopicsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = sub.name;
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() => Debug.Log($"Subtopic selected: {sub.name}"));
//        }
//    }

//    public void BackToSubjects()
//    {
//        ShowSubjects();
//    }

//    void ClearChildren(Transform parent)
//    {
//        foreach (Transform child in parent)
//            Destroy(child.gameObject);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление меню курсов: список предметов → тем → подтем.
/// Данные берутся из ScriptableObject CoursesData.
/// </summary>
public class CoursesPanelController : MonoBehaviour
{
    [Header("Data")]
    public CoursesData coursesData;

    [Header("UI Elements")]
    public GameObject subjectsScrollView;
    public GameObject subjectButtonPrefab;
    public Transform subjectsContent;

    public GameObject topicsContainer;
    public TMP_Text headerText;
    public GameObject topicButtonPrefab;
    public Transform topicsContent;

    public GameObject subtopicsPanel;
    public TMP_Text subtopicsHeaderText;
    public GameObject subtopicButtonPrefab;
    public Transform subtopicsContent;
    public Button backFromSubtopicsButton;

    private CoursesData.Subject[] subjects;
    private CoursesData.Subject currentSubject;

    private void OnEnable()
    {
        if (coursesData == null)
        {
            Debug.LogError("[CoursesPanelController] CoursesData asset is not assigned in Inspector!");
            return;
        }

        subjects = coursesData.subjects;
        ShowSubjects();

        if (backFromSubtopicsButton != null)
            backFromSubtopicsButton.onClick.AddListener(BackToTopics);
    }

    void ShowSubjects()
    {
        subjectsScrollView.SetActive(true);
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(false);
        ClearChildren(subjectsContent);

        foreach (var subj in subjects)
        {
            var btnGO = Instantiate(subjectButtonPrefab, subjectsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = subj.name;

            var s = subj;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ShowTopics(s));
        }
    }

    void ShowTopics(CoursesData.Subject subj)
    {
        subjectsScrollView.SetActive(false);
        topicsContainer.SetActive(true);
        subtopicsPanel.SetActive(false);
        ClearChildren(topicsContent);

        headerText.text = subj.name;
        currentSubject = subj;

        foreach (var topic in subj.topics)
        {
            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = topic.name;

            var t = topic;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ShowSubtopics(t));
        }
    }

    void ShowSubtopics(CoursesData.Topic topic)
    {
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(true);
        ClearChildren(subtopicsContent);

        subtopicsHeaderText.text = topic.name;

        foreach (var sub in topic.subtopics)
        {
            var btnGO = Instantiate(subtopicButtonPrefab, subtopicsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = sub.name;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"Subtopic selected: {sub.name}");
                // TODO: загрузка видео или мини-теста по подтеме
            });
        }
    }

    void BackToTopics()
    {
        subtopicsPanel.SetActive(false);
        topicsContainer.SetActive(true);
    }

    public void BackToSubjects()
    {
        ShowSubjects();
    }

    void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }
}
