//using System;
//using System.Collections;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class PracticeOneQuestionPanel : MonoBehaviour
//{
//    [Header("Firebase Storage")]
//    public string bucketName = "first-5b828.firebasestorage.app";
//    public string contentFolder = "practise";

//    [Header("UI")]
//    public GameObject panelRoot;
//    public TMP_Text headerText;
//    public ScrollRect scrollRect;
//    public GameObject questionEntryPrefab;

//    [Header("Prefabs")]
//    public GameObject answerButtonPrefab;
//    public GameObject nextButtonPrefab;

//    [Header("Navigation")]
//    public Button backButton;
//    public UnityEngine.Events.UnityEvent onClosed;

//    [Header("Feedback")]
//    public TMP_Text resultLabel;
//    public GameObject loadingPanel;

//    [Header("Points UI")]
//    public TMP_Text pointsLabel;

//    [Header("Options")]
//    public bool verbose = false;

//    // �������� �����������: subjectId, topicId, ����� �������� �����
//    public Action<string, string, int> onPointsChanged;

//    private string _subjectId, _topicId, _topicName;
//    private int _pickedQ = -1;
//    private int _correctIndex = -1;
//    private QuestionEntry _entry;
//    private Button _answerBtn;
//    private Button _nextBtn;

//    [Serializable] private class AnswerEntry { public int q; public string correct; }
//    [Serializable] private class AnswersRootObject { public System.Collections.Generic.List<AnswerEntry> answers; }
//    [Serializable] private class AnswersRootStrings { public System.Collections.Generic.List<string> answers; }
//    [Serializable] private class WrapArr { public System.Collections.Generic.List<string> items; }

//    public void Open(string subjectId, string topicId, string topicName)
//    {
//        _subjectId = subjectId; _topicId = topicId; _topicName = topicName;

//        if (panelRoot) panelRoot.SetActive(true);
//        if (headerText) headerText.text = topicName;
//        if (resultLabel) { resultLabel.text = string.Empty; resultLabel.gameObject.SetActive(false); }

//        if (backButton)
//        {
//            backButton.onClick.RemoveAllListeners();
//            backButton.onClick.AddListener(Close);
//        }

//        if (scrollRect && scrollRect.content)
//        {
//            for (int i = scrollRect.content.childCount - 1; i >= 0; i--)
//                Destroy(scrollRect.content.GetChild(i).gameObject);
//        }

//        _entry = null; _pickedQ = -1; _correctIndex = -1; _answerBtn = null; _nextBtn = null;

//        // �������� ��������� ���� �����
//        int ptsNow = PointsService.GetPoints(_subjectId, _topicId);
//        if (pointsLabel) pointsLabel.text = $"���� �� ����: {ptsNow}";

//        StartCoroutine(LoadRandomQuestionRoutine());
//    }

//    public void Close()
//    {
//        if (panelRoot) panelRoot.SetActive(false);
//        if (scrollRect && scrollRect.content)
//        {
//            for (int i = scrollRect.content.childCount - 1; i >= 0; i--)
//                Destroy(scrollRect.content.GetChild(i).gameObject);
//        }
//        _entry = null; _pickedQ = -1; _correctIndex = -1; _answerBtn = null; _nextBtn = null;
//        if (onClosed != null) onClosed.Invoke();
//    }

//    private IEnumerator LoadRandomQuestionRoutine()
//    {
//        SetLoading(true);

//        string ansRel = $"{contentFolder}/{_subjectId}/{_topicId}/answers.json";
//        string ansUrl = MakeUrl(ansRel);

//        string raw = null; bool done = false;
//        yield return CacheService.GetText(
//            ansUrl,
//            "json:" + ansRel,
//            t => { raw = t; done = true; },
//            e => { done = true; }
//        );
//        if (!done) yield return null;

//        var entries = ParseAnswerEntries(raw);
//        if (entries == null || entries.Count == 0)
//        {
//            SetLoading(false);
//            yield break;
//        }

//        var entry = entries[UnityEngine.Random.Range(0, entries.Count)];
//        _pickedQ = entry.q;
//        _correctIndex = LetterToIndex(entry.correct);
//        if (_correctIndex < 0) _correctIndex = 0;

//        string imgRel = $"{contentFolder}/{_subjectId}/{_topicId}/{_pickedQ}";
//        string imgUrl = MakeUrl(imgRel);

//        Sprite sprite = null;
//        bool imgDone = false;
//        yield return CacheService.GetTexture(
//            imgUrl,
//            "img:" + imgUrl,
//            (Sprite s) => { sprite = s; imgDone = true; },
//            2048,
//            (string e) => { imgDone = true; }
//        );
//        if (!imgDone) yield return null;

//        if (scrollRect && scrollRect.content && questionEntryPrefab)
//        {
//            var go = Instantiate(questionEntryPrefab, scrollRect.content);
//            _entry = go.GetComponent<QuestionEntry>();
//            if (_entry != null)
//            {
//                var options = new System.Collections.Generic.List<string> { "�", "�", "�", "�", "�" };
//                _entry.Setup("", sprite, options);
//                var img = _entry.GetComponentInChildren<Image>();
//                if (img) img.preserveAspect = true;
//            }
//        }

//        if (scrollRect && scrollRect.content && answerButtonPrefab)
//        {
//            var btnGO = Instantiate(answerButtonPrefab, scrollRect.content);
//            _answerBtn = btnGO.GetComponent<Button>();
//            if (_answerBtn)
//            {
//                _answerBtn.onClick.RemoveAllListeners();
//                _answerBtn.onClick.AddListener(OnCheck);
//                _answerBtn.interactable = true;
//            }
//            var label = btnGO.GetComponentInChildren<TMP_Text>();
//            if (label) label.text = "��������";
//        }

//        SetLoading(false);
//    }

//    private void OnCheck()
//    {
//        if (_entry == null || _correctIndex < 0) return;

//        var group = _entry.GetComponentInChildren<ToggleGroup>();
//        if (!group) return;

//        var toggles = group.GetComponentsInChildren<Toggle>();
//        int sel = Array.FindIndex(toggles, t => t.isOn);
//        bool ok = (sel == _correctIndex);

//        // ���������
//        for (int i = 0; i < toggles.Length; i++)
//        {
//            var tgl = toggles[i];
//            if (i == _correctIndex) tgl.targetGraphic.color = new Color(0.8f, 1f, 0.8f);
//            if (!ok && sel == i) tgl.targetGraphic.color = new Color(1f, 0.85f, 0.85f);
//        }

//        if (_answerBtn)
//        {
//            _answerBtn.gameObject.SetActive(false);
//            _answerBtn.interactable = false;
//        }

//        if (resultLabel)
//        {
//            string letter = IndexToLetter(_correctIndex);
//            resultLabel.text = ok ? $"�����! ���������� �����: {letter}" : $"�������. ���������� �����: {letter}";
//            resultLabel.gameObject.SetActive(true);
//        }

//        // ���������� ��������� ������
//        int delta = ok ? +10 : -5;
//        int newPts = PointsService.AddPoints(_subjectId, _topicId, delta);

//        if (pointsLabel) pointsLabel.text = $"���� �� ����: {newPts}";
//        onPointsChanged?.Invoke(_subjectId, _topicId, newPts);

//        // ������ ���������� ������
//        if (scrollRect && scrollRect.content && nextButtonPrefab)
//        {
//            var btnGO = Instantiate(nextButtonPrefab, scrollRect.content);
//            _nextBtn = btnGO.GetComponent<Button>();
//            if (_nextBtn)
//            {
//                _nextBtn.onClick.RemoveAllListeners();
//                _nextBtn.onClick.AddListener(() => { Open(_subjectId, _topicId, _topicName); });
//                _nextBtn.interactable = true;
//            }
//            var label = btnGO.GetComponentInChildren<TMP_Text>();
//            if (label) label.text = "��������� ������";
//        }
//    }

//    private System.Collections.Generic.List<AnswerEntry> ParseAnswerEntries(string raw)
//    {
//        if (string.IsNullOrEmpty(raw)) return null;
//        try { var obj = JsonUtility.FromJson<AnswersRootObject>(raw); if (obj?.answers?.Count > 0) return obj.answers; } catch { }
//        try
//        {
//            var arr = JsonUtility.FromJson<WrapArr>("{\"items\":" + raw + "}");
//            if (arr?.items?.Count > 0)
//            {
//                var list = new System.Collections.Generic.List<AnswerEntry>(arr.items.Count);
//                for (int i = 0; i < arr.items.Count; i++) list.Add(new AnswerEntry { q = i + 1, correct = arr.items[i] });
//                return list;
//            }
//        }
//        catch { }
//        try
//        {
//            var str = JsonUtility.FromJson<AnswersRootStrings>(raw);
//            if (str?.answers?.Count > 0)
//            {
//                var list = new System.Collections.Generic.List<AnswerEntry>(str.answers.Count);
//                for (int i = 0; i < str.answers.Count; i++) list.Add(new AnswerEntry { q = i + 1, correct = str.answers[i] });
//                return list;
//            }
//        }
//        catch { }
//        return null;
//    }

//    private static int LetterToIndex(string letter)
//    {
//        if (string.IsNullOrEmpty(letter)) return -1;
//        switch (letter.Trim().ToUpper())
//        {
//            case "�": return 0;
//            case "�": return 1;
//            case "�": return 2;
//            case "�": return 3;
//            case "�": return 4;
//            default: return -1;
//        }
//    }
//    private static string IndexToLetter(int i)
//    {
//        switch (i) { case 0: return "�"; case 1: return "�"; case 2: return "�"; case 3: return "�"; case 4: return "�"; default: return "?"; }
//    }

//    private string MakeUrl(string rel)
//    {
//        var enc = UnityEngine.Networking.UnityWebRequest.EscapeURL(rel).Replace("+", "%20");
//        return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{enc}?alt=media";
//    }
//    private void SetLoading(bool state)
//    {
//        if (loadingPanel) loadingPanel.SetActive(state);
//    }
//}

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

    public void Open(string subjectId, string topicId, string topicName)
    {
        _subjectId = subjectId; _topicId = topicId; _topicName = topicName;

        if (panelRoot) panelRoot.SetActive(true);
        if (headerText) headerText.text = topicName;
        if (resultLabel) { resultLabel.text = string.Empty; resultLabel.gameObject.SetActive(false); }

        // �������� ��������� ���� �����
        int ptsNow = PointsService.GetPoints(_subjectId, _topicId);
        if (pointsLabel) pointsLabel.text = $"���� �� ����: {ptsNow}";

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
            try { PracticeOneQuestionToast.Show("������� ������"); } catch { }
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
            try { PracticeOneQuestionToast.Show("������� ������"); } catch { }
            Close();
            yield break;
        }

        if (scrollRect && scrollRect.content && questionEntryPrefab)
        {
            var go = Instantiate(questionEntryPrefab, scrollRect.content);
            _entry = go.GetComponent<QuestionEntry>();
            if (_entry != null)
            {
                var options = new System.Collections.Generic.List<string> { "�", "�", "�", "�", "�" };
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
            if (label) label.text = "������??";
        }

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
            resultLabel.text = ok ? $"�����! ���������� �����: {letter}" : $"�������. ���������� �����: {letter}";
            resultLabel.gameObject.SetActive(true);
        }

        // ���������� ��������� ������
        int delta = ok ? +10 : -5;
        int newPts = PointsService.AddPoints(_subjectId, _topicId, delta);

        if (pointsLabel) pointsLabel.text = $"���� �� ����: {newPts}";
        onPointsChanged?.Invoke(_subjectId, _topicId, newPts);

        // ������ ���������� ������
        if (scrollRect && scrollRect.content && nextButtonPrefab)
        {
            var btnGO = Instantiate(nextButtonPrefab, scrollRect.content);
            _nextBtn = btnGO.GetComponent<Button>();
            if (_nextBtn)
            {
                _nextBtn.onClick.RemoveAllListeners();
                _nextBtn.onClick.AddListener(() => { Open(_subjectId, _topicId, _topicName); });
                _nextBtn.interactable = true;
            }
            var label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "��������� ������";
        }
    }

    private static int LetterToIndex(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        s = s.Trim().ToUpperInvariant();
        switch (s)
        {
            case "A": case "�": return 0;
            case "B": case "�": return 1;
            case "C": case "�": return 2;
            case "D": case "�": return 3;
            case "E": case "�": return 4;
        }
        return -1;
    }
    private static string IndexToLetter(int i)
    {
        switch (i) { case 0: return "�"; case 1: return "�"; case 2: return "�"; case 3: return "�"; case 4: return "�"; default: return "?"; }
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

    // JSON ������ answers.json (��������� ���� ��������: [{q, correct}] � ["A","B",...])
    private System.Collections.Generic.List<AnswerEntry> ParseAnswerEntries(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        // ������� 1: ������ �������� {q, correct}
        try
        {
            var wrapped = WrapIfArray(raw, "answers");
            var o = JsonUtility.FromJson<AnswersRootObject>(wrapped);
            if (o != null && o.answers != null && o.answers.Count > 0) return o.answers;
        }
        catch { }

        // ������� 2: ������ ����� ["A","B",...]
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

// ---- Local lightweight toast (updated: bigger, black text, longer) ----
public static class PracticeOneQuestionToast
{
    // ���������� �� ���������
    public static void Show(string message, float duration = 2.8f)
    {
        if (string.IsNullOrEmpty(message)) return;
        var runnerGO = new GameObject("[PracticeToastRunner]");
        var beh = runnerGO.AddComponent<UnityEngine.MonoBehaviourProxy>();
        UnityEngine.Object.DontDestroyOnLoad(runnerGO);
        beh.StartCoroutine(ShowRoutine(message, duration, beh, runnerGO));
    }

    private static System.Collections.IEnumerator ShowRoutine(
        string message, float duration, UnityEngine.MonoBehaviour beh, UnityEngine.GameObject runnerGO)
    {
        var canvas = FindTopCanvas();
        if (canvas == null)
        {
            var goCanvas = new UnityEngine.GameObject("ToastCanvas",
                typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = goCanvas.GetComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            var scaler = goCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new UnityEngine.Vector2(1080, 1920);
            UnityEngine.Object.DontDestroyOnLoad(goCanvas);
        }

        // ���������
        var go = new UnityEngine.GameObject("Toast",
            typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasGroup));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<UnityEngine.RectTransform>();
        rt.anchorMin = new UnityEngine.Vector2(0.5f, 0.15f);
        rt.anchorMax = new UnityEngine.Vector2(0.5f, 0.15f);
        rt.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
        rt.anchoredPosition = UnityEngine.Vector2.zero;

        var group = go.GetComponent<UnityEngine.CanvasGroup>();
        group.alpha = 0f;

        // ������� ���, ����� ������ ����� ��� ������
        var bgGo = new UnityEngine.GameObject("BG",
            typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image));
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.GetComponent<UnityEngine.RectTransform>();
        bgRt.anchorMin = UnityEngine.Vector2.zero; bgRt.anchorMax = UnityEngine.Vector2.one;
        bgRt.offsetMin = UnityEngine.Vector2.zero; bgRt.offsetMax = UnityEngine.Vector2.zero;
        var bg = bgGo.GetComponent<UnityEngine.UI.Image>();
        bg.color = new UnityEngine.Color(1f, 1f, 1f, 0.92f); // ����� ~92%
        bg.raycastTarget = false;

        // ����� � ������� � ������
        var txGo = new UnityEngine.GameObject("Text",
            typeof(UnityEngine.RectTransform), typeof(TMPro.TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
        txGo.transform.SetParent(go.transform, false);
        var tx = txGo.GetComponent<TMPro.TextMeshProUGUI>();
        tx.text = message;
        tx.alignment = TMPro.TextAlignmentOptions.Midline;
        tx.enableAutoSizing = true;
        tx.fontSizeMin = 36;   // ���� 28
        tx.fontSizeMax = 60;   // ���� 48
        tx.fontSize = 54;      // ���������
        tx.color = UnityEngine.Color.black; // ������ �����
        tx.raycastTarget = false;

        // ��������/������
        var pad = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        pad.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
        pad.padding = new UnityEngine.RectOffset(40, 40, 26, 26);

        var fitter = go.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        // ��������
        float t = 0f, fade = 0.18f;
        while (t < fade) { t += UnityEngine.Time.unscaledDeltaTime; group.alpha = UnityEngine.Mathf.SmoothStep(0, 1, t / fade); yield return null; }
        yield return new UnityEngine.WaitForSecondsRealtime(duration < 0.5f ? 0.5f : duration);
        t = 0f;
        while (t < fade) { t += UnityEngine.Time.unscaledDeltaTime; group.alpha = UnityEngine.Mathf.SmoothStep(1, 0, t / fade); yield return null; }

        UnityEngine.Object.Destroy(go);
        UnityEngine.Object.Destroy(runnerGO);
    }

    private static UnityEngine.Canvas FindTopCanvas()
    {
        UnityEngine.Canvas best = null; int bestOrder = int.MinValue;
        foreach (var c in UnityEngine.Object.FindObjectsOfType<UnityEngine.Canvas>())
        {
            if (!c.isActiveAndEnabled) continue;
            if (c.renderMode == UnityEngine.RenderMode.WorldSpace) continue;
            if (c.sortingOrder >= bestOrder) { best = c; bestOrder = c.sortingOrder; }
        }
        return best;
    }
}

// Helper to run coroutines from a static context
namespace UnityEngine
{
    public class MonoBehaviourProxy : MonoBehaviour { }
}
