using UnityEngine;
using UnityEngine.Networking; // ��� EscapeURL

public class ContactUsButton : MonoBehaviour
{
    [Header("WhatsApp")]
    [Tooltip("������� � ������������� �������. ������: +996555123456")]
    public string phoneNumber = "+996778133598";

    [TextArea]
    [Tooltip("��������� �� ��������� (����� �������� ������)")]
    public string defaultMessage = "�����! �������� ������ �����.";

    // ������� ��� ������� � OnClick � ������
    public void OpenWhatsApp()
    {
        string digits = ExtractDigits(phoneNumber);
        if (string.IsNullOrEmpty(digits))
        {
            Debug.LogError("[ContactUs] ������ ����� ��������.");
            return;
        }

        // ���� ������ ��� ������ � �� ��������� ������� ���������� (+996)
        if (!digits.StartsWith("996"))
            digits = "996" + digits.TrimStart('0');

        string msg = string.IsNullOrEmpty(defaultMessage) ? "" : UnityWebRequest.EscapeURL(defaultMessage);
        string url = string.IsNullOrEmpty(msg)
            ? $"https://wa.me/{digits}"                 // ������� WhatsApp ��� �������
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
