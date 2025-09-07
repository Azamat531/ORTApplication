using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Firebase;                 // <- для вывода ProjectId
using Firebase.Functions;       // <- для вызова облачных функций

// Create-user form with extra profile fields + Back button + DEBUG logs
public class AdminPanelController : MonoBehaviour
{
    [Header("Inputs — базовые")]
    public TMP_InputField usernameInput;   // username => email username@ort.app
    public TMP_InputField realNameInput;   // отображаемое имя
    public TMP_InputField passwordInput;   // >= 6 символов

    [Header("Inputs — доп. профиль")]
    public TMP_InputField regionInput;     // Область
    public TMP_InputField districtInput;   // Район
    public TMP_InputField schoolInput;     // Школа
    public TMP_InputField phoneInput;      // Телефон (цифры)
    public TMP_InputField whatsappInput;   // WhatsApp (если пусто — возьмём phone)

    [Header("Password Eye (опционально)")]
    public Button passEyeButton;
    public Image passEyeIcon;
    public Sprite eyeOpen;
    public Sprite eyeClosed;

    [Header("Role (TMP_Dropdown)")]
    // Порядок: 0=Окуучу (student), 1=Мугалим (teacher), 2=Админ (admin)
    public TMP_Dropdown roleDropdown;

    [Header("UI")]
    public Button createButton;
    public TextMeshProUGUI statusText;

    [Header("Navigation")]
    public Button backButton;              // кнопка "Назад"
    public GameObject adminMenuRoot;       // корень AdminMenuPanel (куда возвращаться)

    private FirebaseFunctions _functions;
    private bool _pwdVisible = false;
    private bool _busy = false;

    void Awake()
    {
        // Подключаем обработчики
        if (createButton) createButton.onClick.AddListener(() => _ = CreateUser());
        if (passEyeButton) passEyeButton.onClick.AddListener(TogglePasswordVisibility);
        if (backButton) backButton.onClick.AddListener(ClosePanel);

        // Режим пароля по умолчанию
        ApplyPasswordVisibility(false);
        SetStatus("");

        // ВАЖНО: используем тот же регион, куда задеплоена функция
        _functions = FirebaseFunctions.GetInstance("us-central1");

        // Проверка: к какому проекту сейчас привязан Unity
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
        raw = Regex.Replace(raw, "[^a-z0-9.-]", ""); // латиница/цифры/точка/дефис
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

    // Маппинг роли по индексу дропдауна
    private string GetRole()
    {
        if (roleDropdown)
        {
            switch (roleDropdown.value)
            {
                case 2: return "admin";    // Админ
                case 1: return "teacher";  // Мугалим
                default: return "student"; // Окуучу
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

        // доп. поля
        string region = regionInput ? regionInput.text.Trim() : string.Empty;
        string district = districtInput ? districtInput.text.Trim() : string.Empty;
        string school = schoolInput ? schoolInput.text.Trim() : string.Empty;
        string phone = DigitsOnly(phoneInput ? phoneInput.text : string.Empty);
        string whatsapp = DigitsOnly(whatsappInput ? whatsappInput.text : string.Empty);
        if (string.IsNullOrEmpty(whatsapp)) whatsapp = phone;

        if (usernameInput && usernameInput.text != u) usernameInput.text = u;

        if (string.IsNullOrEmpty(u)) { SetStatus("Введите username (латиница/цифры/.-)"); return; }
        if (string.IsNullOrEmpty(r)) { SetStatus("Введите имя"); return; }
        if (p.Length < 6) { SetStatus("Пароль должен быть не короче 6"); return; }

        // DEBUG: что отправляем на функцию
        Debug.Log($"[AdminPanel] SEND extras: username='{u}', role='{role}', region='{region}', district='{district}', school='{school}', phone='{phone}', wa='{whatsapp}'");

        SetBusy(true);
        SetStatus($"Создание… роль: {role}");
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
            SetStatus($"Готово: {u}@ort.app (uid={uid})");

            // очистка формы
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
            var msg = e.Message ?? "Ошибка";
            if (msg.Contains("already-exists")) msg = "Ник уже занят";
            else if (msg.Contains("weak password")) msg = "Слабый пароль";
            else if (msg.Contains("permission-denied")) msg = "Нет прав (нужен admin)";
            else if (msg.Contains("invalid role")) msg = "Неверная роль (проверь порядок опций в дропдауне)";
            SetStatus("Ошибка: " + msg);
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
