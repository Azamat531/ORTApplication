using TMPro;
using UnityEngine;

public class UserRowView : MonoBehaviour
{
    [Header("Base")]
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI realNameText;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI createdText;

    [Header("Extra profile fields")]
    public TextMeshProUGUI regionText;     // �������
    public TextMeshProUGUI districtText;   // �����
    public TextMeshProUGUI schoolText;     // �����
    public TextMeshProUGUI phoneText;      // �������
    public TextMeshProUGUI whatsappText;   // WhatsApp

    private string _uid;

    void Awake()
    {
        // ����-������ �� ������ ����� (�� ������, ���� ������ ���������� ������)
        usernameText = usernameText ?? FindTMP("UsernameText");
        realNameText = realNameText ?? FindTMP("RealNameText");
        roleText = roleText ?? FindTMP("RoleText");
        createdText = createdText ?? FindTMP("CreatedText");
        regionText = regionText ?? FindTMP("RegionText");
        districtText = districtText ?? FindTMP("DistrictText");
        schoolText = schoolText ?? FindTMP("SchoolText");
        phoneText = phoneText ?? FindTMP("PhoneText");
        whatsappText = whatsappText ?? FindTMP("WhatsAppText");
    }

    private TextMeshProUGUI FindTMP(string childName)
    {
        var t = transform.Find(childName);
        return t ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    public void Bind(
        string username, string realName, string localRole, string created, string uid,
        string region, string district, string school, string phone, string whatsapp)
    {
        _uid = uid;

        if (usernameText) usernameText.text = username;
        if (realNameText) realNameText.text = string.IsNullOrWhiteSpace(realName) ? "�" : realName;
        if (roleText) roleText.text = localRole;
        if (createdText) createdText.text = created;

        if (regionText) regionText.text = string.IsNullOrWhiteSpace(region) ? "�" : region;
        if (districtText) districtText.text = string.IsNullOrWhiteSpace(district) ? "�" : district;
        if (schoolText) schoolText.text = string.IsNullOrWhiteSpace(school) ? "�" : school;
        if (phoneText) phoneText.text = string.IsNullOrWhiteSpace(phone) ? "�" : phone;
        if (whatsappText) whatsappText.text = string.IsNullOrWhiteSpace(whatsapp) ? "�" : whatsapp;
    }
}
