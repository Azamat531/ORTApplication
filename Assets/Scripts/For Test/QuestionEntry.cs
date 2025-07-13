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
//        // 1) Заголовок
//        if (headerText != null)
//            headerText.text = text;

//        // 2) Картинка
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

//        // 3) Подготовка ToggleGroup
//        if (answersContainer == null)
//        {
//            Debug.LogError("QuestionEntry: Answers Container не назначен!", this);
//            return;
//        }
//        toggleGroup = answersContainer.GetComponent<ToggleGroup>();
//        if (toggleGroup == null)
//            toggleGroup = answersContainer.gameObject.AddComponent<ToggleGroup>();

//        // Позволяем начать с пустой группы
//        toggleGroup.allowSwitchOff = true;

//        // 4) Очищаем старые варианты
//        foreach (Transform c in answersContainer)
//            Destroy(c.gameObject);

//        // 5) Генерируем новые Toggle
//        for (int i = 0; i < options.Count && i < labels.Length; i++)
//        {
//            var togGO = Instantiate(answerTogglePrefab, answersContainer);
//            var tog = togGO.GetComponent<Toggle>();
//            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();

//            if (lbl != null)
//                lbl.text = $"{labels[i]}. {options[i]}";

//            if (tog != null)
//            {
//                // Ни один не выбран по умолчанию
//                tog.isOn = false;
//                // Встраиваем в группу
//                tog.group = toggleGroup;
//                // Как только юзер выберет этот Toggle — запретим полный сброс
//                tog.onValueChanged.AddListener(isOn =>
//                {
//                    if (isOn)
//                        toggleGroup.allowSwitchOff = false;
//                });
//            }
//        }

//        // 6) Сбрасываем все отметки (чтобы ничего не было выбрано)
//        toggleGroup.SetAllTogglesOff();
//    }
//}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Image questionImage;
    [SerializeField] private Transform answersContainer;
    [SerializeField] private GameObject answerTogglePrefab;

    private ToggleGroup toggleGroup;
    private readonly string[] labels = { "А", "Б", "В", "Г", "Д" };

    public void Setup(string text, Sprite sprite, List<string> options)
    {
        // 1) Устанавливаем текст вопроса
        if (headerText != null)
            headerText.text = text;

        // 2) Подгон картинки
        if (sprite != null && questionImage != null)
        {
            questionImage.sprite = sprite;
            var rt = questionImage.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            float pw = (rt.parent as RectTransform).rect.width;
            rt.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                pw * (sprite.rect.height / sprite.rect.width)
            );
        }

        // 3) Инициализируем ToggleGroup
        if (answersContainer == null)
        {
            Debug.LogError("QuestionEntry: Answers Container не назначен!", this);
            return;
        }
        toggleGroup = answersContainer.GetComponent<ToggleGroup>()
                    ?? answersContainer.gameObject.AddComponent<ToggleGroup>();

        // Включаем allowSwitchOff, чтобы можно было снять выбор полностью
        toggleGroup.allowSwitchOff = true;

        // 4) Убираем старые варианты
        foreach (Transform c in answersContainer)
            Destroy(c.gameObject);

        // 5) Создаём новые Toggle'ы
        for (int i = 0; i < options.Count && i < labels.Length; i++)
        {
            var togGO = Instantiate(answerTogglePrefab, answersContainer);
            var tog = togGO.GetComponent<Toggle>();
            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();

            if (lbl != null)
                lbl.text = $"{labels[i]}. {options[i]}";

            if (tog != null)
            {
                tog.group = toggleGroup;
                tog.isOn = false;  // гарантируем, что по умолчанию всё выключено
            }
        }

        // 6) Ещё раз сбросим все, на всякий случай
        toggleGroup.SetAllTogglesOff();
    }
}
