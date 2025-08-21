using UnityEngine;
using UnityEngine.UI;

public class VideoLessonPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button testButton;            // Кнопка «Тест» на панели видео
    [SerializeField] private GameObject testPanelParent;   // Родитель панели теста (вся панель)

    [Header("Logic")]
    [SerializeField] private TestController testController; // Ссылка на TestController

    // Текущий выбор (должен быть задан при входе в панель видеоурока)
    [Header("Current Selection")]
    public CoursesData.Subject currentSubject;
    public CoursesData.Topic currentTopic;
    public CoursesData.Subtopic currentSubtopic;

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
        if (testButton)
            testButton.onClick.RemoveAllListeners();
    }

    public void SetSelection(CoursesData.Subject s, CoursesData.Topic t, CoursesData.Subtopic st)
    {
        currentSubject = s; currentTopic = t; currentSubtopic = st;
    }

    private void OnTestButtonClicked()
    {
        if (!testPanelParent || !testController)
        {
            Debug.LogError("[VideoLessonPanelController] Не назначены ссылки: testPanelParent/testController");
            return;
        }
        if (currentSubject == null || currentTopic == null || currentSubtopic == null)
        {
            Debug.LogError("[VideoLessonPanelController] Не задан текущий subject/topic/subtopic (вызови SetSelection или назначь в инспекторе)");
            return;
        }

        // Открываем панель теста и передаём контекст
        testPanelParent.SetActive(true);
        testController.StartTest(currentSubject, currentTopic, currentSubtopic);
    }
}