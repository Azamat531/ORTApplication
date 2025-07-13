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
//        // 1) ���������
//        if (headerText != null)
//            headerText.text = text;

//        // 2) ��������
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

//        // 3) ���������� ToggleGroup
//        if (answersContainer == null)
//        {
//            Debug.LogError("QuestionEntry: Answers Container �� ��������!", this);
//            return;
//        }
//        toggleGroup = answersContainer.GetComponent<ToggleGroup>();
//        if (toggleGroup == null)
//            toggleGroup = answersContainer.gameObject.AddComponent<ToggleGroup>();

//        // ��������� ������ � ������ ������
//        toggleGroup.allowSwitchOff = true;

//        // 4) ������� ������ ��������
//        foreach (Transform c in answersContainer)
//            Destroy(c.gameObject);

//        // 5) ���������� ����� Toggle
//        for (int i = 0; i < options.Count && i < labels.Length; i++)
//        {
//            var togGO = Instantiate(answerTogglePrefab, answersContainer);
//            var tog = togGO.GetComponent<Toggle>();
//            var lbl = togGO.GetComponentInChildren<TextMeshProUGUI>();

//            if (lbl != null)
//                lbl.text = $"{labels[i]}. {options[i]}";

//            if (tog != null)
//            {
//                // �� ���� �� ������ �� ���������
//                tog.isOn = false;
//                // ���������� � ������
//                tog.group = toggleGroup;
//                // ��� ������ ���� ������� ���� Toggle � �������� ������ �����
//                tog.onValueChanged.AddListener(isOn =>
//                {
//                    if (isOn)
//                        toggleGroup.allowSwitchOff = false;
//                });
//            }
//        }

//        // 6) ���������� ��� ������� (����� ������ �� ���� �������)
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
    private readonly string[] labels = { "�", "�", "�", "�", "�" };

    public void Setup(string text, Sprite sprite, List<string> options)
    {
        // 1) ������������� ����� �������
        if (headerText != null)
            headerText.text = text;

        // 2) ������ ��������
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

        // 3) �������������� ToggleGroup
        if (answersContainer == null)
        {
            Debug.LogError("QuestionEntry: Answers Container �� ��������!", this);
            return;
        }
        toggleGroup = answersContainer.GetComponent<ToggleGroup>()
                    ?? answersContainer.gameObject.AddComponent<ToggleGroup>();

        // �������� allowSwitchOff, ����� ����� ���� ����� ����� ���������
        toggleGroup.allowSwitchOff = true;

        // 4) ������� ������ ��������
        foreach (Transform c in answersContainer)
            Destroy(c.gameObject);

        // 5) ������ ����� Toggle'�
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
                tog.isOn = false;  // �����������, ��� �� ��������� �� ���������
            }
        }

        // 6) ��� ��� ������� ���, �� ������ ������
        toggleGroup.SetAllTogglesOff();
    }
}
