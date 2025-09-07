using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingItemView : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI rankText;     // 001
    public TextMeshProUGUI nameText;     // Azamat Musabaev
    public TextMeshProUGUI chip1Text;    // Yssyk-Kol
    public TextMeshProUGUI chip2Text;    // Karakol
    public TextMeshProUGUI chip3Text;    // Karasaev
    public TextMeshProUGUI scoreText;    // 98 (������ ������)

    [Header("Visuals (optional)")]
    public Image scorePill;              // ��� ������; ����� ������ ������ � �������

    public void Setup(string rankText, string nameText, string chip1, string chip2, string chip3, int score)
    {
        if (this.rankText) this.rankText.text = rankText;
        if (this.nameText) this.nameText.text = nameText;

        if (chip1Text) chip1Text.text = string.IsNullOrWhiteSpace(chip1) ? "�" : chip1;
        if (chip2Text) chip2Text.text = string.IsNullOrWhiteSpace(chip2) ? "�" : chip2;
        if (chip3Text) chip3Text.text = string.IsNullOrWhiteSpace(chip3) ? "�" : chip3;

        if (scoreText) scoreText.text = score.ToString();
    }
}
