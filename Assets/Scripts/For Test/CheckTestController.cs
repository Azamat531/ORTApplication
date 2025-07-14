//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using UnityEngine.Networking;

//public class CheckTestController : MonoBehaviour
//{
//    [Header("Data")]
//    public CheckTestQuestions questionsData;

//    [Header("Panels")]
//    public GameObject beginImage;
//    public GameObject loadingPanel;
//    public GameObject testPanel;
//    public GameObject resultPanel;

//    [Header("Quiz Setup")]
//    public ScrollRect scrollRect;
//    public GameObject questionEntryPrefab;
//    public GameObject finishButtonPrefab;
//    public TextMeshProUGUI resultPanelText;
//    public Image resultPanelImage;

//    [Header("Start Test Button")]
//    public Button startTestButton; // кнопка "Начать тест", привязывается в инспекторе

//    private List<QuestionData> questions = new();

//    [System.Serializable]
//    public class QuestionData
//    {
//        public string questionText;
//        public string imageUrl;
//        [HideInInspector] public Sprite imageSprite;
//        public List<string> options;
//        public int correctIndex;
//    }

//    void Start()
//    {
//        if (startTestButton != null)
//            startTestButton.onClick.AddListener(StartButtonPressed);

//        beginImage.SetActive(true);
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(false);
//        resultPanel.SetActive(false);
//    }

//    public void StartTest(CoursesData.Subject subject, CoursesData.Topic topic)
//    {
//        beginImage.SetActive(true);
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(false);
//        resultPanel.SetActive(false);

//        questions = new List<QuestionData>();

//        foreach (var subj in questionsData.subjects)
//        {
//            if (subj.name == subject.name)
//            {
//                foreach (var top in subj.topics)
//                {
//                    if (top.name == topic.name)
//                    {
//                        foreach (var q in top.questions)
//                        {
//                            questions.Add(new QuestionData
//                            {
//                                questionText = q.questionText,
//                                imageUrl = q.imageUrl,
//                                options = q.options,
//                                correctIndex = q.correctIndex
//                            });
//                        }
//                        return;
//                    }
//                }
//            }
//        }

//        Debug.LogWarning($"[CheckTestController] Вопросы не найдены для темы '{topic.name}' в предмете '{subject.name}'");
//    }

//    public void StartButtonPressed()
//    {
//        beginImage.SetActive(false);
//        loadingPanel.SetActive(true);
//        StartCoroutine(PreloadAllImages());
//    }

//    private IEnumerator PreloadAllImages()
//    {
//        foreach (var q in questions)
//        {
//            if (q.imageSprite == null && !string.IsNullOrEmpty(q.imageUrl))
//            {
//                using var uwr = UnityWebRequestTexture.GetTexture(q.imageUrl);
//                yield return uwr.SendWebRequest();
//                if (uwr.result == UnityWebRequest.Result.Success)
//                {
//                    var tex = DownloadHandlerTexture.GetContent(uwr);
//                    q.imageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
//                }
//            }
//        }

//        PopulateQuestions();
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(true);
//    }

//    private void PopulateQuestions()
//    {
//        var content = scrollRect.content;
//        foreach (Transform c in content) Destroy(c.gameObject);

//        foreach (var q in questions)
//        {
//            var go = Instantiate(questionEntryPrefab, content);
//            go.GetComponent<QuestionEntry>().Setup(q.questionText, q.imageSprite, q.options);
//        }

//        var btnGO = Instantiate(finishButtonPrefab, content);
//        btnGO.GetComponent<Button>().onClick.AddListener(ShowResult);
//    }

//    private void ShowResult()
//    {
//        int correct = 0;
//        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
//        for (int i = 0; i < groups.Length; i++)
//        {
//            var toggles = groups[i].GetComponentsInChildren<Toggle>();
//            int sel = System.Array.FindIndex(toggles, t => t.isOn);
//            if (sel == questions[i].correctIndex) correct++;
//        }

//        testPanel.SetActive(false);

//        if (correct >= 8)
//        {
//            resultPanelImage.color = new Color32(76, 175, 80, 255);
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nӨтө жакшы жыйынтык!!!\nКийинки темага өтө берсеңиз болот";
//        }
//        else if (correct >= 5)
//        {
//            resultPanelImage.color = new Color32(255, 193, 7, 255);
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nОрточо жыйынтык.\nТеманы үйрөнүшүңүз керек";
//        }
//        else
//        {
//            resultPanelImage.color = new Color32(244, 67, 54, 255);
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nНачар жыйынтык.\nТеманы үйрөнүшүңүз керек";
//        }

//        resultPanel.SetActive(true);
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using UnityEngine.Networking;

//public class CheckTestController : MonoBehaviour
//{
//    [Header("Data")]
//    public CheckTestQuestions questionsData;

//    [Header("Panels")]
//    public GameObject beginImage;
//    public GameObject loadingPanel;
//    public GameObject testPanel;
//    public GameObject resultPanel;
//    public GameObject subtopicsPanel;

//    [Header("Quiz Setup")]
//    public ScrollRect scrollRect;
//    public GameObject questionEntryPrefab;
//    public GameObject finishButtonPrefab;
//    public TextMeshProUGUI resultPanelText;
//    public Image resultPanelImage;
//    public Button skipButton;

//    private List<QuestionData> questions = new();

//    [System.Serializable]
//    public class QuestionData
//    {
//        public string questionText;
//        public string imageUrl;
//        [HideInInspector] public Sprite imageSprite;
//        public List<string> options;
//        public int correctIndex;
//    }

//    void Start()
//    {
//        beginImage.SetActive(true);
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(false);
//        resultPanel.SetActive(false);
//        subtopicsPanel.SetActive(false);

//        if (skipButton != null)
//        {
//            skipButton.onClick.RemoveAllListeners();
//            skipButton.onClick.AddListener(SkipButtonPressed);
//        }
//    }

//    public void StartTest(CoursesData.Subject subject, CoursesData.Topic topic)
//    {
//        beginImage.SetActive(true);
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(false);
//        resultPanel.SetActive(false);
//        subtopicsPanel.SetActive(false);

//        questions = new List<QuestionData>();

//        foreach (var subj in questionsData.subjects)
//        {
//            if (subj.name == subject.name)
//            {
//                foreach (var top in subj.topics)
//                {
//                    if (top.name == topic.name)
//                    {
//                        foreach (var q in top.questions)
//                        {
//                            questions.Add(new QuestionData
//                            {
//                                questionText = q.questionText,
//                                imageUrl = q.imageUrl,
//                                options = q.options,
//                                correctIndex = q.correctIndex
//                            });
//                        }
//                        return;
//                    }
//                }
//            }
//        }

//        Debug.LogWarning($"[CheckTestController] Вопросы не найдены для темы '{topic.name}' в предмете '{subject.name}'");
//    }

//    public void StartButtonPressed()
//    {
//        beginImage.SetActive(false);
//        loadingPanel.SetActive(true);
//        StartCoroutine(PreloadAllImages());
//    }

//    public void SkipButtonPressed()
//    {
//        beginImage.SetActive(false);
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(false);
//        resultPanel.SetActive(false);
//        subtopicsPanel.SetActive(true);
//    }

//    private IEnumerator PreloadAllImages()
//    {
//        foreach (var q in questions)
//        {
//            if (q.imageSprite == null && !string.IsNullOrEmpty(q.imageUrl))
//            {
//                using var uwr = UnityWebRequestTexture.GetTexture(q.imageUrl);
//                yield return uwr.SendWebRequest();
//                if (uwr.result == UnityWebRequest.Result.Success)
//                {
//                    var tex = DownloadHandlerTexture.GetContent(uwr);
//                    q.imageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
//                }
//            }
//        }

//        PopulateQuestions();
//        loadingPanel.SetActive(false);
//        testPanel.SetActive(true);
//    }

//    private void PopulateQuestions()
//    {
//        var content = scrollRect.content;
//        foreach (Transform c in content) Destroy(c.gameObject);

//        foreach (var q in questions)
//        {
//            var go = Instantiate(questionEntryPrefab, content);
//            go.GetComponent<QuestionEntry>().Setup(q.questionText, q.imageSprite, q.options);
//        }

//        var btnGO = Instantiate(finishButtonPrefab, content);
//        btnGO.GetComponent<Button>().onClick.AddListener(ShowResult);
//    }

//    private void ShowResult()
//    {
//        int correct = 0;
//        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
//        for (int i = 0; i < groups.Length; i++)
//        {
//            var toggles = groups[i].GetComponentsInChildren<Toggle>();
//            int sel = System.Array.FindIndex(toggles, t => t.isOn);
//            if (sel == questions[i].correctIndex) correct++;
//        }

//        testPanel.SetActive(false);

//        if (correct >= 8)
//        {
//            resultPanelImage.color = new Color32(76, 175, 80, 255);
//            resultPanelText.text = $"Отлично! {correct}/{questions.Count}";
//        }
//        else if (correct >= 5)
//        {
//            resultPanelImage.color = new Color32(255, 193, 7, 255);
//            resultPanelText.text = $"Неплохо: {correct}/{questions.Count}";
//        }
//        else
//        {
//            resultPanelImage.color = new Color32(244, 67, 54, 255);
//            resultPanelText.text = $"Попробуй снова: {correct}/{questions.Count}";
//        }

//        resultPanel.SetActive(true);
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class CheckTestController : MonoBehaviour
{
    [Header("Data")]
    public CheckTestQuestions questionsData;

    [Header("Panels")]
    public GameObject beginImage;
    public GameObject loadingPanel;
    public GameObject testPanel;
    public GameObject resultPanel;
    public GameObject subtopicsPanel;

    [Header("Buttons")]
    public Button skipButton;
    public Button startButton;

    [Header("Quiz Setup")]
    public ScrollRect scrollRect;
    public GameObject questionEntryPrefab;
    public GameObject finishButtonPrefab;
    public TextMeshProUGUI resultPanelText;
    public Image resultPanelImage;

    private List<QuestionData> questions = new();

    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public string imageUrl;
        [HideInInspector] public Sprite imageSprite;
        public List<string> options;
        public int correctIndex;
    }

    public void StartTest(CoursesData.Subject subject, CoursesData.Topic topic)
    {
        beginImage.SetActive(true);
        loadingPanel.SetActive(false);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);
        subtopicsPanel.SetActive(false);

        questions = new List<QuestionData>();

        foreach (var subj in questionsData.subjects)
        {
            if (subj.name == subject.name)
            {
                foreach (var top in subj.topics)
                {
                    if (top.name == topic.name)
                    {
                        foreach (var q in top.questions)
                        {
                            questions.Add(new QuestionData
                            {
                                questionText = q.questionText,
                                imageUrl = q.imageUrl,
                                options = q.options,
                                correctIndex = q.correctIndex
                            });
                        }
                        break;
                    }
                }
                break;
            }
        }

        Debug.LogWarning($"[CheckTestController] Загружено вопросов: {questions.Count}");
    }

    void Start()
    {
        beginImage.SetActive(true);
        loadingPanel.SetActive(false);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);
        subtopicsPanel.SetActive(false);

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(SkipButtonPressed);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartButtonPressed);
    }

    public void StartButtonPressed()
    {
        beginImage.SetActive(false);
        loadingPanel.SetActive(true);
        StartCoroutine(PreloadAllImages());
    }

    public void SkipButtonPressed()
    {
        beginImage.SetActive(false);
        loadingPanel.SetActive(false);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);
        subtopicsPanel.SetActive(true);
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
                    q.imageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
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
            go.GetComponent<QuestionEntry>().Setup(q.questionText, q.imageSprite, q.options);
        }

        var btnGO = Instantiate(finishButtonPrefab, content);
        btnGO.GetComponent<Button>().onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        int correct = 0;
        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            int sel = System.Array.FindIndex(toggles, t => t.isOn);
            if (sel == questions[i].correctIndex) correct++;
        }

        testPanel.SetActive(false);

        if (correct >= 8)
        {
            resultPanelImage.color = new Color32(76, 175, 80, 255);
            resultPanelText.text = $"Отлично! {correct}/{questions.Count}";
        }
        else if (correct >= 5)
        {
            resultPanelImage.color = new Color32(255, 193, 7, 255);
            resultPanelText.text = $"Неплохо: {correct}/{questions.Count}";
        }
        else
        {
            resultPanelImage.color = new Color32(244, 67, 54, 255);
            resultPanelText.text = $"Попробуй снова: {correct}/{questions.Count}";
        }

        resultPanel.SetActive(true);
    }
}
