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
//    public Button eyeButton;
//    public Image eyeIcon;
//    public Sprite eyeOpen;
//    public Sprite eyeClosed;

//    private const string RememberKey = "auth.remember";
//    private bool _passwordVisible = false;

//    void Awake()
//    {
//        if (loginButton) loginButton.onClick.AddListener(() => _ = DoLogin());

//        if (rememberMeToggle)
//            rememberMeToggle.isOn = PlayerPrefs.GetInt(RememberKey, 1) == 1;

//        SetPasswordVisibility(false);

//        if (eyeButton) eyeButton.onClick.AddListener(ToggleEye);
//        UpdateEyeIcon();
//    }

//    void OnDestroy()
//    {
//        if (eyeButton) eyeButton.onClick.RemoveListener(ToggleEye);
//        if (loginButton) loginButton.onClick.RemoveAllListeners();
//    }

//    private async Task DoLogin()
//    {
//        string u = usernameInput ? usernameInput.text.Trim() : "";
//        string p = passwordInput ? passwordInput.text : "";

//        if (string.IsNullOrEmpty(u)) { SetStatus("Введите имя пользователя"); return; }
//        if (p.Length < 6) { SetStatus("Пароль ≥ 6 символов"); return; }

//        if (rememberMeToggle) PlayerPrefs.SetInt(RememberKey, rememberMeToggle.isOn ? 1 : 0);

//        SetStatus("Вход...");
//        var (ok, err) = await AuthManager.Instance.SignIn(u, p);
//        if (!ok) { SetStatus("Ошибка: " + err); return; }

//        var ui = FindAnyObjectByType<UIManager>();
//        if (ui) ui.OpenMainApp();
//        SetStatus("");
//    }

//    private void SetStatus(string s) { if (statusText) statusText.text = s; }

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

//        string txt = passwordInput.text;
//        passwordInput.SetTextWithoutNotify(txt);
//        passwordInput.ForceLabelUpdate();
//    }

//    private void UpdateEyeIcon()
//    {
//        if (!eyeIcon) return;
//        eyeIcon.sprite = _passwordVisible ? eyeOpen : eyeClosed;
//    }
//}

// Assets/Scripts/Auth/LoginPanelController.cs
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Focus visuals (optional)")]
    public Image usernameBg;       // фон инпута (Image на объекте поля)
    public Image passwordBg;
    public Image usernameFocusRing; // рамка поверх поля (опционально)
    public Image passwordFocusRing;

    [Header("Focus colors")]
    public Color bgNormal = new Color(0.92f, 0.92f, 0.92f, 1f);
    public Color bgFocused = Color.white;
    public Color ringFocused = new Color(0f, 0.6f, 0.2f, 1f);

    private const string RememberKey = "auth.remember";
    private bool _passwordVisible = false;

    void Awake()
    {
        if (loginButton) loginButton.onClick.AddListener(() => _ = DoLogin());

        if (rememberMeToggle)
            rememberMeToggle.isOn = PlayerPrefs.GetInt(RememberKey, 1) == 1;

        // стартуем со скрытым паролем (как было)
        SetPasswordVisibility(false);
        if (eyeButton) eyeButton.onClick.AddListener(ToggleEye);
        UpdateEyeIcon();

        // только стили каретки/фокуса — логика входа не трогается
        StyleInput(usernameInput);
        StyleInput(passwordInput);
        HookFocus(usernameInput, usernameBg, usernameFocusRing);
        HookFocus(passwordInput, passwordBg, passwordFocusRing);
        SetFocus(usernameBg, usernameFocusRing, false);
        SetFocus(passwordBg, passwordFocusRing, false);
    }

    void OnDestroy()
    {
        if (eyeButton) eyeButton.onClick.RemoveListener(ToggleEye);
        if (loginButton) loginButton.onClick.RemoveAllListeners();
    }

    // === LOGIN (как у тебя было) ===
    private async Task DoLogin()
    {
        string u = usernameInput ? usernameInput.text.Trim() : "";
        string p = passwordInput ? passwordInput.text : "";

        if (string.IsNullOrEmpty(u)) { SetStatus("Логин киргизиңиз"); return; }
        if (p.Length < 6) { SetStatus("Сыр сөз ≥ 6"); return; }

        if (rememberMeToggle) PlayerPrefs.SetInt(RememberKey, rememberMeToggle.isOn ? 1 : 0);

        SetStatus("Кирүү…");
        var (ok, err) = await AuthManager.Instance.SignIn(u, p); // ← твой вызов
        if (!ok) { SetStatus("Ката: " + err); return; }

        var ui = FindAnyObjectByType<UIManager>();
        if (ui) ui.OpenMainApp();
        SetStatus("");
    }

    private void SetStatus(string s) { if (statusText) statusText.text = s; }

    // === PASSWORD (оставлено по твоей схеме) ===
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

        // TMP требует переставить текст, чтобы применился contentType
        string txt = passwordInput.text;
        passwordInput.SetTextWithoutNotify(txt);
        passwordInput.ForceLabelUpdate();
    }

    private void UpdateEyeIcon()
    {
        if (!eyeIcon) return;
        eyeIcon.sprite = _passwordVisible ? eyeOpen : eyeClosed;
    }

    // ======= Вспомогательное (только визуал) =======
    private void StyleInput(TMP_InputField f)
    {
        if (!f) return;
        f.customCaretColor = true;
        f.caretColor = new Color32(0, 0, 0, 255);
        f.caretWidth = 3;
        f.caretBlinkRate = 0.6f;
        f.selectionColor = new Color(0, 0, 0, 0.2f);
#if UNITY_ANDROID || UNITY_IOS
        f.shouldHideMobileInput = false;
#endif
    }

    private void HookFocus(TMP_InputField input, Image bg, Image ring)
    {
        if (!input) return;
        input.onSelect.AddListener(_ => SetFocus(bg, ring, true));
        input.onDeselect.AddListener(_ => SetFocus(bg, ring, false));
    }

    private void SetFocus(Image bg, Image ring, bool focused)
    {
        if (bg) bg.color = focused ? bgFocused : bgNormal;
        if (ring)
        {
            ring.enabled = focused;
            if (focused) ring.color = ringFocused;
        }
    }
}
