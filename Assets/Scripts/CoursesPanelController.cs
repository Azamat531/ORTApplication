using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление меню курсов: список предметов ? тем ? подтем.
/// Данные берутся из ScriptableObject CoursesData.
/// </summary>
public class CoursesPanelController : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("ScriptableObject с данными по курсам")]
    public CoursesData coursesData;

    [Header("UI Elements")]
    [Tooltip("ScrollView для списока предметов")]
    public GameObject subjectsScrollView;
    [Tooltip("Префаб кнопки предмета")]
    public GameObject subjectButtonPrefab;
    [Tooltip("Content внутри SubjectsScrollView")]
    public Transform subjectsContent;

    [Tooltip("Контейнер для списка тем и подтем")]
    public GameObject topicsContainer;
    [Tooltip("Заголовок внутри TopicsContainer")]
    public TMP_Text headerText;
    [Tooltip("Префаб кнопки темы/подтемы")]
    public GameObject topicButtonPrefab;
    [Tooltip("Content внутри TopicsContainer ScrollView")]
    public Transform topicsContent;

    private CoursesData.Subject[] subjects;

    void OnEnable()
    {
        if (coursesData == null)
        {
            Debug.LogError("[CoursesPanelController] CoursesData asset is not assigned in Inspector!");
            return;
        }

        subjects = coursesData.subjects;
        ShowSubjects();
    }

    /// <summary>
    /// Отображает список предметов.
    /// </summary>
    void ShowSubjects()
    {
        subjectsScrollView.SetActive(true);
        topicsContainer.SetActive(false);
        ClearChildren(subjectsContent);

        foreach (var subj in subjects)
        {
            var btnGO = Instantiate(subjectButtonPrefab, subjectsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = subj.name;

            // локальная копия для замыкания
            var s = subj;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ShowTopics(s));
        }
    }

    /// <summary>
    /// Отображает список тем выбранного предмета.
    /// </summary>
    void ShowTopics(CoursesData.Subject subj)
    {
        subjectsScrollView.SetActive(false);
        topicsContainer.SetActive(true);
        ClearChildren(topicsContent);

        headerText.text = subj.name;

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

    /// <summary>
    /// Отображает список подтем выбранной темы.
    /// </summary>
    void ShowSubtopics(CoursesData.Topic topic)
    {
        subjectsScrollView.SetActive(false);
        topicsContainer.SetActive(true);
        ClearChildren(topicsContent);

        headerText.text = topic.name;

        foreach (var sub in topic.subtopics)
        {
            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
            var btn = btnGO.GetComponent<Button>();
            btnGO.GetComponentInChildren<TMP_Text>().text = sub.name;

            var s = sub;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"Subtopic selected: {s.name}");
                // TODO: загрузка видео или тестов по подтеме
            });
        }
    }

    /// <summary>
    /// Возврат к списку предметов. Привязать к кнопке "Назад" в TopicsContainer.
    /// </summary>
    public void BackToSubjects()
    {
        ShowSubjects();
    }

    /// <summary>
    /// Очищает все дочерние элементы в контейнере.
    /// </summary>
    void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }
}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Управляет меню курсов: список предметов → тем. При выборе темы запускается тест.
///// </summary>
////[RequireComponent(typeof(Canvas))]
//public class CoursesPanelController : MonoBehaviour
//{
//    [Header("Data")]
//    [Tooltip("ScriptableObject с предметами, темами и подтемами")]
//    public CoursesData coursesData;

//    [Header("References")]
//    [Tooltip("Меню курсов (Panel) для скрытия при старте теста")]
//    public GameObject coursesPanel;
//    [Tooltip("Компонент TestManager для запуска теста")]
//    public TestManager testManager;

//    [Header("Subjects List UI")]
//    public GameObject subjectsScrollView;
//    public GameObject subjectButtonPrefab;
//    public Transform subjectsContent;

//    [Header("Topics List UI")]
//    public GameObject topicsContainer;
//    public TMP_Text headerText;
//    public GameObject topicButtonPrefab;
//    public Transform topicsContent;

//    private CoursesData.Subject[] subjects;

//    void OnEnable()
//    {
//        if (coursesData == null)
//        {
//            Debug.LogError("CoursesData asset is not assigned in Inspector!");
//            return;
//        }
//        subjects = coursesData.subjects;
//        ShowSubjects();
//    }

//    /// <summary>
//    /// Показывает список предметов.
//    /// </summary>
//    void ShowSubjects()
//    {
//        subjectsScrollView.SetActive(true);
//        topicsContainer.SetActive(false);
//        headerText.text = "Предметы";
//        ClearChildren(subjectsContent);

//        foreach (var subj in subjects)
//        {
//            var btnGO = Instantiate(subjectButtonPrefab, subjectsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btnGO.GetComponentInChildren<TMP_Text>().text = subj.name;
//            btn.onClick.AddListener(() => ShowTopics(subj));
//        }
//    }

//    /// <summary>
//    /// Показывает список тем выбранного предмета.
//    /// </summary>
//    void ShowTopics(CoursesData.Subject subj)
//    {
//        Debug.Log($"[Courses] Показ тем для предмета «{subj.name}», всего тем: {subj.topics.Length}");

//        subjectsScrollView.SetActive(false);
//        topicsContainer.SetActive(true);
//        headerText.text = subj.name;
//        ClearChildren(topicsContent);

//        foreach (var topic in subj.topics)
//        {
//            var btnGO = Instantiate(topicButtonPrefab, topicsContent);
//            var btn = btnGO.GetComponent<Button>();
//            btn.onClick.RemoveAllListeners();
//            btn.onClick.AddListener(() => Debug.Log($"[Courses] Нажата тема: {topic.name}"));
//            btn.onClick.AddListener(() => StartTestForTopic(topic));

//        }
//    }

//    /// <summary>
//    /// Скрывает меню курсов и запускает тест для выбранной темы.
//    /// </summary>
//    void StartTestForTopic(CoursesData.Topic topic)
//    {
//        // Скрыть меню курсов
//        if (coursesPanel != null)
//            coursesPanel.SetActive(false);
//        else
//            gameObject.SetActive(false);

//        // Собираем список вопросов для теста
//        var questionList = GetQuestionsForTopic(topic);
//        // Запускаем тест
//        testManager.ShowQuestions(questionList);
//    }

//    /// <summary>
//    /// Возвращает список данных вопросов для указанной темы.
//    /// </summary>
//    List<TestManager.QuestionData> GetQuestionsForTopic(CoursesData.Topic topic)
//    {
//        var list = new List<TestManager.QuestionData>();
//        // Пример конвертации подтем в вопросы:
//        foreach (var sub in topic.subtopics)
//        {
//            list.Add(new TestManager.QuestionData
//            {
//                imagePath = sub.name,            // предполагается, что в Resources есть спрайты с таким именем
//                options = new[] { "Вариант 1", "Вариант 2", "Вариант 3" },
//                correctIndex = 0                 // TODO: задавайте реальный правильный индекс
//            });
//        }
//        return list;
//    }

//    /// <summary>
//    /// Возвращает к списку предметов.
//    /// </summary>
//    public void BackToSubjects()
//    {
//        if (coursesPanel != null) coursesPanel.SetActive(true);
//        ShowSubjects();
//    }

//    /// <summary>
//    /// Очищает все дочерние элементы в контейнере.
//    /// </summary>
//    void ClearChildren(Transform parent)
//    {
//        foreach (Transform child in parent)
//            Destroy(child.gameObject);
//    }
//}

