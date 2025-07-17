// Assets/Scripts/For Test/TestController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class TestController : MonoBehaviour
{
    [Header("Data")]
    public TestQuestions questionsData;

    [Header("Panels & UI")]
    public GameObject beginImage;
    public GameObject loadingPanel;
    public GameObject testPanel;
    public GameObject testPanelParent;

    [Header("Quiz Setup")]
    public Button startTestButton;
    public ScrollRect scrollRect;
    public GameObject questionEntryPrefab;
    public GameObject finishButtonPrefab;

    [Header("Navigation")]
    public Button menuButtonPrefab;  // префаб кнопки «Меню»

    private List<QuestionData> questions = new List<QuestionData>();

    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public string imageUrl;
        [HideInInspector] public Sprite imageSprite;
        public List<string> options;
        public int correctIndex;
    }

    private void OnEnable()
    {
        startTestButton.onClick.RemoveAllListeners();
        startTestButton.onClick.AddListener(StartButtonPressed);
    }

    public void StartTest(
        CoursesData.Subject subject,
        CoursesData.Topic topic,
        CoursesData.Subtopic subtopic)
    {
        beginImage.SetActive(true);
        loadingPanel.SetActive(false);
        testPanel.SetActive(false);
        questions.Clear();

        var soSubj = questionsData.subjects.Find(s => s.name == subject.name);
        var soTopic = soSubj?.topics.Find(t => t.name == topic.name);
        var soSub = soTopic?.subtopics.Find(st => st.name == subtopic.name);

        if (soSub == null)
        {
            Debug.LogError($"Вопросы не найдены для {subject.name} → {topic.name} → {subtopic.name}");
            return;
        }

        foreach (var q in soSub.questions)
        {
            questions.Add(new QuestionData
            {
                questionText = q.questionText,
                imageUrl = q.imageUrl,
                options = new List<string>(q.options),
                correctIndex = q.correctIndex
            });
        }
    }

    public void StartButtonPressed()
    {
        beginImage.SetActive(false);
        loadingPanel.SetActive(true);
        StartCoroutine(PreloadAllImages());
    }

    private IEnumerator PreloadAllImages()
    {
        foreach (var q in questions)
        {
            if (q.imageSprite == null && !string.IsNullOrEmpty(q.imageUrl))
            {
                using var uwr = UnityWebRequestTexture.GetTexture(q.imageUrl);
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(uwr);
                    q.imageSprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        Vector2.one * 0.5f);
                }
            }
        }

        PopulateQuestions();
        loadingPanel.SetActive(false);
        testPanel.SetActive(true);
    }

    private void PopulateQuestions()
    {
        var content = scrollRect.content;
        foreach (Transform c in content) Destroy(c.gameObject);

        foreach (var q in questions)
        {
            var go = Instantiate(questionEntryPrefab, content);
            go.GetComponent<QuestionEntry>().Setup(
                q.questionText, q.imageSprite, q.options);
        }

        var finishGO = Instantiate(finishButtonPrefab, content);
        finishGO.GetComponent<Button>().onClick.AddListener(ShowResult);
    }

    /// <summary>
    /// Подсвечивает правильные ответы, показывает статистику и добавляет кнопку «Меню», которая просто закрывает тест.
    /// </summary>
    private void ShowResult()
    {
        int correctCount = 0;
        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
        for (int i = 0; i < groups.Length && i < questions.Count; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            foreach (var t in toggles)
                t.interactable = false;

            int sel = System.Array.FindIndex(toggles, t => t.isOn);
            if (sel == questions[i].correctIndex)
                correctCount++;

            int correctIdx = questions[i].correctIndex;
            if (correctIdx >= 0 && correctIdx < toggles.Length)
            {
                var label = toggles[correctIdx].GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = Color.green;
            }
        }

        // Скрываем кнопку Finish
        var content = scrollRect.content;
        int finishIndex = content.childCount - 1;
        content.GetChild(finishIndex).gameObject.SetActive(false);

        // Статистика
        var statsGO = new GameObject("StatsText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        statsGO.transform.SetParent(content, false);
        var statsText = statsGO.GetComponent<TextMeshProUGUI>();
        statsText.text = $"Правильно {correctCount} из {questions.Count}";
        statsText.fontSize = 64;
        statsText.color = Color.black;
        statsText.alignment = TextAlignmentOptions.Center;
        statsGO.transform.SetSiblingIndex(finishIndex);

        // Кнопка «Меню» — просто закрывает тестовую панель
        var menuBtn = Instantiate(menuButtonPrefab, content);
        menuBtn.GetComponentInChildren<TMP_Text>().text = "Меню";
        menuBtn.onClick.RemoveAllListeners();
        menuBtn.onClick.AddListener(() =>
        {
            testPanelParent.SetActive(false);
        });
        menuBtn.transform.SetSiblingIndex(finishIndex + 1);
    }
}
