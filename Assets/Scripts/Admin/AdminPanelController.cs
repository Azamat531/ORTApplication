using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Firebase;                 // <- ��� ������ ProjectId
using Firebase.Functions;       // <- ��� ������ �������� �������

// Create-user form with extra profile fields + Back button + DEBUG logs
public class AdminPanelController : MonoBehaviour
{
    [Header("Inputs � �������")]
    public TMP_InputField usernameInput;   // username => email username@ort.app
    public TMP_InputField realNameInput;   // ������������ ���
    public TMP_InputField passwordInput;   // >= 6 ��������

    [Header("Inputs � ���. �������")]
    public TMP_InputField regionInput;     // �������
    public TMP_InputField districtInput;   // �����
    public TMP_InputField schoolInput;     // �����
    public TMP_InputField phoneInput;      // ������� (�����)
    public TMP_InputField whatsappInput;   // WhatsApp (���� ����� � ������ phone)

    [Header("Password Eye (�����������)")]
    public Button passEyeButton;
    public Image passEyeIcon;
    public Sprite eyeOpen;
    public Sprite eyeClosed;

    [Header("Role (TMP_Dropdown)")]
    // �������: 0=������ (student), 1=������� (teacher), 2=����� (admin)
    public TMP_Dropdown roleDropdown;

    [Header("UI")]
    public Button createButton;
    public TextMeshProUGUI statusText;

    [Header("Navigation")]
    public Button backButton;              // ������ "�����"
    public GameObject adminMenuRoot;       // ������ AdminMenuPanel (���� ������������)

    private FirebaseFunctions _functions;
    private bool _pwdVisible = false;
    private bool _busy = false;

    void Awake()
    {
        // ���������� �����������
        if (createButton) createButton.onClick.AddListener(() => _ = CreateUser());
        if (passEyeButton) passEyeButton.onClick.AddListener(TogglePasswordVisibility);
        if (backButton) backButton.onClick.AddListener(ClosePanel);

        // ����� ������ �� ���������
        ApplyPasswordVisibility(false);
        SetStatus("");

        // �����: ���������� ��� �� ������, ���� ���������� �������
        _functions = FirebaseFunctions.GetInstance("us-central1");

        // ��������: � ������ ������� ������ �������� Unity
        Debug.Log("[CHECK] Firebase ProjectId = " + FirebaseApp.DefaultInstance?.Options?.ProjectId);
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        if (createButton) createButton.interactable = !busy;
        if (usernameInput) usernameInput.interactable = !busy;
        if (realNameInput) realNameInput.interactable = !busy;
        if (passwordInput) passwordInput.interactable = !busy;
        if (regionInput) regionInput.interactable = !busy;
        if (districtInput) districtInput.interactable = !busy;
        if (schoolInput) schoolInput.interactable = !busy;
        if (phoneInput) phoneInput.interactable = !busy;
        if (whatsappInput) whatsappInput.interactable = !busy;
        if (roleDropdown) roleDropdown.interactable = !busy;
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg ?? string.Empty;
        if (!string.IsNullOrEmpty(msg)) Debug.Log("[AdminPanel] " + msg);
    }

    private static string SanitizeUsername(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        raw = raw.Trim().ToLowerInvariant();
        raw = Regex.Replace(raw, "[^a-z0-9.-]", ""); // ��������/�����/�����/�����
        return raw.Trim('.', '-');
    }

    private static string DigitsOnly(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return Regex.Replace(raw, "[^0-9]", "");
    }

    private void TogglePasswordVisibility()
    {
        _pwdVisible = !_pwdVisible;
        ApplyPasswordVisibility(_pwdVisible);
    }

    private void ApplyPasswordVisibility(bool visible)
    {
        if (!passwordInput) return;
        passwordInput.contentType = visible ? TMP_InputField.ContentType.Standard
                                            : TMP_InputField.ContentType.Password;
        var txt = passwordInput.text;
        passwordInput.SetTextWithoutNotify(txt);
        passwordInput.ForceLabelUpdate();
        if (passEyeIcon) passEyeIcon.sprite = visible ? eyeOpen : eyeClosed;
    }

    // ������� ���� �� ������� ���������
    private string GetRole()
    {
        if (roleDropdown)
        {
            switch (roleDropdown.value)
            {
                case 2: return "admin";    // �����
                case 1: return "teacher";  // �������
                default: return "student"; // ������
            }
        }
        return "student";
    }

    private async Task CreateUser()
    {
        if (_busy) return;

        string u = SanitizeUsername(usernameInput ? usernameInput.text : string.Empty);
        string r = realNameInput ? realNameInput.text.Trim() : string.Empty;
        string p = passwordInput ? passwordInput.text : string.Empty;
        string role = GetRole();

        // ���. ����
        string region = regionInput ? regionInput.text.Trim() : string.Empty;
        string district = districtInput ? districtInput.text.Trim() : string.Empty;
        string school = schoolInput ? schoolInput.text.Trim() : string.Empty;
        string phone = DigitsOnly(phoneInput ? phoneInput.text : string.Empty);
        string whatsapp = DigitsOnly(whatsappInput ? whatsappInput.text : string.Empty);
        if (string.IsNullOrEmpty(whatsapp)) whatsapp = phone;

        if (usernameInput && usernameInput.text != u) usernameInput.text = u;

        if (string.IsNullOrEmpty(u)) { SetStatus("������� username (��������/�����/.-)"); return; }
        if (string.IsNullOrEmpty(r)) { SetStatus("������� ���"); return; }
        if (p.Length < 6) { SetStatus("������ ������ ���� �� ������ 6"); return; }

        // DEBUG: ��� ���������� �� �������
        Debug.Log($"[AdminPanel] SEND extras: username='{u}', role='{role}', region='{region}', district='{district}', school='{school}', phone='{phone}', wa='{whatsapp}'");

        SetBusy(true);
        SetStatus($"�������� ����: {role}");
        try
        {
            var callable = _functions.GetHttpsCallable("createUserAsAdmin");
            var data = new Dictionary<string, object> {
                { "username", u }, { "realName", r }, { "password", p }, { "role", role },
                { "region", region }, { "district", district }, { "school", school },
                { "phone", phone }, { "whatsapp", whatsapp }
            };
            var result = await callable.CallAsync(data);
            var dict = result?.Data as IDictionary<string, object>;
            var uid = dict != null && dict.ContainsKey("uid") ? dict["uid"].ToString() : "(no uid)";
            SetStatus($"������: {u}@ort.app (uid={uid})");

            // ������� �����
            if (usernameInput) usernameInput.text = string.Empty;
            if (realNameInput) realNameInput.text = string.Empty;
            if (passwordInput) passwordInput.text = string.Empty;
            if (regionInput) regionInput.text = string.Empty;
            if (districtInput) districtInput.text = string.Empty;
            if (schoolInput) schoolInput.text = string.Empty;
            if (phoneInput) phoneInput.text = string.Empty;
            if (whatsappInput) whatsappInput.text = string.Empty;
            if (roleDropdown) roleDropdown.value = 0;
        }
        catch (System.Exception e)
        {
            var msg = e.Message ?? "������";
            if (msg.Contains("already-exists")) msg = "��� ��� �����";
            else if (msg.Contains("weak password")) msg = "������ ������";
            else if (msg.Contains("permission-denied")) msg = "��� ���� (����� admin)";
            else if (msg.Contains("invalid role")) msg = "�������� ���� (������� ������� ����� � ���������)";
            SetStatus("������: " + msg);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ClosePanel()
    {
        SetBusy(false);
        SetStatus("");
        gameObject.SetActive(false);
        if (adminMenuRoot) adminMenuRoot.SetActive(true);
    }
}
