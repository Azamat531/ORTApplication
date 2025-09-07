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
    public TextMeshProUGUI regionText;     // ќбласть
    public TextMeshProUGUI districtText;   // –айон
    public TextMeshProUGUI schoolText;     // Ўкола
    public TextMeshProUGUI phoneText;      // “елефон
    public TextMeshProUGUI whatsappText;   // WhatsApp

    private string _uid;

    void Awake()
    {
        // јвто-вз€тие по именам детей (на случай, если забыли проставить ссылки)
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
        if (realNameText) realNameText.text = string.IsNullOrWhiteSpace(realName) ? "Ч" : realName;
        if (roleText) roleText.text = localRole;
        if (createdText) createdText.text = created;

        if (regionText) regionText.text = string.IsNullOrWhiteSpace(region) ? "Ч" : region;
        if (districtText) districtText.text = string.IsNullOrWhiteSpace(district) ? "Ч" : district;
        if (schoolText) schoolText.text = string.IsNullOrWhiteSpace(school) ? "Ч" : school;
        if (phoneText) phoneText.text = string.IsNullOrWhiteSpace(phone) ? "Ч" : phone;
        if (whatsappText) whatsappText.text = string.IsNullOrWhiteSpace(whatsapp) ? "Ч" : whatsapp;
    }
}
