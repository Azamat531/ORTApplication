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
//    private readonly string[] labels = { "А", "Б", "В", "Г", "Д" };

//    public void Setup(string text, Sprite sprite, List<string> options)
//    {
//        // Заголовок
//        if (headerText != null)
//            headerText.text = text ?? string.Empty;

//        // Картинка с автоподгонкой по ширине
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
//            Debug.LogError("QuestionEntry: Answers Container не назначен!", this);
//            return;
//        }

//        // ToggleGroup (разрешаем начать без выбора)
//        toggleGroup = answersContainer.GetComponent<ToggleGroup>()
//                    ?? answersContainer.gameObject.AddComponent<ToggleGroup>();
//        toggleGroup.allowSwitchOff = true;

//        // Чистим старые варианты
//        foreach (Transform c in answersContainer)
//            Destroy(c.gameObject);

//        // Создаём 5 ответов. Подписи — только буква (А, Б, В, Г, Д)
//        for (int i = 0; i < labels.Length; i++)
//        {
//            var togGO = Instantiate(answerTogglePrefab, answersContainer);
//            var tog = togGO.GetComponent<Toggle>();
//            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();

//            if (lbl != null)
//                lbl.text = labels[i]; // без дублирования "А. А"

//            if (tog != null)
//            {
//                tog.group = toggleGroup;
//                tog.isOn = false;
//            }
//        }

//        toggleGroup.SetAllTogglesOff();
//    }
//}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionEntry : MonoBehaviour
{
    [Header("UI")]
    public Image questionImage;          // картинка вопроса
    public Transform optionsRoot;        // контейнер с вариантами (Toggle'и)
    public ToggleGroup toggleGroup;      // можно оставить пустым — создадим сами

    [Header("Colors")]
    public Color normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    public Color correctColor = new Color(0.16f, 0.64f, 0.24f, 1f);
    public Color wrongColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    private readonly List<Toggle> _toggles = new List<Toggle>();

    /// <summary>Подготовка карточки: картинка + подписи вариантов.</summary>
    public void Setup(string questionTextUnused, Sprite sprite, List<string> optionLabels)
    {
        if (questionImage) questionImage.sprite = sprite;

        CollectToggles();

        // если нужно — подпишем варианты (ищем TMP_Text у Toggle)
        if (optionLabels != null && optionLabels.Count > 0)
        {
            for (int i = 0; i < _toggles.Count && i < optionLabels.Count; i++)
            {
                var txt = _toggles[i].GetComponentInChildren<TMP_Text>();
                if (txt) txt.text = optionLabels[i];
            }
        }

        // сброс выбора и цветов
        foreach (var t in _toggles)
        {
            t.isOn = false;
            SetToggleColor(t, normalColor);
            t.interactable = true;
        }
    }

    /// <summary>Индекс выбранного варианта (0..N-1), либо -1 если ничего не выбрано.</summary>
    public int GetPickedIndex()
    {
        for (int i = 0; i < _toggles.Count; i++)
            if (_toggles[i].isOn) return i;
        return -1;
    }

    /// <summary>Подсветить результат: правильный зелёный, ошибочный красный; заблокировать выбор.</summary>
    public void ShowResult(int correctIndex, int pickedIndex)
    {
        for (int i = 0; i < _toggles.Count; i++)
        {
            var t = _toggles[i];

            if (i == correctIndex) SetToggleColor(t, correctColor);
            else if (i == pickedIndex) SetToggleColor(t, wrongColor);
            else SetToggleColor(t, normalColor);

            t.interactable = false;       // блокируем после проверки
        }
    }

    // ---------- helpers ----------
    private void CollectToggles()
    {
        _toggles.Clear();

        if (!optionsRoot) optionsRoot = transform; // ищем в детях по умолчанию

        var found = optionsRoot.GetComponentsInChildren<Toggle>(true);
        _toggles.AddRange(found);

        // если у контейнера нет ToggleGroup — создадим, чтобы варианты были взаимоисключающими
        if (!_toggles.TrueForAll(t => t.group != null))
        {
            if (!toggleGroup)
            {
                toggleGroup = optionsRoot.GetComponent<ToggleGroup>();
                if (!toggleGroup) toggleGroup = optionsRoot.gameObject.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = true;
            }
            foreach (var t in _toggles) t.group = toggleGroup;
        }
    }

    private static void SetToggleColor(Toggle t, Color c)
    {
        // пробуем покрасить фон варианта (targetGraphic) или ближайший Image
        if (t.targetGraphic is Image img) img.color = c;
        else
        {
            var img2 = t.GetComponent<Image>();
            if (img2) img2.color = c;
        }
    }
}
