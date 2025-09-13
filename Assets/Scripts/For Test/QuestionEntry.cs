//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class QuestionEntry : MonoBehaviour
//{
//    [SerializeField] private TextMeshProUGUI headerText;
//    [SerializeField] private Image questionImage;
//    [SerializeField] private Transform answersContainer;
//    [SerializeField] private GameObject answerTogglePrefab;

//    private ToggleGroup toggleGroup;
//    private readonly string[] labels = { "�", "�", "�", "�", "�" };

//    public void Setup(string text, Sprite sprite, List<string> options)
//    {
//        // ���������
//        if (headerText != null)
//            headerText.text = text ?? string.Empty;

//        // �������� � ������������� �� ������
//        if (sprite != null && questionImage != null)
//        {
//            questionImage.sprite = sprite;
//            var rt = questionImage.rectTransform;
//            rt.anchorMin = new Vector2(0, 1);
//            rt.anchorMax = new Vector2(1, 1);
//            rt.pivot = new Vector2(0.5f, 1);
//            rt.anchoredPosition = Vector2.zero;
//            float pw = (rt.parent as RectTransform).rect.width;
//            rt.SetSizeWithCurrentAnchors(
//                RectTransform.Axis.Vertical,
//                pw * (sprite.rect.height / sprite.rect.width)
//            );
//        }

//        if (answersContainer == null)
//        {
//            Debug.LogError("QuestionEntry: Answers Container �� ��������!", this);
//            return;
//        }

//        // ToggleGroup (��������� ������ ��� ������)
//        toggleGroup = answersContainer.GetComponent<ToggleGroup>()
//                    ?? answersContainer.gameObject.AddComponent<ToggleGroup>();
//        toggleGroup.allowSwitchOff = true;

//        // ������ ������ ��������
//        foreach (Transform c in answersContainer)
//            Destroy(c.gameObject);

//        // ������ 5 �������. ������� � ������ ����� (�, �, �, �, �)
//        for (int i = 0; i < labels.Length; i++)
//        {
//            var togGO = Instantiate(answerTogglePrefab, answersContainer);
//            var tog = togGO.GetComponent<Toggle>();
//            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();

//            if (lbl != null)
//                lbl.text = labels[i]; // ��� ������������ "�. �"

//            if (tog != null)
//            {
//                tog.group = toggleGroup;
//                tog.isOn = false;
//            }
//        }

//        toggleGroup.SetAllTogglesOff();
//    }
//}



//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class QuestionEntry : MonoBehaviour
//{
//    [Header("UI")]
//    [SerializeField] private TextMeshProUGUI headerText;
//    [SerializeField] private Image questionImage;     // Sprite-������� (���� ���� Image)
//    [SerializeField] private RawImage questionRawImage;  // Texture-������� (���� ���� RawImage)
//    [SerializeField] private Transform answersContainer;
//    [SerializeField] private GameObject answerTogglePrefab;

//    [Header("Colors")]
//    [SerializeField] private Color normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
//    [SerializeField] private Color correctColor = new Color(0.16f, 0.64f, 0.24f, 1f);
//    [SerializeField] private Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 1f);

//    private ToggleGroup toggleGroup;
//    private readonly string[] labels = { "�", "�", "�", "�", "�" };
//    private readonly List<Toggle> _toggles = new List<Toggle>();

//    // === ���������� � ������ ������������� (��� �������� Sprite) ===
//    public void Setup(string text, Sprite sprite, List<string> options)
//    {
//        if (headerText) headerText.text = text ?? string.Empty;

//        bool shown = false;

//        // 1) ������������ Image+Sprite
//        if (sprite != null && questionImage != null)
//        {
//            questionImage.enabled = true;
//            questionImage.color = Color.white;
//            questionImage.preserveAspect = true;
//            questionImage.sprite = sprite;
//            FitToParentWidth(questionImage.rectTransform, sprite.rect.width, sprite.rect.height);
//            shown = true;
//        }

//        // 2) ���� ��� Image, �� ���� RawImage � ���� �������
//        if (!shown && sprite != null && sprite.texture != null && questionRawImage != null)
//        {
//            questionRawImage.enabled = true;
//            questionRawImage.color = Color.white;
//            questionRawImage.texture = sprite.texture;
//            FitToParentWidth(questionRawImage.rectTransform, sprite.texture.width, sprite.texture.height);
//            shown = true;
//        }

//        if (!shown) HideImages();

//        BuildAnswers();
//    }

//    /// <summary>����� ������� ������ (0..4), ���� -1.</summary>
//    public int GetPickedIndex()
//    {
//        for (int i = 0; i < _toggles.Count; i++)
//            if (_toggles[i] && _toggles[i].isOn) return i;
//        return -1;
//    }

//    /// <summary>���������� ��������� � ������������� ���������� �����.</summary>
//    public void ShowResult(int correctIndex, int pickedIndex)
//    {
//        for (int i = 0; i < _toggles.Count; i++)
//        {
//            var t = _toggles[i];
//            if (!t) continue;

//            if (i == correctIndex) SetToggleColor(t, correctColor);
//            else if (i == pickedIndex) SetToggleColor(t, wrongColor);
//            else SetToggleColor(t, normalColor);

//            t.interactable = false;
//        }
//    }

//    // === ���������� ===
//    private void BuildAnswers()
//    {
//        if (!answersContainer)
//        {
//            Debug.LogError("QuestionEntry: Answers Container �� ��������!", this);
//            return;
//        }
//        if (!answerTogglePrefab)
//        {
//            Debug.LogError("QuestionEntry: Answer Toggle Prefab �� ��������!", this);
//            return;
//        }

//        // ToggleGroup (��������� ������ ��� ������)
//        toggleGroup = answersContainer.GetComponent<ToggleGroup>()
//                   ?? answersContainer.gameObject.AddComponent<ToggleGroup>();
//        toggleGroup.allowSwitchOff = true;

//        // �������� ������
//        foreach (Transform c in answersContainer) Destroy(c.gameObject);
//        _toggles.Clear();

//        // ������ 5 ������� (���)
//        for (int i = 0; i < labels.Length; i++)
//        {
//            var togGO = Instantiate(answerTogglePrefab, answersContainer);
//            var tog = togGO.GetComponent<Toggle>();
//            if (!tog) tog = togGO.AddComponent<Toggle>();
//            tog.group = toggleGroup;
//            tog.isOn = false;

//            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();
//            if (lbl) lbl.text = labels[i];

//            SetToggleColor(tog, normalColor);
//            _toggles.Add(tog);
//        }

//        toggleGroup.SetAllTogglesOff();
//    }

//    private void HideImages()
//    {
//        if (questionImage) { questionImage.enabled = false; }
//        if (questionRawImage) { questionRawImage.enabled = false; }
//    }

//    private static void SetToggleColor(Toggle t, Color c)
//    {
//        if (!t) return;
//        if (t.targetGraphic is Image img) img.color = c;
//        else
//        {
//            var img2 = t.GetComponent<Image>();
//            if (img2) img2.color = c;
//        }
//    }

//    // ������ ���������: ������ ������ �������� � ��������� ������ �� �������
//    private void FitToParentWidth(RectTransform rt, float srcW, float srcH)
//    {
//        if (!rt) return;
//        var parent = rt.parent as RectTransform;
//        float pw = 0f;
//        if (parent)
//        {
//            pw = parent.rect.width;
//            if (pw <= 0f)
//            {
//                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
//                pw = parent.rect.width;
//            }
//        }
//        if (pw <= 0f) { StartCoroutine(FitNextFrame(rt, srcW, srcH)); return; }

//        float h = pw * (srcH / srcW);
//        rt.anchorMin = new Vector2(0, 1);
//        rt.anchorMax = new Vector2(1, 1);
//        rt.pivot = new Vector2(0.5f, 1);
//        rt.anchoredPosition = Vector2.zero;
//        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
//    }

//    private System.Collections.IEnumerator FitNextFrame(RectTransform rt, float w, float h)
//    {
//        yield return null;
//        var parent = rt && rt.parent ? (RectTransform)rt.parent : null;
//        float pw = parent ? parent.rect.width : 0f;
//        if (pw <= 0f) pw = Screen.width; // ��������� ����
//        float hh = pw * (h / w);
//        rt.anchorMin = new Vector2(0, 1);
//        rt.anchorMax = new Vector2(1, 1);
//        rt.pivot = new Vector2(0.5f, 1);
//        rt.anchoredPosition = Vector2.zero;
//        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hh);
//    }
//}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionEntry : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Image questionImage;     // ���� � ������� ����� Image (Sprite)
    [SerializeField] private RawImage questionRawImage;  // ���� ����� RawImage (Texture2D)
    [SerializeField] private Transform answersContainer;
    [SerializeField] private GameObject answerTogglePrefab;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color correctColor = new Color(0.16f, 0.64f, 0.24f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    private readonly string[] labels = { "�", "�", "�", "�", "�" };
    private readonly List<Toggle> _toggles = new List<Toggle>();
    private ToggleGroup _group;

    // ���������� � ������������ � ��������� Sprite
    public void Setup(string text, Sprite sprite, List<string> options)
    {
        if (headerText) headerText.text = text ?? string.Empty;

        bool shown = false;

        // ������� 1: Image + Sprite
        if (questionImage && sprite)
        {
            questionImage.enabled = true;
            questionImage.color = Color.white;
            questionImage.preserveAspect = true;
            questionImage.sprite = sprite;
            FitToParentWidth(questionImage.rectTransform, sprite.rect.width, sprite.rect.height);
            shown = true;
        }

        // ������� 2: RawImage + Texture �� Sprite
        if (!shown && questionRawImage && sprite && sprite.texture)
        {
            questionRawImage.enabled = true;
            questionRawImage.color = Color.white;
            questionRawImage.texture = sprite.texture;
            FitToParentWidth(questionRawImage.rectTransform, sprite.texture.width, sprite.texture.height);
            shown = true;
        }

        if (!shown) HideImages();

        BuildAnswers();
    }

    public int GetPickedIndex()
    {
        for (int i = 0; i < _toggles.Count; i++)
            if (_toggles[i] && _toggles[i].isOn) return i;
        return -1;
    }

    public void ShowResult(int correctIndex, int pickedIndex)
    {
        for (int i = 0; i < _toggles.Count; i++)
        {
            var t = _toggles[i];
            if (!t) continue;

            if (i == correctIndex) SetToggleColor(t, correctColor);
            else if (i == pickedIndex) SetToggleColor(t, wrongColor);
            else SetToggleColor(t, normalColor);

            t.interactable = false;
        }
    }

    public void ResetVisuals()
    {
        if (_group) _group.SetAllTogglesOff();
        foreach (var t in _toggles)
        {
            if (!t) continue;
            t.isOn = false;
            t.interactable = true;
            SetToggleColor(t, normalColor);
        }
    }

    // --- internal ---
    private void BuildAnswers()
    {
        if (!answersContainer)
        {
            Debug.LogError("QuestionEntry: Answers Container �� ��������!", this);
            return;
        }
        if (!answerTogglePrefab)
        {
            Debug.LogError("QuestionEntry: Answer Toggle Prefab �� ��������!", this);
            return;
        }

        _group = answersContainer.GetComponent<ToggleGroup>();
        if (!_group) _group = answersContainer.gameObject.AddComponent<ToggleGroup>();
        _group.allowSwitchOff = true;

        foreach (Transform c in answersContainer) Destroy(c.gameObject);
        _toggles.Clear();

        for (int i = 0; i < labels.Length; i++)
        {
            var go = Instantiate(answerTogglePrefab, answersContainer);
            var t = go.GetComponent<Toggle>();
            if (!t) t = go.AddComponent<Toggle>();
            t.group = _group;
            t.isOn = false;

            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = labels[i];

            SetToggleColor(t, normalColor);
            _toggles.Add(t);
        }

        _group.SetAllTogglesOff();
    }

    private void HideImages()
    {
        if (questionImage) questionImage.enabled = false;
        if (questionRawImage) questionRawImage.enabled = false;
    }

    private static void SetToggleColor(Toggle t, Color c)
    {
        if (!t) return;
        if (t.targetGraphic is Image img) img.color = c;
        else
        {
            var img2 = t.GetComponent<Image>();
            if (img2) img2.color = c;
        }
    }

    // ��������� ��� �������: ������ ������ �������� � ��������� ������ �� �������
    private void FitToParentWidth(RectTransform rt, float srcW, float srcH)
    {
        if (!rt) return;
        var parent = rt.parent as RectTransform;
        float pw = 0f;
        if (parent)
        {
            pw = parent.rect.width;
            if (pw <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
                pw = parent.rect.width;
            }
        }
        if (pw <= 0f) { StartCoroutine(FitNextFrame(rt, srcW, srcH)); return; }

        float h = pw * (srcH / srcW);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }

    private System.Collections.IEnumerator FitNextFrame(RectTransform rt, float w, float h)
    {
        yield return null;
        var parent = rt && rt.parent ? (RectTransform)rt.parent : null;
        float pw = parent ? parent.rect.width : 0f;
        if (pw <= 0f) pw = Screen.width;
        float hh = pw * (h / w);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hh);
    }
}
