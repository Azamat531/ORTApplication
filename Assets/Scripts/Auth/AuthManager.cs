//// Assets/Scripts/Auth/AuthManager.cs
//using System;
//using System.Threading.Tasks;
//using UnityEngine;
//using Firebase;
//using Firebase.Auth;
//using Firebase.Firestore;

//public class AuthManager : MonoBehaviour
//{
//    public static AuthManager Instance { get; private set; }
//    private FirebaseAuth _auth;
//    private FirebaseFirestore _db;

//    public FirebaseUser CurrentUser => _auth?.CurrentUser;
//    public event Action<FirebaseUser> OnSignedIn;
//    public event Action OnSignedOut;

//    private async void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this; DontDestroyOnLoad(gameObject);
//        await Init();
//    }

//    private async Task Init()
//    {
//        if (_auth != null && _db != null) return;
//        var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
//        if (dep != DependencyStatus.Available) { Debug.LogError("[Auth] Firebase deps: " + dep); return; }
//        _auth = FirebaseAuth.DefaultInstance;
//        _db = FirebaseFirestore.DefaultInstance;
//        Debug.Log("[Auth] Firebase initialized");
//    }

//    public async Task<(bool ok, string err)> Register(string username, string realName, string password)
//    {
//        if (string.IsNullOrWhiteSpace(username)) return (false, "Введите имя пользователя");
//        if (string.IsNullOrWhiteSpace(realName)) return (false, "Введите ваше имя");
//        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return (false, "Пароль ? 6 символов");

//        await Init();
//        string u = username.Trim().ToLowerInvariant();
//        string email = $"{u}@ort.app";

//        try
//        {
//            // 1) ник свободен?
//            var unameDoc = _db.Collection("usernames").Document(u);
//            if ((await unameDoc.GetSnapshotAsync()).Exists) return (false, "Ник уже занят");

//            // 2) создаём пользователя (v13+: возвращает AuthResult)
//            AuthResult createRes = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
//            var user = createRes?.User;
//            if (user == null) return (false, "Создать пользователя не удалось");

//            // 3) профиль + резерв ника батчем; createdAt = ServerTimestamp
//            var userDoc = _db.Collection("users").Document(user.UserId);
//            var batch = _db.StartBatch();
//            batch.Set(userDoc, new
//            {
//                username = u,
//                realName = realName,
//                createdAt = FieldValue.ServerTimestamp
//            }, SetOptions.MergeAll);
//            batch.Set(unameDoc, new { uid = user.UserId });
//            await batch.CommitAsync();

//            // 4) displayName в Auth
//            await user.UpdateUserProfileAsync(new Firebase.Auth.UserProfile { DisplayName = realName });

//            OnSignedIn?.Invoke(user);
//            return (true, null);
//        }
//        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
//        catch (Exception e) { Debug.LogException(e); return (false, "Неизвестная ошибка"); }
//    }

//    public async Task<(bool ok, string err)> SignIn(string username, string password)
//    {
//        await Init();
//        try
//        {
//            string email = $"{username.Trim().ToLowerInvariant()}@ort.app";
//            AuthResult signInRes = await _auth.SignInWithEmailAndPasswordAsync(email, password);
//            var user = signInRes?.User;
//            if (user == null) return (false, "Неверные данные");

//            OnSignedIn?.Invoke(user);
//            return (true, null);
//        }
//        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
//        catch (Exception e) { Debug.LogException(e); return (false, "Неизвестная ошибка"); }
//    }

//    public void SignOut()
//    {
//        _auth?.SignOut();
//        OnSignedOut?.Invoke();
//    }

//    private static string MapAuthError(FirebaseException fe)
//    {
//        var code = (AuthError)fe.ErrorCode;
//        switch (code)
//        {
//            case AuthError.EmailAlreadyInUse: return "Ник уже используется";
//            case AuthError.WeakPassword: return "Слабый пароль";
//            case AuthError.InvalidEmail: return "Некорректный ник";
//            case AuthError.WrongPassword: return "Неверный пароль";
//            case AuthError.UserNotFound: return "Пользователь не найден";
//            default: return "Ошибка авторизации";
//        }
//    }
//}

// AuthManager.cs — добавлен хук для PointsService.SetCurrentUid(...)
using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }
    private FirebaseAuth _auth;
    private FirebaseFirestore _db;

    public FirebaseUser CurrentUser => _auth?.CurrentUser;
    public event Action<FirebaseUser> OnSignedIn;
    public event Action OnSignedOut;

    private async void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        await Init();
    }

    private async Task Init()
    {
        if (_auth != null && _db != null) return;
        var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dep != DependencyStatus.Available) { Debug.LogError("[Auth] Firebase deps: " + dep); return; }
        _auth = FirebaseAuth.DefaultInstance;
        _db = FirebaseFirestore.DefaultInstance;
        Debug.Log("[Auth] Firebase initialized");
    }

    public async Task<(bool ok, string err)> Register(string username, string realName, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) return (false, "Введите имя пользователя");
        if (string.IsNullOrWhiteSpace(realName)) return (false, "Введите ваше имя");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return (false, "Пароль ? 6 символов");

        await Init();
        string u = username.Trim().ToLowerInvariant();
        string email = $"{u}@ort.app";

        try
        {
            var unameDoc = _db.Collection("usernames").Document(u);
            if ((await unameDoc.GetSnapshotAsync()).Exists) return (false, "Ник уже занят");

            AuthResult createRes = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var user = createRes?.User;
            if (user == null) return (false, "Создать пользователя не удалось");

            var userDoc = _db.Collection("users").Document(user.UserId);
            var batch = _db.StartBatch();
            batch.Set(userDoc, new { username = u, realName = realName, createdAt = FieldValue.ServerTimestamp }, SetOptions.MergeAll);
            batch.Set(unameDoc, new { uid = user.UserId });
            await batch.CommitAsync();

            await user.UpdateUserProfileAsync(new Firebase.Auth.UserProfile { DisplayName = realName });

            // NEW: уведомляем PointsService о новом пользователе
            PointsService.SetCurrentUid(user.UserId);

            OnSignedIn?.Invoke(user);
            return (true, null);
        }
        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
        catch (Exception e) { Debug.LogException(e); return (false, "Неизвестная ошибка"); }
    }

    public async Task<(bool ok, string err)> SignIn(string username, string password)
    {
        await Init();
        try
        {
            string email = $"{username.Trim().ToLowerInvariant()}@ort.app";
            AuthResult signInRes = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            var user = signInRes?.User;
            if (user == null) return (false, "Неверные данные");

            // NEW: уведомляем PointsService о вошедшем пользователе
            PointsService.SetCurrentUid(user.UserId);

            OnSignedIn?.Invoke(user);
            return (true, null);
        }
        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
        catch (Exception e) { Debug.LogException(e); return (false, "Неизвестная ошибка"); }
    }

    public void SignOut()
    {
        _auth?.SignOut();
        // NEW: сбрасываем привязку очков
        PointsService.SetCurrentUid(null);
        OnSignedOut?.Invoke();
    }

    private static string MapAuthError(FirebaseException fe)
    {
        var code = (AuthError)fe.ErrorCode;
        switch (code)
        {
            case AuthError.EmailAlreadyInUse: return "Ник уже используется";
            case AuthError.WeakPassword: return "Слабый пароль";
            case AuthError.InvalidEmail: return "Некорректный ник";
            case AuthError.WrongPassword: return "Неверный пароль";
            case AuthError.UserNotFound: return "Пользователь не найден";
            default: return "Ошибка авторизации";
        }
    }
}
