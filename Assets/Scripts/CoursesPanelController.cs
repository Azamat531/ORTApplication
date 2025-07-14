//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Управление меню курсов: список предметов → тем → подтем (в отдельной панели).
///// Данные берутся из ScriptableObject CoursesData.
///// </summary>
//public class CoursesPanelController : MonoBehaviour
//{
//    [Header("Data")]
//    [Tooltip("ScriptableObject с данными по курсам")]
//    public CoursesData coursesData;

//    [Header("UI Elements")]
//    [Tooltip("ScrollView для списока предметов")]
//    public GameObject subjectsScrollView;
//    [Tooltip("Префаб кнопки предмета")]
//    public GameObject subjectButtonPrefab;
//    [Tooltip("Content внутри SubjectsScrollView")]
//    public Transform subjectsContent;

//    [Tooltip("Контейнер для списка тем")]
//    public GameObject topicsContainer;
//    [Tooltip("Заголовок внутри TopicsContainer")]
//    public TMP_Text headerText;
//    [Tooltip("Префаб кнопки темы")]
//    public GameObject topicButtonPrefab;
//    [Tooltip("Content внутри TopicsContainer ScrollView")]
//    public Transform topicsContent;

//    [Header("Subtopics Panel")]
//    [Tooltip("Отдельная панель с подтемами")]
//    public GameObject subtopicsPanel;
//    [Tooltip("Заголовок панели подтем")]
//    public TMP_Text subtopicsHeaderText;
//    [Tooltip("Контейнер кнопок подтем")]
//    public Transform subtopicsContent;
//    [Tooltip("Префаб кнопки подтемы")]
//    public GameObject subtopicButtonPrefab;
//    [Tooltip("Кнопка Назад")]
//    public Button subtopicsBackButton;

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

//    /// <summary>
//    /// Отображает список предметов.
//    /// </summary>
//    void ShowSubjects()
//    {
//        subjectsScrollView.SetActive(true);
//        topicsContainer.SetActive(false);
//        subtopicsPanel.SetActive(false);
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

//    /// <summary>
//    /// Отображает список тем выбранного предмета.
//    /// </summary>
//    void ShowTopics(CoursesData.Subject subj)
//    {
//        subjectsScrollView.SetActive(false);
//        topicsContainer.SetActive(true);
//        subtopicsPanel.SetActive(false);
//        ClearChildren(topicsContent);

//        headerText.text = subj.name;

//        foreach (var topic in subj.topics)
//        {
//            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = topic.name;

//            var t = topic;
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() => ShowSubtopicsPanel(t));
//        }
//    }

//    /// <summary>
//    /// Показывает отдельную панель подтем.
//    /// </summary>
//    void ShowSubtopicsPanel(CoursesData.Topic topic)
//    {
//        topicsContainer.SetActive(false);
//        subtopicsPanel.SetActive(true);
//        ClearChildren(subtopicsContent);

//        subtopicsHeaderText.text = topic.name;

//        foreach (var sub in topic.subtopics)
//        {
//            var btnGO = Instantiate(subtopicButtonPrefab, subtopicsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = sub.name;

//            var s = sub;
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() =>
//            {
//                Debug.Log($"[Subtopic] Выбрана подтема: {s.name}");
//                // Здесь добавить: открыть видео, мини-тест и т.д.
//            });
//        }

//        subtopicsBackButton.onClick.RemoveAllListeners();
//        subtopicsBackButton.onClick.AddListener(() =>
//        {
//            subtopicsPanel.SetActive(false);
//            topicsContainer.SetActive(true);
//        });
//    }

//    /// <summary>
//    /// Возврат к списку предметов. Привязать к кнопке "Назад".
//    /// </summary>
//    public void BackToSubjects()
//    {
//        subtopicsPanel.SetActive(false);
//        topicsContainer.SetActive(false);
//        subjectsScrollView.SetActive(true);
//        ShowSubjects();
//    }

//    /// <summary>
//    /// Удаляет всех детей из указанного Transform.
//    /// </summary>
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
/// Управление меню курсов: список предметов → тем → подтем (в отдельной панели) → запуск теста.
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

    [Header("Subtopics Panel")]
    public GameObject subtopicsPanel;
    public TMP_Text subtopicsHeaderText;
    public Transform subtopicsContent;
    public GameObject subtopicButtonPrefab;
    public Button subtopicsBackButton;

    [Header("Test Panel")]
    public GameObject checkTestPanel;
    public CheckTestController checkTestController;

    private CoursesData.Subject[] subjects;

    void OnEnable()
    {
        if (coursesData == null)
        {
            Debug.LogError("[CoursesPanelController] CoursesData asset is not assigned!");
            return;
        }

        subjects = coursesData.subjects;
        ShowSubjects();
    }

    void ShowSubjects()
    {
        subjectsScrollView.SetActive(true);
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(false);
        checkTestPanel.SetActive(false);
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
        checkTestPanel.SetActive(false);
        ClearChildren(topicsContent);

        headerText.text = subj.name;

        foreach (var topic in subj.topics)
        {
            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = topic.name;

            var s = subj;
            var t = topic;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => StartTestForTopic(s, t));
            // Или для перехода к подтемам: btn.onClick.AddListener(() => ShowSubtopicsPanel(t));
        }
    }

    void ShowSubtopicsPanel(CoursesData.Topic topic)
    {
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(true);
        checkTestPanel.SetActive(false);
        ClearChildren(subtopicsContent);

        subtopicsHeaderText.text = topic.name;

        foreach (var sub in topic.subtopics)
        {
            var btnGO = Instantiate(subtopicButtonPrefab, subtopicsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = sub.name;

            var s = sub;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[Subtopic] Выбрана подтема: {s.name}");
                // TODO: Запуск видео или мини-теста
            });
        }

        subtopicsBackButton.onClick.RemoveAllListeners();
        subtopicsBackButton.onClick.AddListener(() =>
        {
            subtopicsPanel.SetActive(false);
            topicsContainer.SetActive(true);
        });
    }

    void StartTestForTopic(CoursesData.Subject subject, CoursesData.Topic topic)
    {
        subjectsScrollView.SetActive(false);
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(false);
        checkTestPanel.SetActive(true);

        checkTestController.StartTest(subject, topic);
    }

    public void BackToSubjects()
    {
        subtopicsPanel.SetActive(false);
        topicsContainer.SetActive(false);
        checkTestPanel.SetActive(false);
        subjectsScrollView.SetActive(true);
        ShowSubjects();
    }

    void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }
}
