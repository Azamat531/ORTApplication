using UnityEngine;
using UnityEngine.Networking; // для EscapeURL

public class ContactUsButton : MonoBehaviour
{
    [Header("WhatsApp")]
    [Tooltip("Телефон в международном формате. Пример: +996555123456")]
    public string phoneNumber = "+996778133598";

    [TextArea]
    [Tooltip("Сообщение по умолчанию (можно оставить пустым)")]
    public string defaultMessage = "Салам! Колдонмо боюнча суроо.";

    // Привяжи эту функцию к OnClick у кнопки
    public void OpenWhatsApp()
    {
        string digits = ExtractDigits(phoneNumber);
        if (string.IsNullOrEmpty(digits))
        {
            Debug.LogError("[ContactUs] Пустой номер телефона.");
            return;
        }

        // Если забыли код страны — по умолчанию считаем Кыргызстан (+996)
        if (!digits.StartsWith("996"))
            digits = "996" + digits.TrimStart('0');

        string msg = string.IsNullOrEmpty(defaultMessage) ? "" : UnityWebRequest.EscapeURL(defaultMessage);
        string url = string.IsNullOrEmpty(msg)
            ? $"https://wa.me/{digits}"                 // откроет WhatsApp или браузер
            : $"https://wa.me/{digits}?text={msg}";

        Application.OpenURL(url);
    }

    private static string ExtractDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) if (char.IsDigit(c)) sb.Append(c);
        return sb.ToString();
    }
}
