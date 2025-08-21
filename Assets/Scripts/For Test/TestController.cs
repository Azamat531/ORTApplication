// ============================================
// File: Assets/Scripts/For Test/TestController.cs
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TestController : MonoBehaviour
{
    [Header("UI Panels")] public GameObject loadingPanel; public GameObject testPanel; public GameObject testPanelParent;
    [Header("Quiz Setup")] public ScrollRect scrollRect; public GameObject questionEntryPrefab; public GameObject finishButtonPrefab;
    [Header("Navigation")] public Button menuButtonPrefab;

    // внешний колбэк
    public System.Action ExitRequested;

    private readonly List<QuestionData> questions = new List<QuestionData>();

    [System.Serializable] public class QuestionData { public string imageUrl; [HideInInspector] public Sprite imageSprite; public List<string> options; public int correctIndex; }
    [System.Serializable] public class RemoteQuestion { public string imageUrl; public string correctAnswer; }

    public void StartTestFromRemote(string subtopicTitle, List<RemoteQuestion> remoteQuestions) { StartTestFromRemote(remoteQuestions); }
    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions)
    {
        if (loadingPanel) loadingPanel.SetActive(true); if (testPanel) testPanel.SetActive(true); if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        questions.Clear(); var letterOptions = new List<string> { "А", "Б", "В", "Г", "Д" };
        foreach (var rq in remoteQuestions)
        { int idx = LetterToIndex(rq.correctAnswer); if (idx < 0 || idx > 4) idx = 0; questions.Add(new QuestionData { imageUrl = rq.imageUrl, options = letterOptions, correctIndex = idx }); }
        StartCoroutine(PreloadAllImages());
    }

    public void StartTest(CoursesData.Subject a, CoursesData.Topic b, CoursesData.Subtopic c) { Debug.LogWarning("Legacy StartTest called. Use StartTestFromRemote."); }

    private static int LetterToIndex(string letter)
    { if (string.IsNullOrEmpty(letter)) return -1; switch (letter.Trim().ToUpper()) { case "А": return 0; case "Б": return 1; case "В": return 2; case "Г": return 3; case "Д": return 4; default: return -1; } }

    private IEnumerator PreloadAllImages()
    {
        foreach (var q in questions)
        {
            if (q.imageSprite == null && !string.IsNullOrEmpty(q.imageUrl))
            {
                bool done = false;
                yield return CacheService.GetTexture(q.imageUrl, "img:" + q.imageUrl, sprite => { q.imageSprite = sprite; done = true; }, onError: e => { Debug.LogWarning(e); done = true; });
                if (!done) yield return null;
            }
        }
        PopulateQuestions(); if (loadingPanel) loadingPanel.SetActive(false); if (testPanel) testPanel.SetActive(true);
    }

    private void PopulateQuestions()
    {
        var content = scrollRect.content; for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        foreach (var q in questions)
        { var go = Instantiate(questionEntryPrefab, content); go.GetComponent<QuestionEntry>().Setup("", q.imageSprite, q.options); }
        var finishGO = Instantiate(finishButtonPrefab, content); finishGO.GetComponent<Button>().onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        int correctCount = 0; var content = scrollRect.content; var groups = content.GetComponentsInChildren<ToggleGroup>();
        for (int i = 0; i < groups.Length && i < questions.Count; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>(); foreach (var t in toggles) t.interactable = false;
            int sel = System.Array.FindIndex(toggles, t => t.isOn); if (sel == questions[i].correctIndex) correctCount++;
            int correctIdx = questions[i].correctIndex; if (correctIdx >= 0 && correctIdx < toggles.Length)
            { var label = toggles[correctIdx].GetComponentInChildren<TMP_Text>(); if (label != null) label.color = Color.green; }
        }
        int finishIndex = content.childCount - 1; if (finishIndex >= 0) content.GetChild(finishIndex).gameObject.SetActive(false);
        var statsGO = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); statsGO.transform.SetParent(content, false);
        var statsText = statsGO.GetComponent<TextMeshProUGUI>(); statsText.text = $"Правильно {correctCount} из {questions.Count}"; statsText.fontSize = 64; statsText.color = Color.black; statsText.alignment = TextAlignmentOptions.Center; statsGO.transform.SetSiblingIndex(Mathf.Max(finishIndex, 0));

        var menuBtn = Instantiate(menuButtonPrefab, content); menuBtn.GetComponentInChildren<TMP_Text>().text = "Меню"; menuBtn.onClick.RemoveAllListeners(); menuBtn.onClick.AddListener(ExitToMenu);
    }

    private void ExitToMenu()
    {
        if (testPanelParent) testPanelParent.SetActive(false);
        ExitRequested?.Invoke();
    }
}
