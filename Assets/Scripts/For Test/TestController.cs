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
//        public string imageUrl;     // без расширения — CacheService сам подберёт
//        public string correctAnswer;// "А"/"Б"/"В"/"Г"/"Д"
//    }

//    // API
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

//    // === Ограниченная параллельность загрузок ===
//    private IEnumerator PreloadAllImagesConcurrent(int maxConcurrent)
//    {
//        int next = 0; int active = 0; int total = questions.Count;
//        bool[] doneFlags = new bool[total];

//        System.Action<int> startOne = null;
//        startOne = (int i) =>
//        {
//            if (i >= total) return; active++;
//            // >>> важное изменение: используем Sprite-перегрузку с maxSize <<<
//            StartCoroutine(CacheService.GetTexture(
//                questions[i].imageUrl,
//                cacheKey: "img:" + questions[i].imageUrl,
//                onDone: sprite => { questions[i].imageSprite = sprite; doneFlags[i] = true; active--; },
//                maxSize: 0,
//                onError: e => { Debug.LogWarning(e); doneFlags[i] = true; active--; }
//            ));
//        };

//        while (active < maxConcurrent && next < total) { startOne(next++); }

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
//            entry.Setup("", q.imageSprite, q.options);
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
    public GameObject questionEntryPrefab;  // должен иметь компонент QuestionEntry
    public Button finishButtonPrefab;       // Button на корне
    public Button menuButtonPrefab;         // опционально
    public Button solutionButtonPrefab;     // опционально

    [Header("Solution Video")]
    public GameObject solutionVideoPanel;
    public VideoStreamPlayer solutionVideoPlayer;

    public System.Action ExitRequested;

    // событие результата теста (correct, total)
    public System.Action<int, int> TestFinished;

    private readonly List<Question> questions = new List<Question>();
    private string solutionVideoUrl;

    [System.Serializable]
    private class Question
    {
        public string imageUrl;
        [HideInInspector] public Sprite imageSprite;
        public List<string> options;
        public int correctIndex;
    }

    [System.Serializable]
    public class RemoteQuestion
    {
        public string imageUrl;
        public string correctAnswer; // "А"/"Б"/"В"/"Г"/"Д"
    }

    void Awake()
    {
        Debug.Log("[TestController] Awake on '" + gameObject.name + "' (instanceID=" + GetInstanceID() + ")", this);
    }

    void OnValidate()
    {
        if (scrollRect == null) Debug.LogWarning("[TestController] ScrollRect не назначен", this);
        else if (scrollRect.content == null) Debug.LogWarning("[TestController] ScrollRect.content не назначен", scrollRect);

        if (questionEntryPrefab == null) Debug.LogWarning("[TestController] questionEntryPrefab не назначен", this);
        if (finishButtonPrefab == null) Debug.LogWarning("[TestController] finishButtonPrefab не назначен", this);
    }

    // API
    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions) { InternalStart(remoteQuestions, true, null); }
    public void StartTestFromRemote(List<RemoteQuestion> remoteQuestions, string solutionUrl) { InternalStart(remoteQuestions, false, solutionUrl); }
    public void SetSolutionVideoUrl(string url) { solutionVideoUrl = url; }

    private void InternalStart(List<RemoteQuestion> remoteQuestions, bool tryGuessSolution, string explicitSolutionUrl)
    {
        if (remoteQuestions == null || remoteQuestions.Count == 0)
        {
            Debug.LogError("[TestController] Пустой список вопросов");
            return;
        }

        if (!ValidateRequiredRefs(true)) return;

        if (!string.IsNullOrEmpty(explicitSolutionUrl)) solutionVideoUrl = explicitSolutionUrl;
        if (loadingPanel) loadingPanel.SetActive(true);
        if (testPanel) testPanel.SetActive(true);
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        questions.Clear();
        var letterOptions = new List<string> { "А", "Б", "В", "Г", "Д" };

        for (int i = 0; i < remoteQuestions.Count; i++)
        {
            var rq = remoteQuestions[i];
            int idx = LetterToIndex(rq.correctAnswer);
            var q = new Question();
            q.imageUrl = rq.imageUrl;
            q.options = letterOptions;
            q.correctIndex = idx;
            questions.Add(q);
        }

        if (tryGuessSolution && string.IsNullOrEmpty(solutionVideoUrl))
        {
            var guess = TryGuessSolutionUrl(remoteQuestions);
            if (!string.IsNullOrEmpty(guess)) solutionVideoUrl = guess;
        }

        StartCoroutine(PreloadAllImagesConcurrent(3));
    }

    private static int LetterToIndex(string letter)
    {
        if (string.IsNullOrEmpty(letter)) return -1;
        string up = letter.Trim().ToUpper();
        if (up == "А") return 0;
        if (up == "Б") return 1;
        if (up == "В") return 2;
        if (up == "Г") return 3;
        if (up == "Д") return 4;
        return -1;
    }

    private static string TryGuessSolutionUrl(List<RemoteQuestion> remoteQuestions)
    {
        string first = remoteQuestions[0].imageUrl;
        if (string.IsNullOrEmpty(first)) return null;

        int q = first.IndexOf('?');
        string baseUrl = (q >= 0) ? first.Substring(0, q) : first;
        string query = (q >= 0) ? first.Substring(q) : "";

        if (baseUrl.EndsWith("/v1")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 3);
        return baseUrl + "/v2" + query;
    }

    private IEnumerator PreloadAllImagesConcurrent(int maxConcurrent)
    {
        int index = 0, completed = 0;
        var running = new List<IEnumerator>();

        while (completed < questions.Count)
        {
            while (running.Count < maxConcurrent && index < questions.Count)
            {
                int i = index++;
                IEnumerator e = LoadImage(i);
                running.Add(e);
                StartCoroutine(Wrap(e, () => { completed++; running.Remove(e); }));
            }
            yield return null;
        }

        if (!ValidateRequiredRefs(true))
        {
            if (loadingPanel) loadingPanel.SetActive(false);
            yield break;
        }

        PopulateQuestions();

        if (loadingPanel) loadingPanel.SetActive(false);
    }

    private IEnumerator Wrap(IEnumerator inner, System.Action onDone) { yield return inner; if (onDone != null) onDone(); }

    private IEnumerator LoadImage(int i)
    {
        var q = questions[i];
        Sprite sp = null;

        // из кэша
        string enc = CacheService.GetCachedPath("img:" + q.imageUrl, ".png");
        if (string.IsNullOrEmpty(enc)) enc = CacheService.GetCachedPath("img:" + q.imageUrl, ".jpg");
        if (string.IsNullOrEmpty(enc)) enc = CacheService.GetCachedPath("img:" + q.imageUrl, ".jpeg");
        if (string.IsNullOrEmpty(enc)) enc = CacheService.GetCachedPath("img:" + q.imageUrl, ".webp");

        if (!string.IsNullOrEmpty(enc))
        {
            string plain = CacheService.GetOrMakePlainTemp(enc, ".png");
            if (!string.IsNullOrEmpty(plain) && System.IO.File.Exists(plain))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(plain);
                var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                if (tex.LoadImage(bytes))
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // онлайн
        if (sp == null)
        {
            yield return CacheService.GetTexture(q.imageUrl, "img:" + q.imageUrl,
                texture =>
                {
                    if (texture != null)
                        sp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                },
                err => Debug.LogWarning("[TestController] Не удалось загрузить " + q.imageUrl + ": " + err));
        }

        q.imageSprite = sp;
    }

    private void PopulateQuestions()
    {
        if (!ValidateRequiredRefs(true)) return;

        var content = scrollRect.content;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        DebugDumpRefs("[PopulateQuestions]");

        if (questionEntryPrefab == null)
        {
            Debug.LogError("[TestController] questionEntryPrefab == null");
            return;
        }
        if (finishButtonPrefab == null)
        {
            Debug.LogError("[TestController] finishButtonPrefab == null");
            return;
        }

        // вопросы
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var go = Object.Instantiate(questionEntryPrefab, content);
            var entry = go.GetComponent<QuestionEntry>();
            if (entry == null)
            {
                Debug.LogError("[TestController] На questionEntryPrefab нет компонента QuestionEntry", go);
                continue;
            }
            entry.Setup("", q.imageSprite, q.options);
        }

        // кнопка Завершить
        var finishGO = Object.Instantiate(finishButtonPrefab, content);
        var finishBtn = finishGO.GetComponent<Button>();
        finishBtn.onClick.RemoveAllListeners();
        finishBtn.onClick.AddListener(ShowResult);
    }

    private void ShowResult()
    {
        if (!ValidateRequiredRefs(false)) return;

        int correctCount = 0;
        var content = scrollRect.content;
        var groups = content.GetComponentsInChildren<ToggleGroup>();

        for (int i = 0; i < groups.Length && i < questions.Count; i++)
        {
            var toggles = groups[i].GetComponentsInChildren<Toggle>();
            for (int t = 0; t < toggles.Length; t++) toggles[t].interactable = false;

            int sel = -1;
            for (int t = 0; t < toggles.Length; t++) if (toggles[t].isOn) { sel = t; break; }
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
        statsText.text = "Правильно " + correctCount + " из " + questions.Count;
        statsText.fontSize = 32;
        statsText.alignment = TextAlignmentOptions.Center;
        statsGO.transform.SetSiblingIndex(finishIndex > 0 ? finishIndex : 0);

        // наружу
        if (TestFinished != null) TestFinished(correctCount, questions.Count);

        // меню
        if (menuButtonPrefab != null)
        {
            var menuBtn = Object.Instantiate(menuButtonPrefab, content);
            var txt = menuBtn.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = "Меню";
            menuBtn.onClick.RemoveAllListeners();
            menuBtn.onClick.AddListener(ExitToMenu);
        }

        // решение
        if (!string.IsNullOrEmpty(solutionVideoUrl) && solutionVideoPanel && solutionVideoPlayer && solutionButtonPrefab != null)
        {
            var solBtn = Object.Instantiate(solutionButtonPrefab, content);
            var t = solBtn.GetComponentInChildren<TMP_Text>();
            if (t) t.text = "Решение";
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
        if (ExitRequested != null) ExitRequested();
    }

    // сервис
    private bool ValidateRequiredRefs(bool forBuildUI)
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogError("[TestController] ScrollRect или его Content не назначены", this);
            return false;
        }
        if (forBuildUI)
        {
            if (questionEntryPrefab == null)
            {
                Debug.LogError("[TestController] questionEntryPrefab не назначен", this);
                return false;
            }
            if (finishButtonPrefab == null)
            {
                Debug.LogError("[TestController] finishButtonPrefab не назначен", this);
                return false;
            }
        }
        return true;
    }

    private void DebugDumpRefs(string tag)
    {
        string s =
            tag + " on '" + gameObject.name + "' (id=" + GetInstanceID() + ")\n" +
            "scrollRect=" + (scrollRect ? scrollRect.name : "NULL") +
            ", content=" + (scrollRect && scrollRect.content ? scrollRect.content.name : "NULL") + "\n" +
            "questionEntryPrefab=" + (questionEntryPrefab ? questionEntryPrefab.name : "NULL") +
            ", finishButtonPrefab=" + (finishButtonPrefab ? finishButtonPrefab.name : "NULL") +
            ", menuButtonPrefab=" + (menuButtonPrefab ? menuButtonPrefab.name : "NULL") +
            ", solutionButtonPrefab=" + (solutionButtonPrefab ? solutionButtonPrefab.name : "NULL") + "\n" +
            "solutionVideoPanel=" + (solutionVideoPanel ? solutionVideoPanel.name : "NULL") +
            ", solutionVideoPlayer=" + (solutionVideoPlayer ? solutionVideoPlayer.name : "NULL");
        Debug.Log(s, this);
    }
}
