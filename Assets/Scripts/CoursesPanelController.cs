// Assets/Scripts/CoursesPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление меню курсов: предметы → темы → подтемы.
/// При выборе подтемы открывается панель VideoScreen;
/// кнопка «Тест» на видео‑панели запускает тест по выбранной подтеме.
/// </summary>
/// using UnityEngine;
/// панели запускает тест по выбранной подтеме.
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
    public GameObject topicButtonPrefab;
    public Transform topicsContent;
    public TMP_Text headerText;

    public GameObject subtopicsPanel;
    public GameObject subtopicButtonPrefab;
    public Transform subtopicsContent;
    public Button backFromSubtopicsButton;

    [Header("Video Screen")]
    public GameObject videoScreenPanel;
    public VideoStreamPlayer videoStreamPlayer;
    public Button testButton;

    [Header("Test Controller")]
    public TestController testController;

    private CoursesData.Subject currentSubject;
    private CoursesData.Topic currentTopic;
    private CoursesData.Subtopic currentSubtopic;

    private void OnEnable()
    {
        if (coursesData == null)
        {
            Debug.LogError("CoursesData не назначен в Inspector!");
            return;
        }

        backFromSubtopicsButton?.onClick.RemoveAllListeners();
        backFromSubtopicsButton?.onClick.AddListener(ShowTopicsList);

        testButton?.onClick.RemoveAllListeners();
        testButton?.onClick.AddListener(OnTestButtonPressed);

        ShowSubjectsList();
    }

    void ShowSubjectsList()
    {
        subjectsScrollView.SetActive(true);
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(false);
        videoScreenPanel.SetActive(false);

        ClearChildren(subjectsContent);
        foreach (var subj in coursesData.subjects)
        {
            var btn = Instantiate(subjectButtonPrefab, subjectsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = subj.name;
            var s = subj;
            btn.onClick.AddListener(() => ShowTopicsList(s));
        }
    }

    void ShowTopicsList(CoursesData.Subject subj)
    {
        currentSubject = subj;
        subjectsScrollView.SetActive(false);
        topicsContainer.SetActive(true);
        subtopicsPanel.SetActive(false);
        videoScreenPanel.SetActive(false);

        headerText.text = subj.name;
        ClearChildren(topicsContent);
        foreach (var topic in subj.topics)
        {
            var btn = Instantiate(topicButtonPrefab, topicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = topic.name;
            var t = topic;
            btn.onClick.AddListener(() => ShowSubtopicsList(t));
        }
    }

    void ShowSubtopicsList(CoursesData.Topic topic)
    {
        currentTopic = topic;
        topicsContainer.SetActive(false);
        subtopicsPanel.SetActive(true);
        videoScreenPanel.SetActive(false);

        ClearChildren(subtopicsContent);
        foreach (var sub in topic.subtopics)
        {
            var btn = Instantiate(subtopicButtonPrefab, subtopicsContent).GetComponent<Button>();
            btn.GetComponentInChildren<TMP_Text>().text = sub.name;
            var s = sub;
            btn.onClick.AddListener(() =>
            {
                currentSubtopic = s;
                videoScreenPanel.SetActive(true);
                videoStreamPlayer.SetVideoURL(s.videoURL);
            });
        }
    }

    void ShowTopicsList()
    {
        subtopicsPanel.SetActive(false);
        topicsContainer.SetActive(true);
        videoScreenPanel.SetActive(false);
    }

    void OnTestButtonPressed()
    {
        if (testController == null)
        {
            Debug.LogError("TestController не назначен в Inspector!");
            return;
        }
        // Скрываем видео‑панель
        videoScreenPanel.SetActive(false);
        // Активируем панель теста
        testController.gameObject.SetActive(true);
        // Запускаем тест для выбранной подтемы
        testController.StartTest(currentSubject, currentTopic, currentSubtopic);
        testController.StartButtonPressed();
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
