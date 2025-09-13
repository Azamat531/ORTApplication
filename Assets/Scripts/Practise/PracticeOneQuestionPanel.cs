using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PracticeOneQuestionPanel : MonoBehaviour
{
    [Header("Firebase Storage")]
    public string bucketName = "first-5b828.firebasestorage.app";
    public string contentFolder = "practise";

    [Header("UI")]
    public GameObject panelRoot;
    public TMP_Text headerText;
    public ScrollRect scrollRect;
    public GameObject loadingPanel;
    public GameObject questionEntryPrefab;
    public GameObject answerButtonPrefab;
    public GameObject nextButtonPrefab;
    public TMP_Text resultLabel;
    public TMP_Text pointsLabel;
    public Button backButton;               // Кнопка «Назад»

    [Header("Events")]
    public Action onClosed;
    public Action<string, string, int> onPointsChanged; // (subjectId, topicId, points)

    private string _subjectId;
    private string _topicId;
    private string _topicName;
    private int _pickedQ = -1;
    private int _correctIndex = -1;

    private QuestionEntry _entry;
    private Button _answerBtn;
    private Button _nextBtn;

    [Serializable] private class AnswerEntry { public int q; public string correct; }
    [Serializable] private class AnswersRootObject { public System.Collections.Generic.List<AnswerEntry> answers; }
    [Serializable] private class AnswersRootStrings { public System.Collections.Generic.List<string> answers; }

    // --- Новое для динамических очков ---
    [Header("Scoring")]
    [Tooltip("После этого времени (сек) даём минимальные очки")]
    public float maxAnswerTime = 90f;
    private float _questionStartTime;

    private void Awake()
    {
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }
    }

    private void OnDestroy()
    {
        if (backButton) backButton.onClick.RemoveListener(Close);
    }

    public void Open(string subjectId, string topicId, string topicName)
    {
        _subjectId = subjectId; _topicId = topicId; _topicName = topicName;

        if (panelRoot) panelRoot.SetActive(true);
        if (headerText) headerText.text = topicName;
        if (resultLabel) { resultLabel.text = string.Empty; resultLabel.gameObject.SetActive(false); }

        // показать очки сразу
        int ptsNow = PointsService.GetPoints(_subjectId, _topicId);
        if (pointsLabel) pointsLabel.text = $"Очки по теме: {ptsNow}";

        // очистить старый контент, чтобы новый вопрос был «вместо», а не «снизу»
        ClearContent();

        StartCoroutine(LoadRandomQuestionRoutine());
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);

        if (scrollRect && scrollRect.content)
        {
            for (int i = scrollRect.content.childCount - 1; i >= 0; i--)
                Destroy(scrollRect.content.GetChild(i).gameObject);
        }

        _entry = null; _pickedQ = -1; _correctIndex = -1; _answerBtn = null; _nextBtn = null;
        onClosed?.Invoke();
    }

    private void ClearContent()
    {
        if (scrollRect && scrollRect.content)
        {
            for (int i = scrollRect.content.childCount - 1; i >= 0; i--)
                Destroy(scrollRect.content.GetChild(i).gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
        _entry = null; _answerBtn = null; _nextBtn = null;
        _pickedQ = -1; _correctIndex = -1;
    }

    private IEnumerator LoadRandomQuestionRoutine()
    {
        SetLoading(true);

        string ansRel = $"{contentFolder}/{_subjectId}/{_topicId}/answers.json";
        string ansUrl = MakeUrl(ansRel);

        string raw = null; bool done = false;
        yield return CacheService.GetText(
            ansUrl,
            "json:" + ansRel,
            t => { raw = t; done = true; },
            e => { done = true; }
        );
        if (!done) yield return null;

        var entries = ParseAnswerEntries(raw);
        if (entries == null || entries.Count == 0)
        {
            SetLoading(false);
            Close();
            yield break;
        }

        var entry = entries[UnityEngine.Random.Range(0, entries.Count)];
        _pickedQ = entry.q;
        _correctIndex = LetterToIndex(entry.correct);
        if (_correctIndex < 0) _correctIndex = 0;

        string imgRel = $"{contentFolder}/{_subjectId}/{_topicId}/{_pickedQ}";
        string imgUrl = MakeUrl(imgRel);

        Sprite sprite = null;
        bool imgDone = false;
        yield return CacheService.GetTexture(
            imgUrl,
            "img:" + imgUrl,
            (Sprite s) => { sprite = s; imgDone = true; },
            2048,
            (string e) => { imgDone = true; }
        );
        if (!imgDone) yield return null;

        if (sprite == null)
        {
            SetLoading(false);
            Close();
            yield break;
        }

        if (scrollRect && scrollRect.content && questionEntryPrefab)
        {
            var go = Instantiate(questionEntryPrefab, scrollRect.content);
            _entry = go.GetComponent<QuestionEntry>();
            if (_entry != null)
            {
                var options = new System.Collections.Generic.List<string> { "А", "Б", "В", "Г", "Д" };
                _entry.Setup("", sprite, options);

                var img = _entry.GetComponentInChildren<Image>();
                if (img) img.preserveAspect = true;
            }
        }

        if (scrollRect && scrollRect.content && answerButtonPrefab)
        {
            var btnGO = Instantiate(answerButtonPrefab, scrollRect.content);
            _answerBtn = btnGO.GetComponent<Button>();
            if (_answerBtn)
            {
                _answerBtn.onClick.RemoveAllListeners();
                _answerBtn.onClick.AddListener(OnCheck);
                _answerBtn.interactable = true;
            }
            var label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "Текшер??";
        }

        // старт таймера для расчёта очков
        _questionStartTime = Time.realtimeSinceStartup;

        SetLoading(false);
    }

    private void OnCheck()
    {
        if (_entry == null) return;
        int picked = _entry.GetPickedIndex();
        bool ok = picked == _correctIndex;

        _entry.ShowResult(_correctIndex, picked);

        if (_answerBtn) _answerBtn.interactable = false;

        if (resultLabel)
        {
            string letter = IndexToLetter(_correctIndex);
            resultLabel.text = ok ? $"Верно! Правильный ответ: {letter}" : $"Неверно. Правильный ответ: {letter}";
            resultLabel.gameObject.SetActive(true);
        }

        // ===== ДИНАМИЧЕСКИЕ ОЧКИ по времени ответа =====
        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _questionStartTime);
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, maxAnswerTime));

        int delta;
        if (ok)
            delta = Mathf.RoundToInt(Mathf.Lerp(20f, 7f, t));          // быстрее ? ближе к +15, медленнее ? к +5
        else
            delta = -Mathf.RoundToInt(Mathf.Lerp(4f, 11f, t));          // быстрее ошибка ? -3, медленнее ? -9

        int newPts = PointsService.AddPoints(_subjectId, _topicId, delta);
        if (pointsLabel) pointsLabel.text = $"Очки по теме: {newPts}";
        onPointsChanged?.Invoke(_subjectId, _topicId, newPts);
        // ================================================

        // Следующий вопрос — заменяем контент на месте
        if (scrollRect && scrollRect.content && nextButtonPrefab)
        {
            var btnGO = Instantiate(nextButtonPrefab, scrollRect.content);
            _nextBtn = btnGO.GetComponent<Button>();
            if (_nextBtn)
            {
                _nextBtn.onClick.RemoveAllListeners();
                _nextBtn.onClick.AddListener(() =>
                {
                    if (resultLabel) { resultLabel.text = string.Empty; resultLabel.gameObject.SetActive(false); }
                    ClearContent();
                    StartCoroutine(LoadRandomQuestionRoutine());
                });
                _nextBtn.interactable = true;
            }
            var label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "Следующий вопрос";
        }
    }

    // ---- utils ----
    private static int LetterToIndex(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        s = s.Trim().ToUpperInvariant();
        switch (s)
        {
            case "A": case "А": return 0;
            case "B": case "Б": return 1;
            case "C": case "В": return 2;
            case "D": case "Г": return 3;
            case "E": case "Д": return 4;
        }
        return -1;
    }
    private static string IndexToLetter(int i)
    {
        switch (i) { case 0: return "А"; case 1: return "Б"; case 2: return "В"; case 3: return "Г"; case 4: return "Д"; default: return "?"; }
    }

    private string MakeUrl(string rel)
    {
        var enc = UnityEngine.Networking.UnityWebRequest.EscapeURL(rel).Replace("+", "%20");
        return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{enc}?alt=media";
    }
    private void SetLoading(bool state)
    {
        if (loadingPanel) loadingPanel.SetActive(state);
        if (scrollRect) scrollRect.gameObject.SetActive(!state);
    }

    // JSON: поддержка [{q,correct}] и ["A","B",...]
    private System.Collections.Generic.List<AnswerEntry> ParseAnswerEntries(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        try
        {
            var wrapped = WrapIfArray(raw, "answers");
            var o = JsonUtility.FromJson<AnswersRootObject>(wrapped);
            if (o != null && o.answers != null && o.answers.Count > 0) return o.answers;
        }
        catch { }

        try
        {
            var wrapped = WrapIfArray(raw, "answers");
            var s = JsonUtility.FromJson<AnswersRootStrings>(wrapped);
            if (s != null && s.answers != null && s.answers.Count > 0)
            {
                var list = new System.Collections.Generic.List<AnswerEntry>();
                for (int i = 0; i < s.answers.Count; i++)
                    list.Add(new AnswerEntry { q = i + 1, correct = s.answers[i] });
                return list;
            }
        }
        catch { }

        return null;
    }

    private static string WrapIfArray(string raw, string prop)
    {
        raw = raw.Trim();
        if (raw.StartsWith("{")) return raw;
        if (raw.StartsWith("[")) return $"{{\"{prop}\":{raw}}}";
        return raw;
    }
}

