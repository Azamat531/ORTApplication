//// Assets/Scripts/Auth/RegisterPanelController.cs
//using System.Threading.Tasks;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class RegisterPanelController : MonoBehaviour
//{
//    [Header("Register UI")]
//    public TMP_InputField usernameInput;   // ник
//    public TMP_InputField realNameInput;   // настоящее имя
//    public TMP_InputField passwordInput;   // пароль
//    public TMP_InputField confirmInput;    // подтверждение
//    public TextMeshProUGUI statusText;
//    public Button registerButton;

//    [Header("Password Eyes (icons shared)")]
//    public Sprite eyeOpen;
//    public Sprite eyeClosed;

//    [Header("Password #1 Eye")]
//    public Button pass1EyeButton;
//    public Image pass1EyeIcon;

//    [Header("Password #2 Eye")]
//    public Button pass2EyeButton;
//    public Image pass2EyeIcon;

//    [Header("Focus visuals (optional)")]
//    public Image usernameBg, realNameBg, passwordBg, password2Bg;
//    public Image usernameFocusRing, realNameFocusRing, passwordFocusRing, password2FocusRing;

//    [Header("Focus colors")]
//    public Color bgNormal = new Color(0.92f, 0.92f, 0.92f, 1f);
//    public Color bgFocused = Color.white;
//    public Color ringFocused = new Color(0f, 0.6f, 0.2f, 1f);

//    private bool _p1Visible = false;
//    private bool _p2Visible = false;

//    void Awake()
//    {
//        if (registerButton) registerButton.onClick.AddListener(() => _ = DoRegister());

//        // старт — оба поля скрыты (• • • •), как было у тебя
//        SetPasswordVisibility(passwordInput, pass1EyeIcon, false);
//        SetPasswordVisibility(confirmInput, pass2EyeIcon, false);

//        if (pass1EyeButton) pass1EyeButton.onClick.AddListener(() =>
//            TogglePasswordVisibility(passwordInput, pass1EyeIcon, ref _p1Visible));
//        if (pass2EyeButton) pass2EyeButton.onClick.AddListener(() =>
//            TogglePasswordVisibility(confirmInput, pass2EyeIcon, ref _p2Visible));

//        // только визуал каретки/фокуса
//        StyleInput(usernameInput);
//        StyleInput(realNameInput);
//        StyleInput(passwordInput);
//        StyleInput(confirmInput);

//        HookFocus(usernameInput, usernameBg, usernameFocusRing);
//        HookFocus(realNameInput, realNameBg, realNameFocusRing);
//        HookFocus(passwordInput, passwordBg, passwordFocusRing);
//        HookFocus(confirmInput, password2Bg, password2FocusRing);

//        SetFocus(usernameBg, usernameFocusRing, false);
//        SetFocus(realNameBg, realNameFocusRing, false);
//        SetFocus(passwordBg, passwordFocusRing, false);
//        SetFocus(password2Bg, password2FocusRing, false);
//    }

//    private async Task DoRegister()
//    {
//        string u = usernameInput ? usernameInput.text.Trim() : "";
//        string rn = realNameInput ? realNameInput.text.Trim() : "";
//        string p1 = passwordInput ? passwordInput.text : "";
//        string p2 = confirmInput ? confirmInput.text : "";

//        if (string.IsNullOrEmpty(u)) { SetStatus("Логин киргизиңиз"); return; }
//        if (string.IsNullOrEmpty(rn)) { SetStatus("Атыңызды киргизиңиз"); return; }
//        if (p1.Length < 6) { SetStatus("Сыр сөз ≥ 6"); return; }
//        if (p1 != p2) { SetStatus("Сыр сөздөр дал келбейт"); return; }

//        SetStatus("Катталуу…");
//        var (ok, err) = await AuthManager.Instance.Register(u, rn, p1); // ← твой вызов
//        if (!ok) { SetStatus("Ката: " + err); return; }

//        var ui = FindAnyObjectByType<UIManager>();
//        if (ui) ui.OpenMainApp();
//        SetStatus("");
//    }

//    private void SetStatus(string s) { if (statusText) statusText.text = s; }

//    // === Показ/скрытие пароля (как у тебя) ===
//    private void TogglePasswordVisibility(TMP_InputField input, Image icon, ref bool state)
//    {
//        state = !state;
//        SetPasswordVisibility(input, icon, state);
//    }

//    private void SetPasswordVisibility(TMP_InputField input, Image icon, bool visible)
//    {
//        if (!input) return;

//        input.contentType = visible
//            ? TMP_InputField.ContentType.Standard
//            : TMP_InputField.ContentType.Password;

//        string txt = input.text;
//        input.SetTextWithoutNotify(txt);
//        input.ForceLabelUpdate();

//        if (icon) icon.sprite = visible ? eyeOpen : eyeClosed;
//    }

//    // ===== Визуал фокуса/каретки =====
//    private void StyleInput(TMP_InputField f)
//    {
//        if (!f) return;
//        f.customCaretColor = true;
//        f.caretColor = new Color32(0, 0, 0, 255);
//        f.caretWidth = 3;
//        f.caretBlinkRate = 0.6f;
//        f.selectionColor = new Color(0, 0, 0, 0.2f);
//#if UNITY_ANDROID || UNITY_IOS
//        f.shouldHideMobileInput = false;
//#endif
//    }

//    private void HookFocus(TMP_InputField input, Image bg, Image ring)
//    {
//        if (!input) return;
//        input.onSelect.AddListener(_ => SetFocus(bg, ring, true));
//        input.onDeselect.AddListener(_ => SetFocus(bg, ring, false));
//    }

//    private void SetFocus(Image bg, Image ring, bool focused)
//    {
//        if (bg) bg.color = focused ? bgFocused : bgNormal;
//        if (ring)
//        {
//            ring.enabled = focused;
//            if (focused) ring.color = ringFocused;
//        }
//    }
//}

// Assets/Scripts/Auth/RegisterPanelController.cs
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterPanelController : MonoBehaviour
{
    [Header("Register UI")]
    public TMP_InputField usernameInput;   // ник
    public TMP_InputField realNameInput;   // настоящее имя
    public TMP_InputField passwordInput;   // пароль
    public TMP_InputField confirmInput;    // подтверждение
    public TextMeshProUGUI statusText;
    public Button registerButton;

    [Header("Password Eyes (icons shared)")]
    public Sprite eyeOpen;
    public Sprite eyeClosed;

    [Header("Password #1 Eye")]
    public Button pass1EyeButton;
    public Image pass1EyeIcon;

    [Header("Password #2 Eye")]
    public Button pass2EyeButton;
    public Image pass2EyeIcon;

    private bool _p1Visible = false;
    private bool _p2Visible = false;

    void Awake()
    {
        if (registerButton) registerButton.onClick.AddListener(() => _ = DoRegister());

        // старт — оба поля скрыты (• • • •), как у тебя
        SetPasswordVisibility(passwordInput, pass1EyeIcon, false);
        SetPasswordVisibility(confirmInput, pass2EyeIcon, false);

        if (pass1EyeButton) pass1EyeButton.onClick.AddListener(() =>
            TogglePasswordVisibility(passwordInput, pass1EyeIcon, ref _p1Visible));
        if (pass2EyeButton) pass2EyeButton.onClick.AddListener(() =>
            TogglePasswordVisibility(confirmInput, pass2EyeIcon, ref _p2Visible));

        // Стили каретки/поведение на мобилках (ничего из логики не трогаем)
        StyleInput(usernameInput);
        StyleInput(realNameInput);
        StyleInput(passwordInput);
        StyleInput(confirmInput);
    }

    void OnDestroy()
    {
        if (registerButton) registerButton.onClick.RemoveAllListeners();
        if (pass1EyeButton) pass1EyeButton.onClick.RemoveAllListeners();
        if (pass2EyeButton) pass2EyeButton.onClick.RemoveAllListeners();
    }

    // === REGISTRATION (твоя логика) ===
    private async Task DoRegister()
    {
        string u = usernameInput ? usernameInput.text.Trim() : "";
        string rn = realNameInput ? realNameInput.text.Trim() : "";
        string p1 = passwordInput ? passwordInput.text : "";
        string p2 = confirmInput ? confirmInput.text : "";

        if (string.IsNullOrEmpty(u)) { SetStatus("Логин киргизиңиз"); return; }
        if (string.IsNullOrEmpty(rn)) { SetStatus("Атыңызды киргизиңиз"); return; }
        if (p1.Length < 6) { SetStatus("Сыр сөз ≥ 6"); return; }
        if (p1 != p2) { SetStatus("Сыр сөздөр дал келбейт"); return; }

        SetStatus("Катталуу…");
        var (ok, err) = await AuthManager.Instance.Register(u, rn, p1); // ← твой вызов
        if (!ok) { SetStatus("Ката: " + err); return; }

        var ui = FindAnyObjectByType<UIManager>();
        if (ui) ui.OpenMainApp();
        SetStatus("");
    }

    private void SetStatus(string s) { if (statusText) statusText.text = s; }

    // === Показ/скрытие паролей (твоя схема) ===
    private void TogglePasswordVisibility(TMP_InputField input, Image icon, ref bool state)
    {
        state = !state;
        SetPasswordVisibility(input, icon, state);
    }

    private void SetPasswordVisibility(TMP_InputField input, Image icon, bool visible)
    {
        if (!input) return;

        input.contentType = visible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        // Переставить текст, чтобы применился contentType
        string txt = input.text;
        input.SetTextWithoutNotify(txt);
        input.ForceLabelUpdate();

        if (icon) icon.sprite = visible ? eyeOpen : eyeClosed;
    }

    // === Каретка и стабильный фокус на Android/iOS ===
    private void StyleInput(TMP_InputField f)
    {
        if (!f) return;

        f.customCaretColor = true;
        f.caretColor = new Color32(0, 0, 0, 255);
        f.caretWidth = 2;
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
        yield return null; // дождаться кадра с поднятой клавиатурой
        if (!f) yield break;
        f.ActivateInputField();
        f.MoveTextEnd(false);
        f.ForceLabelUpdate();
    }
}
