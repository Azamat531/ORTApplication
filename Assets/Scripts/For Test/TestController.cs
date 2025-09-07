//// ============================================
//// File: Assets/Scripts/For Test/TestController.cs
//// Firebase-only test builder: imageUrl + correctAnswer (letters А–Д)
//// Optimized: image preload with limited concurrency (max 3)
//// Keeps solution video flow (v2) with dedicated panel
//// ============================================
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;

//public class TestController : MonoBehaviour
//{
//    [Header("UI Panels")]
//    public GameObject loadingPanel;
//    public GameObject testPanel;
//    public GameObject testPanelParent;

//    [Header("Quiz Setup")]
//    public ScrollRect scrollRect;
//    public GameObject questionEntryPrefab;
//    public GameObject finishButtonPrefab;

//    [Header("Navigation Buttons")]
//    public Button menuButtonPrefab;
//    public Button solutionButtonPrefab;

//    [Header("Solution Video Target")]
//    public GameObject solutionVideoPanel;
//    public VideoStreamPlayer solutionVideoPlayer;

//    public System.Action ExitRequested;

//    private readonly List<QuestionData> questions = new List<QuestionData>();
//    private string solutionVideoUrl;

//    [System.Serializable]
//    public class QuestionData
//    {
//        public string imageUrl;
//        [HideInInspector] public Sprite imageSprite;
//        public List<string> options;
//        public int correctIndex;
//    }

//    [System.Serializable]
//    public class RemoteQuestion
//    {
//        public string imageUrl;     // без расширения картинки — CacheService сам подберёт
//        public string correctAnswer;// "А"/"Б"/"В"/"Г"/"Д"
//    }

//    // API (совместим со старым кодом)
//    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions) => InternalStart(remoteQuestions, true, null);
//    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions, string solutionUrl) => InternalStart(remoteQuestions, true, solutionUrl);
//    public void SetSolutionVideoUrl(string url) => solutionVideoUrl = url;

//    private void InternalStart(List<RemoteQuestion> remoteQuestions, bool tryGuessSolution, string explicitSolutionUrl)
//    {
//        if (remoteQuestions == null || remoteQuestions.Count == 0)
//        {
//            Debug.LogError("[TestController] Пустой список вопросов");
//            return;
//        }

//        if (!string.IsNullOrEmpty(explicitSolutionUrl)) solutionVideoUrl = explicitSolutionUrl;
//        if (loadingPanel) loadingPanel.SetActive(true);
//        if (testPanel) testPanel.SetActive(true);
//        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

//        questions.Clear();
//        var letterOptions = new List<string> { "А", "Б", "В", "Г", "Д" };

//        foreach (var rq in remoteQuestions)
//        {
//            int idx = LetterToIndex(rq.correctAnswer);
//            if (idx < 0 || idx > 4) idx = 0;
//            questions.Add(new QuestionData
//            {
//                imageUrl = rq.imageUrl,
//                options = letterOptions,
//                correctIndex = idx
//            });
//        }

//        if (tryGuessSolution && string.IsNullOrEmpty(solutionVideoUrl))
//        {
//            var guess = TryGuessSolutionUrl(remoteQuestions);
//            if (!string.IsNullOrEmpty(guess)) solutionVideoUrl = guess;
//        }

//        StartCoroutine(PreloadAllImagesConcurrent(maxConcurrent: 3));
//    }

//    private static int LetterToIndex(string letter)
//    {
//        if (string.IsNullOrEmpty(letter)) return -1;
//        switch (letter.Trim().ToUpper())
//        {
//            case "А": return 0;
//            case "Б": return 1;
//            case "В": return 2;
//            case "Г": return 3;
//            case "Д": return 4;
//            default: return -1;
//        }
//    }

//    // Попытка угадать v2 по пути первого вопроса (если урл не передали явно)
//    private static string TryGuessSolutionUrl(List<RemoteQuestion> remoteQuestions)
//    {
//        if (remoteQuestions == null || remoteQuestions.Count == 0) return null;
//        var url = remoteQuestions[0]?.imageUrl; if (string.IsNullOrEmpty(url)) return null;

//        int idxO = url.IndexOf("/o/"); if (idxO < 0) return null;
//        int qmark = url.IndexOf('?');
//        string prefix = url.Substring(0, idxO + 3);
//        string encodedPath = qmark >= 0 ? url.Substring(idxO + 3, qmark - (idxO + 3)) : url.Substring(idxO + 3);
//        string query = qmark >= 0 ? url.Substring(qmark) : string.Empty;

//        string decoded = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(encodedPath);
//        int lastSlash = decoded.LastIndexOf('/'); if (lastSlash < 0 || lastSlash + 2 > decoded.Length) return null;
//        if (decoded[lastSlash + 1] != 'q') return null;
//        for (int i = lastSlash + 2; i < decoded.Length; i++) if (!char.IsDigit(decoded[i])) return null;

//        string replaced = decoded.Substring(0, lastSlash + 1) + "v2";
//        string reencoded = UnityEngine.Networking.UnityWebRequest.EscapeURL(replaced).Replace("+", "%20");
//        return prefix + reencoded + query;
//    }

//    // === Ограниченная параллельность загрузок (экономит время и память) ===
//    private IEnumerator PreloadAllImagesConcurrent(int maxConcurrent)
//    {
//        int next = 0; int active = 0; int total = questions.Count;
//        bool[] doneFlags = new bool[total];

//        System.Action<int> startOne = null;
//        startOne = (int i) =>
//        {
//            if (i >= total) return; active++;
//            StartCoroutine(CacheService.GetTexture(
//                questions[i].imageUrl,
//                cacheKey: "img:" + questions[i].imageUrl,
//                onDone: sprite => { questions[i].imageSprite = sprite; doneFlags[i] = true; active--; },
//                onError: e => { Debug.LogWarning(e); doneFlags[i] = true; active--; }
//            ));
//        };

//        // запустить первые задачи
//        while (active < maxConcurrent && next < total) { startOne(next++); }

//        // ждать до завершения всех
//        while (true)
//        {
//            bool all = true; for (int i = 0; i < total; i++) if (!doneFlags[i]) { all = false; break; }
//            if (all) break;

//            while (active < maxConcurrent && next < total) { startOne(next++); }
//            yield return null;
//        }

//        PopulateQuestions();
//        if (loadingPanel) loadingPanel.SetActive(false);
//        if (testPanel) testPanel.SetActive(true);
//    }

//    private void PopulateQuestions()
//    {
//        var content = scrollRect.content;
//        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

//        foreach (var q in questions)
//        {
//            var go = Object.Instantiate(questionEntryPrefab, content);
//            var entry = go.GetComponent<QuestionEntry>();
//            entry.Setup("", q.imageSprite, q.options); // текст вопроса не используем — только картинка + буквы
//        }

//        var finishGO = Object.Instantiate(finishButtonPrefab, content);
//        finishGO.GetComponent<Button>().onClick.AddListener(ShowResult);
//    }

//    private void ShowResult()
//    {
//        int correctCount = 0;
//        var content = scrollRect.content;
//        var groups = content.GetComponentsInChildren<ToggleGroup>();

//        for (int i = 0; i < groups.Length && i < questions.Count; i++)
//        {
//            var toggles = groups[i].GetComponentsInChildren<Toggle>();
//            foreach (var t in toggles) t.interactable = false;

//            int sel = System.Array.FindIndex(toggles, t => t.isOn);
//            if (sel == questions[i].correctIndex) correctCount++;

//            int correctIdx = questions[i].correctIndex;
//            if (correctIdx >= 0 && correctIdx < toggles.Length)
//            {
//                var label = toggles[correctIdx].GetComponentInChildren<TMP_Text>();
//                if (label != null) label.color = Color.green;
//            }
//        }

//        int finishIndex = content.childCount - 1;
//        if (finishIndex >= 0) content.GetChild(finishIndex).gameObject.SetActive(false);

//        var statsGO = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
//        statsGO.transform.SetParent(content, false);
//        var statsText = statsGO.GetComponent<TextMeshProUGUI>();
//        statsText.text = $"Правильно {correctCount} из {questions.Count}";
//        statsText.fontSize = 64; statsText.color = Color.black; statsText.alignment = TextAlignmentOptions.Center;
//        statsGO.transform.SetSiblingIndex(Mathf.Max(finishIndex, 0));

//        // Меню + Решение
//        var menuBtn = Object.Instantiate(menuButtonPrefab, content);
//        menuBtn.GetComponentInChildren<TMP_Text>().text = "Меню";
//        menuBtn.onClick.RemoveAllListeners();
//        menuBtn.onClick.AddListener(ExitToMenu);

//        if (!string.IsNullOrEmpty(solutionVideoUrl) && solutionVideoPanel && solutionVideoPlayer)
//        {
//            var solBtn = Object.Instantiate(solutionButtonPrefab, content);
//            solBtn.GetComponentInChildren<TMP_Text>().text = "Решение";
//            solBtn.onClick.RemoveAllListeners();
//            solBtn.onClick.AddListener(PlaySolutionVideo);
//        }
//    }

//    private void PlaySolutionVideo()
//    {
//        if (testPanelParent) testPanelParent.SetActive(false);
//        if (solutionVideoPanel) solutionVideoPanel.SetActive(true);
//        if (solutionVideoPlayer && !string.IsNullOrEmpty(solutionVideoUrl))
//            solutionVideoPlayer.SetVideoURL(solutionVideoUrl);
//    }

//    private void ExitToMenu()
//    {
//        if (testPanelParent) testPanelParent.SetActive(false);
//        ExitRequested?.Invoke();
//    }
//}

// ============================================
// File: Assets/Scripts/For Test/TestController.cs
// Firebase-only test builder: imageUrl + correctAnswer (letters А–Д)
// Optimized: image preload with limited concurrency (max 3)
// Keeps solution video flow (v2) with dedicated panel
// ============================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TestController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loadingPanel;
    public GameObject testPanel;
    public GameObject testPanelParent;

    [Header("Quiz Setup")]
    public ScrollRect scrollRect;
    public GameObject questionEntryPrefab;
    public GameObject finishButtonPrefab;

    [Header("Navigation Buttons")]
    public Button menuButtonPrefab;
    public Button solutionButtonPrefab;

    [Header("Solution Video Target")]
    public GameObject solutionVideoPanel;
    public VideoStreamPlayer solutionVideoPlayer;

    public System.Action ExitRequested;

    private readonly List<QuestionData> questions = new List<QuestionData>();
    private string solutionVideoUrl;

    [System.Serializable]
    public class QuestionData
    {
        public string imageUrl;
        [HideInInspector] public Sprite imageSprite;
        public List<string> options;
        public int correctIndex;
    }

    [System.Serializable]
    public class RemoteQuestion
    {
        public string imageUrl;     // без расширения — CacheService сам подберёт
        public string correctAnswer;// "А"/"Б"/"В"/"Г"/"Д"
    }

    // API
    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions) => InternalStart(remoteQuestions, true, null);
    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions, string solutionUrl) => InternalStart(remoteQuestions, true, solutionUrl);
    public void SetSolutionVideoUrl(string url) => solutionVideoUrl = url;

    private void InternalStart(List<RemoteQuestion> remoteQuestions, bool tryGuessSolution, string explicitSolutionUrl)
    {
        if (remoteQuestions == null || remoteQuestions.Count == 0)
        {
            Debug.LogError("[TestController] Пустой список вопросов");
            return;
        }

        if (!string.IsNullOrEmpty(explicitSolutionUrl)) solutionVideoUrl = explicitSolutionUrl;
        if (loadingPanel) loadingPanel.SetActive(true);
        if (testPanel) testPanel.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        questions.Clear();
        var letterOptions = new List<string> { "А", "Б", "В", "Г", "Д" };

        foreach (var rq in remoteQuestions)
        {
            int idx = LetterToIndex(rq.correctAnswer);
            if (idx < 0 || idx > 4) idx = 0;
            questions.Add(new QuestionData
            {
                imageUrl = rq.imageUrl,
                options = letterOptions,
                correctIndex = idx
            });
        }

        if (tryGuessSolution && string.IsNullOrEmpty(solutionVideoUrl))
        {
            var guess = TryGuessSolutionUrl(remoteQuestions);
            if (!string.IsNullOrEmpty(guess)) solutionVideoUrl = guess;
        }

        StartCoroutine(PreloadAllImagesConcurrent(maxConcurrent: 3));
    }

    private static int LetterToIndex(string letter)
    {
        if (string.IsNullOrEmpty(letter)) return -1;
        switch (letter.Trim().ToUpper())
        {
            case "А": return 0;
            case "Б": return 1;
            case "В": return 2;
            case "Г": return 3;
            case "Д": return 4;
            default: return -1;
        }
    }

    private static string TryGuessSolutionUrl(List<RemoteQuestion> remoteQuestions)
    {
        if (remoteQuestions == null || remoteQuestions.Count == 0) return null;
        var url = remoteQuestions[0]?.imageUrl; if (string.IsNullOrEmpty(url)) return null;

        int idxO = url.IndexOf("/o/"); if (idxO < 0) return null;
        int qmark = url.IndexOf('?');
        string prefix = url.Substring(0, idxO + 3);
        string encodedPath = qmark >= 0 ? url.Substring(idxO + 3, qmark - (idxO + 3)) : url.Substring(idxO + 3);
        string query = qmark >= 0 ? url.Substring(qmark) : string.Empty;

        string decoded = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(encodedPath);
        int lastSlash = decoded.LastIndexOf('/'); if (lastSlash < 0 || lastSlash + 2 > decoded.Length) return null;
        if (decoded[lastSlash + 1] != 'q') return null;
        for (int i = lastSlash + 2; i < decoded.Length; i++) if (!char.IsDigit(decoded[i])) return null;

        string replaced = decoded.Substring(0, lastSlash + 1) + "v2";
        string reencoded = UnityEngine.Networking.UnityWebRequest.EscapeURL(replaced).Replace("+", "%20");
        return prefix + reencoded + query;
    }

    // === Ограниченная параллельность загрузок ===
    private IEnumerator PreloadAllImagesConcurrent(int maxConcurrent)
    {
        int next = 0; int active = 0; int total = questions.Count;
        bool[] doneFlags = new bool[total];

        System.Action<int> startOne = null;
        startOne = (int i) =>
        {
            if (i >= total) return; active++;
            // >>> важное изменение: используем Sprite-перегрузку с maxSize <<<
            StartCoroutine(CacheService.GetTexture(
                questions[i].imageUrl,
                cacheKey: "img:" + questions[i].imageUrl,
                onDone: sprite => { questions[i].imageSprite = sprite; doneFlags[i] = true; active--; },
                maxSize: 0,
                onError: e => { Debug.LogWarning(e); doneFlags[i] = true; active--; }
            ));
        };

        while (active < maxConcurrent && next < total) { startOne(next++); }

        while (true)
        {
            bool all = true; for (int i = 0; i < total; i++) if (!doneFlags[i]) { all = false; break; }
            if (all) break;

            while (active < maxConcurrent && next < total) { startOne(next++); }
            yield return null;
        }

        PopulateQuestions();
        if (loadingPanel) loadingPanel.SetActive(false);
        if (testPanel) testPanel.SetActive(true);
    }

    private void PopulateQuestions()
    {
        var content = scrollRect.content;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        foreach (var q in questions)
        {
            var go = Object.Instantiate(questionEntryPrefab, content);
            var entry = go.GetComponent<QuestionEntry>();
            entry.Setup("", q.imageSprite, q.options);
        }

        var finishGO = Object.Instantiate(finishButtonPrefab, content);
        finishGO.GetComponent<Button>().onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        int correctCount = 0;
        var content = scrollRect.content;
        var groups = content.GetComponentsInChildren<ToggleGroup>();

        for (int i = 0; i < groups.Length && i < questions.Count; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            foreach (var t in toggles) t.interactable = false;

            int sel = System.Array.FindIndex(toggles, t => t.isOn);
            if (sel == questions[i].correctIndex) correctCount++;

            int correctIdx = questions[i].correctIndex;
            if (correctIdx >= 0 && correctIdx < toggles.Length)
            {
                var label = toggles[correctIdx].GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = Color.green;
            }
        }

        int finishIndex = content.childCount - 1;
        if (finishIndex >= 0) content.GetChild(finishIndex).gameObject.SetActive(false);

        var statsGO = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statsGO.transform.SetParent(content, false);
        var statsText = statsGO.GetComponent<TextMeshProUGUI>();
        statsText.text = $"Правильно {correctCount} из {questions.Count}";
        statsText.fontSize = 64; statsText.color = Color.black; statsText.alignment = TextAlignmentOptions.Center;
        statsGO.transform.SetSiblingIndex(Mathf.Max(finishIndex, 0));

        var menuBtn = Object.Instantiate(menuButtonPrefab, content);
        menuBtn.GetComponentInChildren<TMP_Text>().text = "Меню";
        menuBtn.onClick.RemoveAllListeners();
        menuBtn.onClick.AddListener(ExitToMenu);

        if (!string.IsNullOrEmpty(solutionVideoUrl) && solutionVideoPanel && solutionVideoPlayer)
        {
            var solBtn = Object.Instantiate(solutionButtonPrefab, content);
            solBtn.GetComponentInChildren<TMP_Text>().text = "Решение";
            solBtn.onClick.RemoveAllListeners();
            solBtn.onClick.AddListener(PlaySolutionVideo);
        }
    }

    private void PlaySolutionVideo()
    {
        if (testPanelParent) testPanelParent.SetActive(false);
        if (solutionVideoPanel) solutionVideoPanel.SetActive(true);
        if (solutionVideoPlayer && !string.IsNullOrEmpty(solutionVideoUrl))
            solutionVideoPlayer.SetVideoURL(solutionVideoUrl);
    }

    private void ExitToMenu()
    {
        if (testPanelParent) testPanelParent.SetActive(false);
        ExitRequested?.Invoke();
    }
}
