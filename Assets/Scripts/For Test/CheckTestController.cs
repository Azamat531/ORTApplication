using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class CheckTestController : MonoBehaviour
{
    [Header("Startup Panel")]
    [SerializeField] private GameObject beginImage;    // BeginImage

    [Header("Panels")]
    [SerializeField] private GameObject loadingPanel;  // LoadingPanel
    [SerializeField] private GameObject testPanel;     // QuestionsScrollView (сам тест)
    [SerializeField] private GameObject resultPanel;   // ResultPanel

    [Header("Quiz Setup")]
    [SerializeField] private ScrollRect scrollRect;              // компонент на QuestionsScrollView
    [SerializeField] private GameObject questionEntryPrefab;     // ваш префаб QuestionEntry
    [SerializeField] private GameObject finishButtonPrefab;      // префаб кнопки “Закончить”
    [SerializeField] private TextMeshProUGUI resultPanelText;    // TMP внутри ResultPanel
    [SerializeField] private Image resultPanelImage;             // Image-фон внутри ResultPanel
    [SerializeField] private List<QuestionData> questions;       // список вопросов

    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public string imageUrl;
        [HideInInspector] public Sprite imageSprite;
        public List<string> options;
        public int correctIndex;
    }

    void Start()
    {
        // Изначально показываем только стартовый экран
        beginImage.SetActive(true);
        loadingPanel.SetActive(false);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    // Привяжите этот метод к кнопке “Начать тест” на BeginImage
    public void StartTest()
    {
        beginImage.SetActive(false);
        loadingPanel.SetActive(true);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);

        StartCoroutine(PreloadAllImages());
    }

    private IEnumerator PreloadAllImages()
    {
        // Загружаем картинки
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
                        tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                }
            }
        }

        // Заполняем вопросы + кнопку “Закончить”
        PopulateQuestions();

        // Переключаем панели
        loadingPanel.SetActive(false);
        testPanel.SetActive(true);
    }

    private void PopulateQuestions()
    {
        var content = scrollRect.content;
        foreach (Transform c in content) Destroy(c.gameObject);

        // Вопросы
        for (int i = 0; i < questions.Count; i++)
        {
            var data = questions[i];
            var go = Instantiate(questionEntryPrefab, content);
            go.GetComponent<QuestionEntry>()
              .Setup(data.questionText, data.imageSprite, data.options);
        }

        // Кнопка “Закончить”
        var btnGO = Instantiate(finishButtonPrefab, content);
        btnGO.GetComponent<Button>().onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        // Подсчёт
        int correct = 0;
        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            int sel = System.Array.FindIndex(toggles, t => t.isOn);
            if (sel == questions[i].correctIndex) correct++;

        }

        // Скрываем тест
        testPanel.SetActive(false);

        // Цвет + текст результата
        if (correct >= 8)
        {
            resultPanelImage.color = new Color32(76, 175, 80, 255);
            resultPanelText.color = Color.white;
            resultPanelText.text = $"Отлично! {correct}/{questions.Count}";
        }
        else if (correct >= 5)
        {
            resultPanelImage.color = new Color32(255, 193, 7, 255);
            resultPanelText.color = new Color32(0x33, 0x33, 0x33, 255);
            resultPanelText.text = $"Неплохо: {correct}/{questions.Count}";
        }
        else
        {
            resultPanelImage.color = new Color32(244, 67, 54, 255);
            resultPanelText.color = Color.white;
            resultPanelText.text = $"Попробуйте ещё: {correct}/{questions.Count}";
        }

        resultPanel.SetActive(true);
    }
}
