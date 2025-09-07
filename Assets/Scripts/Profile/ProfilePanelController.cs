//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Firebase.Auth;
//using Firebase.Firestore;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class ProfilePanelController : MonoBehaviour
//{
//    [Header("Texts")]
//    public TextMeshProUGUI uidText;        // 8-значный publicId
//    public TextMeshProUGUI usernameText;
//    public TextMeshProUGUI realNameText;
//    public TextMeshProUGUI statusText;

//    [Header("Top fields (tags)")]
//    public TextMeshProUGUI regionText;     // Область
//    public TextMeshProUGUI districtText;   // Район
//    public TextMeshProUGUI schoolText;     // Школа
//    public TextMeshProUGUI phoneText;      // Телефон

//    [Header("Main Buttons")]
//    public Button changePasswordButton;
//    public Button signOutButton;

//    [Header("Password Window")]
//    public GameObject passwordWindow;
//    public TMP_InputField currentPwInput;
//    public TMP_InputField newPw1Input;
//    public TMP_InputField newPw2Input;
//    public Button pwSaveButton;
//    public Button pwCancelButton;
//    public TextMeshProUGUI pwWindowStatus;

//    [Header("Admin (visibility & navigation)")]
//    public Button adminButton;
//    public GameObject adminMenuPanelRoot;
//    public bool adminButtonDefaultVisible = false;

//    private FirebaseAuth _auth;
//    private FirebaseFirestore _db;
//    private bool _alive;

//    // RNG для 8-значного ID
//    private static readonly System.Random Rng = new System.Random();

//    void Awake()
//    {
//        _auth = FirebaseAuth.DefaultInstance;
//        _db = FirebaseFirestore.DefaultInstance;

//        if (changePasswordButton) changePasswordButton.onClick.AddListener(OpenPasswordWindow);
//        if (signOutButton) signOutButton.onClick.AddListener(SignOut);

//        if (pwSaveButton) pwSaveButton.onClick.AddListener(() => _ = SavePassword());
//        if (pwCancelButton) pwCancelButton.onClick.AddListener(ClosePasswordWindow); // делегат без скобок

//        if (adminButton)
//        {
//            adminButton.onClick.AddListener(OpenAdminMenu);
//            adminButton.gameObject.SetActive(adminButtonDefaultVisible);
//        }
//    }

//    void OnEnable()
//    {
//        _alive = true;
//        HookAuthEvents(true);
//        _ = LoadProfile();
//        _ = RefreshAdminGate();
//    }

//    void OnDisable()
//    {
//        _alive = false;
//        HookAuthEvents(false);
//    }

//    private void HookAuthEvents(bool on)
//    {
//        if (AuthManager.Instance == null) return;
//        if (on)
//        {
//            AuthManager.Instance.OnSignedIn += HandleSignedIn;
//            AuthManager.Instance.OnSignedOut += HandleSignedOut;
//        }
//        else
//        {
//            AuthManager.Instance.OnSignedIn -= HandleSignedIn;
//            AuthManager.Instance.OnSignedOut -= HandleSignedOut;
//        }
//    }

//    private void HandleSignedIn(FirebaseUser u)
//    {
//        if (!_alive) return;
//        _ = LoadProfile();
//        _ = RefreshAdminGate();
//    }

//    private void HandleSignedOut()
//    {
//        if (!_alive) return;
//        if (uidText) uidText.text = "";
//        if (usernameText) usernameText.text = "";
//        if (realNameText) realNameText.text = "";
//        SetTopFields("—", "—", "—", "—");
//        SetStatus("Не авторизован");
//        if (adminButton) adminButton.gameObject.SetActive(false);
//    }

//    // ================= PROFILE LOAD =================
//    private async Task LoadProfile()
//    {
//        SetStatus("");
//        var user = _auth.CurrentUser;
//        if (user == null) { HandleSignedOut(); return; }

//        try
//        {
//            var userDoc = _db.Collection("users").Document(user.UserId);
//            var snap = await userDoc.GetSnapshotAsync();
//            if (!_alive) return;

//            string username = "";
//            string realName = user.DisplayName ?? "";
//            if (snap.Exists)
//            {
//                if (snap.TryGetValue("username", out string u)) username = u;
//                if (snap.TryGetValue("realName", out string rn)) realName = rn;
//            }
//            if (usernameText) usernameText.text = username;
//            if (realNameText) realNameText.text = realName;

//            // ----- Область/Район/Школа/Телефон -----
//            string region = GetStr(snap, "region", "oblast", "area", "province");
//            string district = GetStr(snap, "district", "raion");
//            string school = GetStr(snap, "school", "schoolName");
//            string phone = FormatPhone(GetStr(snap, "phone", "phoneNumber", "tel"));

//            SetTopFields(region, district, school, phone);

//            // ----- Публичный 8-значный ID -----
//            string publicId = await GetOrCreatePublicId(user.UserId, snap);
//            if (!_alive) return;
//            if (uidText) uidText.text = publicId;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            SetStatus("Ошибка загрузки профиля");
//        }
//    }

//    private void SetTopFields(string region, string district, string school, string phone)
//    {
//        if (regionText) regionText.text = string.IsNullOrWhiteSpace(region) ? "—" : region;
//        if (districtText) districtText.text = string.IsNullOrWhiteSpace(district) ? "—" : district;
//        if (schoolText) schoolText.text = string.IsNullOrWhiteSpace(school) ? "—" : school;
//        if (phoneText) phoneText.text = string.IsNullOrWhiteSpace(phone) ? "—" : phone;
//    }

//    private static string GetStr(DocumentSnapshot snap, params string[] keys)
//    {
//        if (snap == null || !snap.Exists) return "";
//        foreach (var k in keys)
//        {
//            if (snap.ContainsField(k))
//            {
//                try { var v = snap.GetValue<string>(k); if (!string.IsNullOrWhiteSpace(v)) return v; } catch { }
//            }
//        }
//        return "";
//    }

//    private static string FormatPhone(string p)
//    {
//        if (string.IsNullOrWhiteSpace(p)) return "";
//        var digits = new string(p.Where(char.IsDigit).ToArray());
//        return digits; // если нужна маска — скажи формат, добавлю
//    }

//    // ====== 8-ЗНАЧНЫЙ ПУБЛИЧНЫЙ ID (через транзакцию) ======
//    private async Task<string> GetOrCreatePublicId(string uid, DocumentSnapshot knownSnap = null)
//    {
//        if (knownSnap != null && knownSnap.Exists &&
//            knownSnap.TryGetValue("publicId", out string existing) &&
//            IsValid8(existing))
//            return existing;

//        var userDoc = _db.Collection("users").Document(uid);

//        if (knownSnap == null)
//        {
//            var s = await userDoc.GetSnapshotAsync();
//            if (s.Exists && s.TryGetValue("publicId", out string ex) && IsValid8(ex))
//                return ex;
//        }

//        for (int attempt = 0; attempt < 12; attempt++)
//        {
//            string candidate = Random8();
//            var reservation = _db.Collection("publicIds").Document(candidate);

//            try
//            {
//                await _db.RunTransactionAsync<object>(async tx =>
//                {
//                    var resSnap = await tx.GetSnapshotAsync(reservation);
//                    if (resSnap.Exists)
//                        throw new InvalidOperationException("exists");

//                    tx.Set(reservation, new Dictionary<string, object>
//                    {
//                        { "ownerUid",  uid },
//                        { "createdAt", Timestamp.GetCurrentTimestamp() }
//                    });

//                    tx.Set(userDoc, new Dictionary<string, object>
//                    {
//                        { "publicId", candidate }
//                    }, SetOptions.MergeAll);

//                    return null;
//                });

//                return candidate;
//            }
//            catch (Exception e)
//            {
//                var m = (e.Message ?? "").ToLowerInvariant();
//                if (m.Contains("exist")) continue;   // занято — пробуем другой
//                Debug.LogException(e);
//                throw;
//            }
//        }

//        throw new Exception("Не удалось выдать уникальный 8-значный ID");
//    }

//    private static string Random8()
//    {
//        int hi = Rng.Next(0, 10000);
//        int lo = Rng.Next(0, 10000);
//        return $"{hi:D4}{lo:D4}";
//    }

//    private static bool IsValid8(string s) => !string.IsNullOrEmpty(s) && s.Length == 8 && s.All(char.IsDigit);

//    // ================= PASSWORD CHANGE =================
//    private void OpenPasswordWindow()
//    {
//        if (pwWindowStatus) pwWindowStatus.text = "";
//        if (currentPwInput) currentPwInput.text = "";
//        if (newPw1Input) newPw1Input.text = "";
//        if (newPw2Input) newPw2Input.text = "";
//        if (passwordWindow) passwordWindow.SetActive(true);
//    }

//    private void ClosePasswordWindow()
//    {
//        if (passwordWindow) passwordWindow.SetActive(false);
//    }

//    private async Task SavePassword()
//    {
//        var user = _auth.CurrentUser;
//        if (user == null) { if (pwWindowStatus) pwWindowStatus.text = "Не авторизован"; return; }

//        var current = currentPwInput ? currentPwInput.text : "";
//        var p1 = newPw1Input ? newPw1Input.text : "";
//        var p2 = newPw2Input ? newPw2Input.text : "";

//        if (string.IsNullOrEmpty(p1) || p1.Length < 6) { if (pwWindowStatus) pwWindowStatus.text = "Пароль ≥ 6"; return; }
//        if (p1 != p2) { if (pwWindowStatus) pwWindowStatus.text = "Пароли не совпадают"; return; }

//        try
//        {
//            var email = user.Email;
//            if (string.IsNullOrEmpty(email)) { if (pwWindowStatus) pwWindowStatus.text = "У аккаунта нет email"; return; }

//            var cred = EmailAuthProvider.GetCredential(email, current);
//            await user.ReauthenticateAsync(cred);
//            await user.UpdatePasswordAsync(p1);

//            if (!_alive) return;
//            if (pwWindowStatus) pwWindowStatus.text = "Готово";
//            ClosePasswordWindow();
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            if (pwWindowStatus) pwWindowStatus.text = "Ошибка смены пароля";
//        }
//    }

//    // ================= ADMIN GATE =================
//    private async Task RefreshAdminGate()
//    {
//        if (!adminButton) return;

//        adminButton.gameObject.SetActive(adminButtonDefaultVisible);

//        var user = _auth.CurrentUser;
//        if (user == null) { adminButton.gameObject.SetActive(false); return; }

//        try
//        {
//            var snap = await _db.Collection("users").Document(user.UserId).GetSnapshotAsync();
//            if (!_alive) return;

//            bool isAdmin = false;
//            if (snap.Exists && snap.TryGetValue("role", out string role))
//                isAdmin = (role?.ToLowerInvariant() == "admin");

//            adminButton.gameObject.SetActive(isAdmin);
//        }
//        catch
//        {
//            adminButton.gameObject.SetActive(false);
//        }
//    }

//    private void OpenAdminMenu()
//    {
//        if (adminMenuPanelRoot) adminMenuPanelRoot.SetActive(true);
//    }

//    private void SignOut()
//    {
//        try
//        {
//            if (passwordWindow) passwordWindow.SetActive(false);
//            if (adminMenuPanelRoot) adminMenuPanelRoot.SetActive(false);
//            gameObject.SetActive(false);

//            _auth.SignOut();

//            var ui = FindAnyObjectByType<UIManager>();
//            if (ui != null) ui.OpenLoginPanel();
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            SetStatus("Ошибка выхода");
//        }
//    }

//    private void SetStatus(string msg)
//    {
//        if (statusText) statusText.text = msg ?? string.Empty;
//        if (!string.IsNullOrEmpty(msg)) Debug.Log("[Profile] " + msg);
//    }
//}

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanelController : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI uidText;        // 8-значный publicId
    public TextMeshProUGUI usernameText;   // Логин
    public TextMeshProUGUI realNameText;
    public TextMeshProUGUI statusText;

    [Header("Top fields")]
    public TextMeshProUGUI regionText;     // Область
    public TextMeshProUGUI districtText;   // Район
    public TextMeshProUGUI schoolText;     // Школа
    public TextMeshProUGUI phoneText;      // Телефон

    [Header("Main Buttons")]
    public Button changePasswordButton;
    public Button signOutButton;

    [Header("Password Window")]
    public GameObject passwordWindow;
    public TMP_InputField currentPwInput;
    public TMP_InputField newPw1Input;
    public TMP_InputField newPw2Input;
    public Button pwSaveButton;
    public Button pwCancelButton;
    public TextMeshProUGUI pwWindowStatus;

    [Header("Admin")]
    public Button adminButton;
    public GameObject adminMenuPanelRoot;
    public bool adminButtonDefaultVisible = false;

    private FirebaseAuth _auth;
    private FirebaseFirestore _db;
    private bool _alive;

    private static readonly System.Random Rng = new System.Random();

    void Awake()
    {
        _auth = FirebaseAuth.DefaultInstance;
        _db = FirebaseFirestore.DefaultInstance;

        if (changePasswordButton) changePasswordButton.onClick.AddListener(OpenPasswordWindow);
        if (signOutButton) signOutButton.onClick.AddListener(SignOut);

        if (pwSaveButton) pwSaveButton.onClick.AddListener(() => _ = SavePassword());
        if (pwCancelButton) pwCancelButton.onClick.AddListener(ClosePasswordWindow);

        if (adminButton)
        {
            adminButton.onClick.AddListener(OpenAdminMenu);
            adminButton.gameObject.SetActive(adminButtonDefaultVisible);
        }
    }

    void OnEnable()
    {
        _alive = true;
        HookAuthEvents(true);
        _ = LoadProfile();
        _ = RefreshAdminGate();
    }

    void OnDisable()
    {
        _alive = false;
        HookAuthEvents(false);
    }

    private void HookAuthEvents(bool on)
    {
        if (AuthManager.Instance == null) return;
        if (on)
        {
            AuthManager.Instance.OnSignedIn += HandleSignedIn;
            AuthManager.Instance.OnSignedOut += HandleSignedOut;
        }
        else
        {
            AuthManager.Instance.OnSignedIn -= HandleSignedIn;
            AuthManager.Instance.OnSignedOut -= HandleSignedOut;
        }
    }

    private void HandleSignedIn(FirebaseUser u) { if (_alive) { _ = LoadProfile(); _ = RefreshAdminGate(); } }

    private void HandleSignedOut()
    {
        if (!_alive) return;
        if (uidText) uidText.text = "";
        if (usernameText) usernameText.text = "";
        if (realNameText) realNameText.text = "";
        SetTopFields("—", "—", "—", "—");
        SetStatus("Не авторизован");
        if (adminButton) adminButton.gameObject.SetActive(false);
    }

    // ================= PROFILE LOAD =================
    private async Task LoadProfile()
    {
        SetStatus("");
        var user = _auth.CurrentUser;
        if (user == null) { HandleSignedOut(); return; }

        try
        {
            var userRef = _db.Collection("users").Document(user.UserId);
            var snap = await userRef.GetSnapshotAsync();
            if (!_alive) return;

            // Публичные поля
            string realName = user.DisplayName ?? "";
            if (snap.Exists && snap.TryGetValue("realName", out string rn)) realName = rn;
            if (realNameText) realNameText.text = realName;

            string region = GetStr(snap, "region", "oblast", "area", "province");
            string district = GetStr(snap, "district", "raion");
            string school = GetStr(snap, "school", "schoolName");
            string phone = FormatPhone(GetStr(snap, "phone", "phoneNumber", "tel"));
            SetTopFields(region, district, school, phone);

            // ЛОГИН (username) — по приоритетам:
            string login = await ResolveUsername(user, userRef, snap);
            if (usernameText) usernameText.text = string.IsNullOrEmpty(login) ? "—" : login;

            // Публичный 8-значный ID
            string publicId = await GetOrCreatePublicId(userRef, snap);
            if (!_alive) return;
            if (uidText) uidText.text = publicId;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus("Ошибка загрузки профиля");
        }
    }

    // Приоритеты username: users.username → users/private/account.username → emailPrefix → DisplayName
    private async Task<string> ResolveUsername(FirebaseUser user, DocumentReference userRef, DocumentSnapshot publicSnap)
    {
        // 1) users/{uid}.username (публичное поле)
        if (publicSnap != null && publicSnap.Exists &&
            publicSnap.TryGetValue("username", out string u) &&
            !string.IsNullOrWhiteSpace(u))
            return NormalizeLogin(u);

        // 2) users/{uid}/private/account.username (если используешь приватно)
        try
        {
            var priv = await userRef.Collection("private").Document("account").GetSnapshotAsync();
            if (priv.Exists && priv.TryGetValue("username", out string pu) && !string.IsNullOrWhiteSpace(pu))
                return NormalizeLogin(pu);
        }
        catch { /* приватного дока может не быть — это нормально */ }

        // 3) email до @
        var email = user.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            int at = email.IndexOf('@');
            if (at > 0) return NormalizeLogin(email.Substring(0, at));
        }

        // 4) DisplayName (последний вариант)
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return NormalizeLogin(user.DisplayName);

        return "";
    }

    private static string NormalizeLogin(string s)
    {
        var t = (s ?? "").Trim();
        // если нужно строго в нижнем регистре — раскомментируй следующую строку:
        // t = t.ToLowerInvariant();
        return t;
    }

    private void SetTopFields(string region, string district, string school, string phone)
    {
        if (regionText) regionText.text = string.IsNullOrWhiteSpace(region) ? "—" : region;
        if (districtText) districtText.text = string.IsNullOrWhiteSpace(district) ? "—" : district;
        if (schoolText) schoolText.text = string.IsNullOrWhiteSpace(school) ? "—" : school;
        if (phoneText) phoneText.text = string.IsNullOrWhiteSpace(phone) ? "—" : phone;
    }

    private static string GetStr(DocumentSnapshot snap, params string[] keys)
    {
        if (snap == null || !snap.Exists) return "";
        foreach (var k in keys)
        {
            if (snap.ContainsField(k))
            {
                try { var v = snap.GetValue<string>(k); if (!string.IsNullOrWhiteSpace(v)) return v; } catch { }
            }
        }
        return "";
    }

    private static string FormatPhone(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return "";
        var digits = new string(p.Where(char.IsDigit).ToArray());
        return digits;
    }

    // ====== 8-ЗНАЧНЫЙ ID (через транзакцию) ======
    private async Task<string> GetOrCreatePublicId(DocumentReference userRef, DocumentSnapshot knownSnap = null)
    {
        if (knownSnap != null && knownSnap.Exists &&
            knownSnap.TryGetValue("publicId", out string existing) &&
            IsValid8(existing))
            return existing;

        // перечитать, если снапа не было
        if (knownSnap == null)
        {
            var s = await userRef.GetSnapshotAsync();
            if (s.Exists && s.TryGetValue("publicId", out string ex) && IsValid8(ex))
                return ex;
        }

        var db = userRef.Firestore; // <-- фикс

        for (int attempt = 0; attempt < 12; attempt++)
        {
            string candidate = Random8();
            var reservation = db.Collection("publicIds").Document(candidate);

            try
            {
                await db.RunTransactionAsync<object>(async tx =>
                {
                    var resSnap = await tx.GetSnapshotAsync(reservation);
                    if (resSnap.Exists) throw new InvalidOperationException("exists");

                    tx.Set(reservation, new Dictionary<string, object>
                {
                    { "ownerUid",  userRef.Id },
                    { "createdAt", Timestamp.GetCurrentTimestamp() }
                });

                    tx.Set(userRef, new Dictionary<string, object>
                {
                    { "publicId", candidate }
                }, SetOptions.MergeAll);

                    return null;
                });

                return candidate; // транзакция прошла — ID закреплён
            }
            catch (Exception e)
            {
                var m = (e.Message ?? "").ToLowerInvariant();
                if (m.Contains("exist")) continue; // занято — пробуем другой
                Debug.LogException(e);
                throw;
            }
        }

        throw new Exception("Не удалось выдать уникальный 8-значный ID");
    }


    private static string Random8()
    {
        int hi = Rng.Next(0, 10000);
        int lo = Rng.Next(0, 10000);
        return $"{hi:D4}{lo:D4}";
    }

    private static bool IsValid8(string s) => !string.IsNullOrEmpty(s) && s.Length == 8 && s.All(char.IsDigit);

    // ===== Password =====
    private void OpenPasswordWindow()
    {
        if (pwWindowStatus) pwWindowStatus.text = "";
        if (currentPwInput) currentPwInput.text = "";
        if (newPw1Input) newPw1Input.text = "";
        if (newPw2Input) newPw2Input.text = "";
        if (passwordWindow) passwordWindow.SetActive(true);
    }
    private void ClosePasswordWindow() { if (passwordWindow) passwordWindow.SetActive(false); }

    private async Task SavePassword()
    {
        var user = _auth.CurrentUser;
        if (user == null) { if (pwWindowStatus) pwWindowStatus.text = "Не авторизован"; return; }

        var current = currentPwInput ? currentPwInput.text : "";
        var p1 = newPw1Input ? newPw1Input.text : "";
        var p2 = newPw2Input ? newPw2Input.text : "";
        if (string.IsNullOrEmpty(p1) || p1.Length < 6) { if (pwWindowStatus) pwWindowStatus.text = "Пароль ≥ 6"; return; }
        if (p1 != p2) { if (pwWindowStatus) pwWindowStatus.text = "Пароли не совпадают"; return; }

        try
        {
            var email = user.Email;
            if (string.IsNullOrEmpty(email)) { if (pwWindowStatus) pwWindowStatus.text = "У аккаунта нет email"; return; }

            var cred = EmailAuthProvider.GetCredential(email, current);
            await user.ReauthenticateAsync(cred);
            await user.UpdatePasswordAsync(p1);

            if (!_alive) return;
            if (pwWindowStatus) pwWindowStatus.text = "Готово";
            ClosePasswordWindow();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (pwWindowStatus) pwWindowStatus.text = "Ошибка смены пароля";
        }
    }

    // ===== Admin =====
    private async Task RefreshAdminGate()
    {
        if (!adminButton) return;
        adminButton.gameObject.SetActive(adminButtonDefaultVisible);

        var user = _auth.CurrentUser;
        if (user == null) { adminButton.gameObject.SetActive(false); return; }

        try
        {
            var snap = await _db.Collection("users").Document(user.UserId).GetSnapshotAsync();
            if (!_alive) return;

            bool isAdmin = false;
            if (snap.Exists && snap.TryGetValue("role", out string role))
                isAdmin = (role?.ToLowerInvariant() == "admin");

            adminButton.gameObject.SetActive(isAdmin);
        }
        catch { adminButton.gameObject.SetActive(false); }
    }

    private void OpenAdminMenu() { if (adminMenuPanelRoot) adminMenuPanelRoot.SetActive(true); }
    private void SignOut()
    {
        try
        {
            if (passwordWindow) passwordWindow.SetActive(false);
            if (adminMenuPanelRoot) adminMenuPanelRoot.SetActive(false);
            gameObject.SetActive(false);
            _auth.SignOut();
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.OpenLoginPanel();
        }
        catch (Exception e) { Debug.LogException(e); SetStatus("Ошибка выхода"); }
    }
    private void SetStatus(string msg) { if (statusText) statusText.text = msg ?? string.Empty; if (!string.IsNullOrEmpty(msg)) Debug.Log("[Profile] " + msg); }
}
