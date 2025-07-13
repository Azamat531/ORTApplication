//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;
//using TMPro;

//public class TestController : MonoBehaviour
//{
//    [Header("Quiz Setup")]
//    [SerializeField] private ScrollRect scrollRect;
//    [SerializeField] private GameObject questionEntryPrefab;
//    [SerializeField] private GameObject finishButtonPrefab;
//    [SerializeField] private List<QuestionData> questions;

//    [Header("Result Panel")]
//    [SerializeField] private GameObject resultPanel;
//    [SerializeField] private Image resultPanelImage;
//    [SerializeField] private TextMeshProUGUI resultPanelText;

//    [System.Serializable]
//    public class QuestionData
//    {
//        public string questionText;
//        public string imageUrl;
//        [HideInInspector] public Sprite imageSprite;
//        public List<string> options;
//        public int correctIndex;
//    }

//    private void Start()
//    {
//        // Скрываем панель результата до завершения теста
//        resultPanel.SetActive(false);
//        StartCoroutine(PreloadAllImages());
//    }

//    private IEnumerator PreloadAllImages()
//    {
//        for (int i = 0; i < questions.Count; i++)
//        {
//            if (questions[i].imageSprite != null) continue;
//            using var uwr = UnityWebRequestTexture.GetTexture(questions[i].imageUrl);
//            yield return uwr.SendWebRequest();
//            if (uwr.result == UnityWebRequest.Result.Success)
//            {
//                var tex = DownloadHandlerTexture.GetContent(uwr);
//                questions[i].imageSprite =
//                    Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
//            }
//        }
//        PopulateQuestions();
//    }

//    private void PopulateQuestions()
//    {
//        var content = scrollRect.content;
//        foreach (Transform child in content)
//            Destroy(child.gameObject);

//        for (int i = 0; i < questions.Count; i++)
//        {
//            var data = questions[i];
//            var entryGO = Instantiate(questionEntryPrefab, content);
//            entryGO.GetComponent<QuestionEntry>()
//                   .Setup(data.questionText, data.imageSprite, data.options);
//        }

//        // Добавляем кнопку "Закончить" в конец списка
//        var btnGO = Instantiate(finishButtonPrefab, content);
//        btnGO.GetComponent<Button>()
//             .onClick.AddListener(ShowResult);
//    }

//    private void ShowResult()
//    {
//        // Подсчёт правильных ответов
//        int correct = 0;
//        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
//        for (int i = 0; i < groups.Length; i++)
//        {
//            var toggles = groups[i].GetComponentsInChildren<Toggle>();
//            int selected = System.Array.FindIndex(toggles, t => t.isOn);
//            if (selected >= 0 && selected == questions[i].correctIndex)
//                correct++;
//        }

//        // Скрываем тестовую панель
//        scrollRect.gameObject.SetActive(false);

//        // Выбор цвета и текста результата
//        if (correct >= 8)
//        {
//            resultPanelImage.color = new Color32(76, 175, 80, 255);
//            resultPanelText.color = Color.white;
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nӨтө жакшы жыйынтык!!!\nКийинки темага өтө берсеңиз болот";
//        }
//        else if (correct >= 5)
//        {
//            resultPanelImage.color = new Color32(255, 193, 7, 255);
//            resultPanelText.color = new Color32(0x33, 0x33, 0x33, 255);
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nОрточо жыйынтык.\nТеманы үйрөнүшүңүз керек";
//        }
//        else
//        {
//            resultPanelImage.color = new Color32(244, 67, 54, 255);
//            resultPanelText.color = Color.white;
//            resultPanelText.text = $"{questions.Count} суроодон {correct} туура.\nНачар жыйынтык.\nТеманы үйрөнүшүңүз керек";
//        }

//        // Отображаем панель результата
//        resultPanel.SetActive(true);
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class TestController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loadingPanel;     // LoadingPanel
    [SerializeField] private GameObject testPanel;        // TestPanel
    [SerializeField] private GameObject resultPanel;      // ResultPanel

    [Header("Quiz Setup")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject questionEntryPrefab;
    [SerializeField] private GameObject finishButtonPrefab;
    [SerializeField] private TextMeshProUGUI resultPanelText;
    [SerializeField] private Image resultPanelImage;
    [SerializeField] private List<QuestionData> questions;

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
        // Включаем только LoadingPanel
        loadingPanel.SetActive(true);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);

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
                        tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                }
            }
        }

        PopulateQuestions();

        // Переключаемся на тест
        loadingPanel.SetActive(false);
        testPanel.SetActive(true);
    }

    private void PopulateQuestions()
    {
        var content = scrollRect.content;
        foreach (Transform ch in content) Destroy(ch.gameObject);

        for (int i = 0; i < questions.Count; i++)
        {
            var data = questions[i];
            var go = Instantiate(questionEntryPrefab, content);
            go.GetComponent<QuestionEntry>()
              .Setup(data.questionText, data.imageSprite, data.options);
        }

        var btnGO = Instantiate(finishButtonPrefab, content);
        btnGO.GetComponent<Button>()
             .onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        // Считаем правильные
        int correct = 0;
        var groups = scrollRect.content.GetComponentsInChildren<ToggleGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            int selected = System.Array.FindIndex(toggles, t => t.isOn);
            if (selected == questions[i].correctIndex)
                correct++;
        }

        // Скрываем тестовую панель
        testPanel.SetActive(false);

        // Выбираем цвет и текст результата
        if (correct >= 8)
        {
            resultPanelImage.color = new Color32(76, 175, 80, 255);
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
            resultPanelText.text = $"Попробуйте ещё: {correct}/{questions.Count}";
        }

        resultPanel.SetActive(true);
    }
}
