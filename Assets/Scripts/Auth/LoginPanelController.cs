//using System.Threading.Tasks;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class LoginPanelController : MonoBehaviour
//{
//    [Header("Login UI")]
//    public TMP_InputField usernameInput;
//    public TMP_InputField passwordInput;
//    public Toggle rememberMeToggle;
//    public TextMeshProUGUI statusText;
//    public Button loginButton;

//    [Header("Password Eye")]
//    public Button eyeButton;       // кнопка-глаз
//    public Image eyeIcon;          // иконка (опц.)
//    public Sprite eyeOpen;         // иконка "показать"
//    public Sprite eyeClosed;       // иконка "скрыть"

//    private const string RememberKey = "auth.remember";
//    private bool _passwordVisible = false;

//    void Awake()
//    {
//        if (loginButton) loginButton.onClick.AddListener(() => _ = DoLogin());

//        if (rememberMeToggle)
//            rememberMeToggle.isOn = PlayerPrefs.GetInt(RememberKey, 1) == 1;

//        // стартуем со скрытым паролем
//        SetPasswordVisibility(false);
//        if (eyeButton) eyeButton.onClick.AddListener(ToggleEye);
//        UpdateEyeIcon();

//        // стили и стабильная каретка (мобилки)
//        StyleInput(usernameInput);
//        StyleInput(passwordInput);
//    }

//    void OnDestroy()
//    {
//        if (eyeButton) eyeButton.onClick.RemoveListener(ToggleEye);
//        if (loginButton) loginButton.onClick.RemoveAllListeners();
//    }

//    // === LOGIN (твоя логика) ===
//    private async Task DoLogin()
//    {
//        string u = usernameInput ? usernameInput.text.Trim() : "";
//        string p = passwordInput ? passwordInput.text : "";

//        if (string.IsNullOrEmpty(u)) { SetStatus("Логин киргизиңиз"); return; }
//        if (p.Length < 6) { SetStatus("Сыр сөз ≥ 6"); return; }

//        if (rememberMeToggle) PlayerPrefs.SetInt(RememberKey, rememberMeToggle.isOn ? 1 : 0);

//        SetStatus("Кирүү…");
//        var (ok, err) = await AuthManager.Instance.SignIn(u, p); // ← твой вызов
//        if (!ok) { SetStatus("Ката: " + err); return; }

//        var ui = FindAnyObjectByType<UIManager>();
//        if (ui) ui.OpenMainApp();
//        SetStatus("");
//    }

//    private void SetStatus(string s) { if (statusText) statusText.text = s; }

//    // === PASSWORD (твоя логика) ===
//    private void ToggleEye()
//    {
//        _passwordVisible = !_passwordVisible;
//        SetPasswordVisibility(_passwordVisible);
//        UpdateEyeIcon();
//    }

//    private void SetPasswordVisibility(bool visible)
//    {
//        if (!passwordInput) return;

//        passwordInput.contentType = visible
//            ? TMP_InputField.ContentType.Standard
//            : TMP_InputField.ContentType.Password;

//        // TMP просит заново "установить" текст, чтобы применился contentType
//        string txt = passwordInput.text;
//        passwordInput.SetTextWithoutNotify(txt);
//        passwordInput.ForceLabelUpdate();
//    }

//    private void UpdateEyeIcon()
//    {
//        if (!eyeIcon) return;
//        eyeIcon.sprite = _passwordVisible ? eyeOpen : eyeClosed;
//    }

//    // === Каретка и поведение на мобилках ===
//    private void StyleInput(TMP_InputField f)
//    {
//        if (!f) return;

//        f.customCaretColor = true;
//        f.caretColor = new Color32(0, 0, 0, 255);
//        f.caretWidth = 2;
//        f.caretBlinkRate = 0.6f;
//        f.selectionColor = new Color(0, 0, 0, 0.2f);

//#if UNITY_ANDROID || UNITY_IOS
//        // Пусть каретку рисует TMP (а не системный in-place input)
//        f.shouldHideMobileInput = true;
//#endif
//        // Гарантируем видимую каретку при фокусе
//        f.onSelect.AddListener(_ => StartCoroutine(EnsureCaretVisibleNextFrame(f)));
//    }

//    private System.Collections.IEnumerator EnsureCaretVisibleNextFrame(TMP_InputField f)
//    {
//        yield return null; // дождаться кадра с подъёмом клавиатуры
//        if (!f) yield break;
//        f.ActivateInputField();
//        f.MoveTextEnd(false);
//        f.ForceLabelUpdate();
//    }
//}

using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth; // нужен Firebase Auth, чтобы проверить текущего пользователя

public class LoginPanelController : MonoBehaviour
{
    [Header("Login UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Toggle rememberMeToggle;
    public TextMeshProUGUI statusText;
    public Button loginButton;

    [Header("Password Eye")]
    public Button eyeButton;       // кнопка-глаз
    public Image eyeIcon;          // иконка (опц.)
    public Sprite eyeOpen;         // иконка "показать"
    public Sprite eyeClosed;       // иконка "скрыть"

    private const string RememberKey = "auth.remember";
    private const string RememberUserKey = "auth.remember.username";
    private bool _passwordVisible = false;

    void Awake()
    {
        if (loginButton) loginButton.onClick.AddListener(() => _ = DoLogin());

        // ===== init remember toggle =====
        bool remember = PlayerPrefs.GetInt(RememberKey, 1) == 1; // по умолчанию включён
        if (rememberMeToggle)
        {
            rememberMeToggle.isOn = remember;
            rememberMeToggle.onValueChanged.AddListener(OnRememberChanged);
        }

        // Если remember включён — подставим сохранённый логин
        if (remember && usernameInput)
        {
            string saved = PlayerPrefs.GetString(RememberUserKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                usernameInput.SetTextWithoutNotify(saved);
                usernameInput.MoveTextEnd(false);
                usernameInput.ForceLabelUpdate();
            }
        }

        // Сохранять логин по завершению редактирования, если remember включён
        if (usernameInput)
        {
            usernameInput.onEndEdit.AddListener(_ =>
            {
                if (rememberMeToggle && rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetString(RememberUserKey, usernameInput.text.Trim());
                    PlayerPrefs.Save();
                }
            });
        }

        // стартуем со скрытым паролем
        SetPasswordVisibility(false);
        if (eyeButton) eyeButton.onClick.AddListener(ToggleEye);
        UpdateEyeIcon();

        // стили и стабильная каретка (мобилки)
        StyleInput(usernameInput);
        StyleInput(passwordInput);
    }

    void Start()
    {
        // ===== авто-вход =====
        // Если "Запомни меня" включён и у Firebase уже есть текущий пользователь — сразу в приложение.
        if (rememberMeToggle && rememberMeToggle.isOn)
            StartCoroutine(TryAutoEnterIfSession());
    }

    private System.Collections.IEnumerator TryAutoEnterIfSession()
    {
        // Дадим Firebase время инициализироваться (1-2 кадра)
        yield return null;
        yield return null;

        var auth = FirebaseAuth.DefaultInstance;
        var user = auth != null ? auth.CurrentUser : null;

        if (user != null) // сеанс уже существует и валиден (Firebase сам восстанавливает)
        {
            var ui = FindAnyObjectByType<UIManager>();
            if (ui) ui.OpenMainApp();
        }
    }

    void OnDestroy()
    {
        if (eyeButton) eyeButton.onClick.RemoveListener(ToggleEye);
        if (loginButton) loginButton.onClick.RemoveAllListeners();
        if (rememberMeToggle) rememberMeToggle.onValueChanged.RemoveListener(OnRememberChanged);
        if (usernameInput) usernameInput.onEndEdit.RemoveAllListeners();
    }

    // === LOGIN (твоя логика) ===
    private async Task DoLogin()
    {
        string u = usernameInput ? usernameInput.text.Trim() : "";
        string p = passwordInput ? passwordInput.text : "";

        if (string.IsNullOrEmpty(u)) { SetStatus("Логин киргизиңиз"); return; }
        if (p.Length < 6) { SetStatus("Сыр сөз ≥ 6"); return; }

        // Сохраняем состояние тумблера
        if (rememberMeToggle)
            PlayerPrefs.SetInt(RememberKey, rememberMeToggle.isOn ? 1 : 0);

        SetStatus("Кирүү…");
        var (ok, err) = await AuthManager.Instance.SignIn(u, p); // ← твой вызов
        if (!ok) { SetStatus("Ката: " + err); return; }

        // Если remember включен — сохраняем логин (пароль не трогаем)
        if (rememberMeToggle && rememberMeToggle.isOn)
            PlayerPrefs.SetString(RememberUserKey, u);
        else
            PlayerPrefs.DeleteKey(RememberUserKey);
        PlayerPrefs.Save();

        var ui = FindAnyObjectByType<UIManager>();
        if (ui) ui.OpenMainApp();
        SetStatus("");
    }

    private void SetStatus(string s) { if (statusText) statusText.text = s; }

    // === PASSWORD (твоя логика) ===
    private void ToggleEye()
    {
        _passwordVisible = !_passwordVisible;
        SetPasswordVisibility(_passwordVisible);
        UpdateEyeIcon();
    }

    private void SetPasswordVisibility(bool visible)
    {
        if (!passwordInput) return;

        passwordInput.contentType = visible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        string txt = passwordInput.text;
        passwordInput.SetTextWithoutNotify(txt);
        passwordInput.ForceLabelUpdate();
    }

    private void UpdateEyeIcon()
    {
        if (!eyeIcon) return;
        eyeIcon.sprite = _passwordVisible ? eyeOpen : eyeClosed;
    }

    // === Каретка и поведение на мобилках ===
    private void StyleInput(TMP_InputField f)
    {
        if (!f) return;

        f.customCaretColor = true;
        f.caretColor = new Color32(0, 0, 0, 255);
        f.caretWidth = 2;                // <- как ты попросил
        f.caretBlinkRate = 0.6f;
        f.selectionColor = new Color(0, 0, 0, 0.2f);

#if UNITY_ANDROID || UNITY_IOS
        // Пусть каретку рисует TMP (а не системный in-place input)
        f.shouldHideMobileInput = true;
#endif
        // Гарантировать видимую каретку при фокусе
        f.onSelect.AddListener(_ => StartCoroutine(EnsureCaretVisibleNextFrame(f)));
    }

    private System.Collections.IEnumerator EnsureCaretVisibleNextFrame(TMP_InputField f)
    {
        yield return null; // дождаться кадра с подъёмом клавиатуры
        if (!f) yield break;
        f.ActivateInputField();
        f.MoveTextEnd(false);
        f.ForceLabelUpdate();
    }

    // === Обработка тумблера Remember ===
    private void OnRememberChanged(bool on)
    {
        PlayerPrefs.SetInt(RememberKey, on ? 1 : 0);
        if (!on)
        {
            PlayerPrefs.DeleteKey(RememberUserKey);
        }
        else if (usernameInput)
        {
            PlayerPrefs.SetString(RememberUserKey, usernameInput.text.Trim());
        }
        PlayerPrefs.Save();
    }
}
