//// Assets/Scripts/Auth/AuthPanelController.cs
//using System.Threading.Tasks;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class AuthPanelController : MonoBehaviour
//{
//    [Header("Inputs (optional, can autowire by names)")]
//    public TMP_InputField usernameInput;   // "UsernameInput"
//    public TMP_InputField realNameInput;   // "RealNameInput"
//    public TMP_InputField passwordInput;   // "PasswordInput"

//    [Header("Buttons")]
//    public Button signUpButton;            // "SignUpBtn"
//    public Button signInButton;            // "SignInBtn"
//    public Button signOutButton;           // "SignOutBtn"

//    [Header("Status")]
//    public TextMeshProUGUI statusText;     // "Status"

//    void Awake()
//    {
//        AutoWireIfNull();

//        if (signUpButton) signUpButton.onClick.AddListener(() => _ = OnSignUp());
//        if (signInButton) signInButton.onClick.AddListener(() => _ = OnSignIn());
//        if (signOutButton) signOutButton.onClick.AddListener(OnSignOut);

//        // Если пароль не настроен, подстрахуемся
//        if (passwordInput && passwordInput.contentType != TMP_InputField.ContentType.Password)
//            passwordInput.contentType = TMP_InputField.ContentType.Password;

//        Refresh();
//        LogMissing();
//    }

//    void OnEnable()
//    {
//        if (AuthManager.Instance != null)
//        {
//            AuthManager.Instance.OnSignedIn += _ => Refresh();
//            AuthManager.Instance.OnSignedOut += () => Refresh();
//        }
//    }
//    void OnDisable()
//    {
//        if (AuthManager.Instance != null)
//        {
//            AuthManager.Instance.OnSignedIn -= _ => Refresh();
//            AuthManager.Instance.OnSignedOut -= () => Refresh();
//        }
//    }

//    private void AutoWireIfNull()
//    {
//        // ищем по точным именам в дочерних объектах
//        if (!usernameInput) usernameInput = Find<TMP_InputField>("UsernameInput");
//        if (!realNameInput) realNameInput = Find<TMP_InputField>("RealNameInput");
//        if (!passwordInput) passwordInput = Find<TMP_InputField>("PasswordInput");

//        if (!signUpButton) signUpButton = Find<Button>("SignUpBtn");
//        if (!signInButton) signInButton = Find<Button>("SignInBtn");
//        if (!signOutButton) signOutButton = Find<Button>("SignOutBtn");

//        if (!statusText) statusText = Find<TextMeshProUGUI>("Status");
//    }

//    private T Find<T>(string name) where T : Component
//    {
//        var t = transform.Find(name);
//        return t ? t.GetComponent<T>() : null;
//    }

//    private void SetBusy(bool busy)
//    {
//        if (signUpButton) signUpButton.interactable = !busy;
//        if (signInButton) signInButton.interactable = !busy;
//        if (signOutButton) signOutButton.interactable = !busy;
//    }

//    private void SetStatus(string msg)
//    {
//        if (statusText) statusText.text = msg ?? "";
//        Debug.Log("[AuthUI] " + (msg ?? ""));
//    }

//    private void Refresh()
//    {
//        bool signedIn = AuthManager.Instance && AuthManager.Instance.CurrentUser != null;

//        if (signOutButton) signOutButton.gameObject.SetActive(signedIn);
//        if (realNameInput) realNameInput.gameObject.SetActive(!signedIn); // на входе реальное имя не нужно
//        if (signUpButton) signUpButton.gameObject.SetActive(!signedIn);
//        if (signInButton) signInButton.gameObject.SetActive(!signedIn);

//        if (!signedIn) SetStatus("");
//        else SetStatus("Вы вошли: " + AuthManager.Instance.CurrentUser.DisplayName);
//    }

//    private async Task OnSignUp()
//    {
//        string u = usernameInput ? usernameInput.text.Trim() : "";
//        string r = realNameInput ? realNameInput.text.Trim() : "";
//        string p = passwordInput ? passwordInput.text : "";
//        if (string.IsNullOrEmpty(u)) { SetStatus("Введите имя пользователя"); return; }
//        if (string.IsNullOrEmpty(r)) { SetStatus("Введите ваше имя"); return; }
//        if (p.Length < 6) { SetStatus("Пароль ? 6 символов"); return; }

//        SetBusy(true); SetStatus("Регистрация...");
//        var (ok, err) = await AuthManager.Instance.Register(u, r, p);
//        SetBusy(false);
//        SetStatus(ok ? "Готово! Вы вошли." : ("Ошибка: " + err));
//        Refresh();
//    }

//    private async Task OnSignIn()
//    {
//        string u = usernameInput ? usernameInput.text.Trim() : "";
//        string p = passwordInput ? passwordInput.text : "";
//        if (string.IsNullOrEmpty(u)) { SetStatus("Введите имя пользователя"); return; }
//        if (p.Length < 6) { SetStatus("Пароль ? 6 символов"); return; }

//        SetBusy(true); SetStatus("Вход...");
//        var (ok, err) = await AuthManager.Instance.SignIn(u, p);
//        SetBusy(false);
//        SetStatus(ok ? "Вы вошли" : ("Ошибка: " + err));
//        Refresh();
//    }

//    private void OnSignOut()
//    {
//        AuthManager.Instance.SignOut();
//        SetStatus("Вы вышли");
//        Refresh();
//    }

//    private void LogMissing()
//    {
//        if (!GetComponentInParent<Canvas>()) Debug.LogWarning("[AuthUI] Нет Canvas");
//        if (!usernameInput) Debug.LogWarning("[AuthUI] UsernameInput не найден");
//        if (!passwordInput) Debug.LogWarning("[AuthUI] PasswordInput не найден");
//        if (!signInButton) Debug.LogWarning("[AuthUI] SignInBtn не найден");
//        if (!signUpButton) Debug.LogWarning("[AuthUI] SignUpBtn не найден");
//        if (!statusText) Debug.LogWarning("[AuthUI] Status не найден");
//        if (!FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
//            Debug.LogWarning("[AuthUI] Нет EventSystem в сцене");
//    }
//}

// Assets/Scripts/Auth/AuthPanelController.cs
using System.Threading.Tasks;
using Firebase.Auth;          // для FirebaseUser в сигнатурах обработчиков
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthPanelController : MonoBehaviour
{
    [Header("Inputs (optional, can autowire by names)")]
    public TMP_InputField usernameInput;   // "UsernameInput"
    public TMP_InputField realNameInput;   // "RealNameInput"
    public TMP_InputField passwordInput;   // "PasswordInput"

    [Header("Buttons")]
    public Button signUpButton;            // "SignUpBtn"
    public Button signInButton;            // "SignInBtn"
    public Button signOutButton;           // "SignOutBtn"

    [Header("Status")]
    public TextMeshProUGUI statusText;     // "Status"

    // храним делегаты, чтобы корректно отписываться
    private System.Action<FirebaseUser> _onInHandler;
    private System.Action _onOutHandler;

    void Awake()
    {
        AutoWireIfNull();

        if (signUpButton) signUpButton.onClick.AddListener(() => _ = OnSignUp());
        if (signInButton) signInButton.onClick.AddListener(() => _ = OnSignIn());
        if (signOutButton) signOutButton.onClick.AddListener(OnSignOut);

        if (passwordInput && passwordInput.contentType != TMP_InputField.ContentType.Password)
            passwordInput.contentType = TMP_InputField.ContentType.Password;

        Refresh();
        LogMissing();
    }

    void OnEnable()
    {
        if (AuthManager.Instance != null)
        {
            _onInHandler = _ => Refresh();
            _onOutHandler = () => Refresh();

            AuthManager.Instance.OnSignedIn += _onInHandler;
            AuthManager.Instance.OnSignedOut += _onOutHandler;
        }
    }
    void OnDisable()
    {
        if (AuthManager.Instance != null)
        {
            if (_onInHandler != null) AuthManager.Instance.OnSignedIn -= _onInHandler;
            if (_onOutHandler != null) AuthManager.Instance.OnSignedOut -= _onOutHandler;
        }
    }

    private void AutoWireIfNull()
    {
        if (!usernameInput) usernameInput = Find<TMP_InputField>("UsernameInput");
        if (!realNameInput) realNameInput = Find<TMP_InputField>("RealNameInput");
        if (!passwordInput) passwordInput = Find<TMP_InputField>("PasswordInput");

        if (!signUpButton) signUpButton = Find<Button>("SignUpBtn");
        if (!signInButton) signInButton = Find<Button>("SignInBtn");
        if (!signOutButton) signOutButton = Find<Button>("SignOutBtn");

        if (!statusText) statusText = Find<TextMeshProUGUI>("Status");
    }

    private T Find<T>(string name) where T : Component
    {
        var t = transform.Find(name);
        return t ? t.GetComponent<T>() : null;
    }

    private void SetBusy(bool busy)
    {
        if (signUpButton) signUpButton.interactable = !busy;
        if (signInButton) signInButton.interactable = !busy;
        if (signOutButton) signOutButton.interactable = !busy;
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg ?? "";
        Debug.Log("[AuthUI] " + (msg ?? ""));
    }

    private void Refresh()
    {
        bool signedIn = AuthManager.Instance && AuthManager.Instance.CurrentUser != null;

        if (signOutButton) signOutButton.gameObject.SetActive(signedIn);
        if (realNameInput) realNameInput.gameObject.SetActive(!signedIn);
        if (signUpButton) signUpButton.gameObject.SetActive(!signedIn);
        if (signInButton) signInButton.gameObject.SetActive(!signedIn);

        if (!signedIn) SetStatus("");
        else SetStatus("Вы вошли: " + AuthManager.Instance.CurrentUser.DisplayName);
    }

    private async Task OnSignUp()
    {
        string u = usernameInput ? usernameInput.text.Trim() : "";
        string r = realNameInput ? realNameInput.text.Trim() : "";
        string p = passwordInput ? passwordInput.text : "";
        if (string.IsNullOrEmpty(u)) { SetStatus("Введите имя пользователя"); return; }
        if (string.IsNullOrEmpty(r)) { SetStatus("Введите ваше имя"); return; }
        if (p.Length < 6) { SetStatus("Пароль ? 6 символов"); return; }

        SetBusy(true); SetStatus("Регистрация...");
        var (ok, err) = await AuthManager.Instance.Register(u, r, p);
        SetBusy(false);
        SetStatus(ok ? "Готово! Вы вошли." : ("Ошибка: " + err));
        Refresh();
    }

    private async Task OnSignIn()
    {
        string u = usernameInput ? usernameInput.text.Trim() : "";
        string p = passwordInput ? passwordInput.text : "";
        if (string.IsNullOrEmpty(u)) { SetStatus("Введите имя пользователя"); return; }
        if (p.Length < 6) { SetStatus("Пароль ? 6 символов"); return; }

        SetBusy(true); SetStatus("Вход...");
        var (ok, err) = await AuthManager.Instance.SignIn(u, p);
        SetBusy(false);
        SetStatus(ok ? "Вы вошли" : ("Ошибка: " + err));
        Refresh();
    }

    private void OnSignOut()
    {
        AuthManager.Instance.SignOut();
        SetStatus("Вы вышли");
        Refresh();
    }

    private void LogMissing()
    {
        if (!GetComponentInParent<Canvas>()) Debug.LogWarning("[AuthUI] Нет Canvas");
        if (!usernameInput) Debug.LogWarning("[AuthUI] UsernameInput не найден");
        if (!passwordInput) Debug.LogWarning("[AuthUI] PasswordInput не найден");
        if (!signInButton) Debug.LogWarning("[AuthUI] SignInBtn не найден");
        if (!signUpButton) Debug.LogWarning("[AuthUI] SignUpBtn не найден");
        if (!statusText) Debug.LogWarning("[AuthUI] Status не найден");
        if (!FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
            Debug.LogWarning("[AuthUI] Нет EventSystem в сцене");
    }
}
