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
//        if (string.IsNullOrWhiteSpace(username)) return (false, "������� ��� ������������");
//        if (string.IsNullOrWhiteSpace(realName)) return (false, "������� ���� ���");
//        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return (false, "������ ? 6 ��������");

//        await Init();
//        string u = username.Trim().ToLowerInvariant();
//        string email = $"{u}@ort.app";

//        try
//        {
//            // 1) ��� ��������?
//            var unameDoc = _db.Collection("usernames").Document(u);
//            if ((await unameDoc.GetSnapshotAsync()).Exists) return (false, "��� ��� �����");

//            // 2) ������ ������������ (v13+: ���������� AuthResult)
//            AuthResult createRes = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
//            var user = createRes?.User;
//            if (user == null) return (false, "������� ������������ �� �������");

//            // 3) ������� + ������ ���� ������; createdAt = ServerTimestamp
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

//            // 4) displayName � Auth
//            await user.UpdateUserProfileAsync(new Firebase.Auth.UserProfile { DisplayName = realName });

//            OnSignedIn?.Invoke(user);
//            return (true, null);
//        }
//        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
//        catch (Exception e) { Debug.LogException(e); return (false, "����������� ������"); }
//    }

//    public async Task<(bool ok, string err)> SignIn(string username, string password)
//    {
//        await Init();
//        try
//        {
//            string email = $"{username.Trim().ToLowerInvariant()}@ort.app";
//            AuthResult signInRes = await _auth.SignInWithEmailAndPasswordAsync(email, password);
//            var user = signInRes?.User;
//            if (user == null) return (false, "�������� ������");

//            OnSignedIn?.Invoke(user);
//            return (true, null);
//        }
//        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
//        catch (Exception e) { Debug.LogException(e); return (false, "����������� ������"); }
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
//            case AuthError.EmailAlreadyInUse: return "��� ��� ������������";
//            case AuthError.WeakPassword: return "������ ������";
//            case AuthError.InvalidEmail: return "������������ ���";
//            case AuthError.WrongPassword: return "�������� ������";
//            case AuthError.UserNotFound: return "������������ �� ������";
//            default: return "������ �����������";
//        }
//    }
//}

// AuthManager.cs � �������� ��� ��� PointsService.SetCurrentUid(...)
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
        if (string.IsNullOrWhiteSpace(username)) return (false, "������� ��� ������������");
        if (string.IsNullOrWhiteSpace(realName)) return (false, "������� ���� ���");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return (false, "������ ? 6 ��������");

        await Init();
        string u = username.Trim().ToLowerInvariant();
        string email = $"{u}@ort.app";

        try
        {
            var unameDoc = _db.Collection("usernames").Document(u);
            if ((await unameDoc.GetSnapshotAsync()).Exists) return (false, "��� ��� �����");

            AuthResult createRes = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var user = createRes?.User;
            if (user == null) return (false, "������� ������������ �� �������");

            var userDoc = _db.Collection("users").Document(user.UserId);
            var batch = _db.StartBatch();
            batch.Set(userDoc, new { username = u, realName = realName, createdAt = FieldValue.ServerTimestamp }, SetOptions.MergeAll);
            batch.Set(unameDoc, new { uid = user.UserId });
            await batch.CommitAsync();

            await user.UpdateUserProfileAsync(new Firebase.Auth.UserProfile { DisplayName = realName });

            // NEW: ���������� PointsService � ����� ������������
            PointsService.SetCurrentUid(user.UserId);

            OnSignedIn?.Invoke(user);
            return (true, null);
        }
        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
        catch (Exception e) { Debug.LogException(e); return (false, "����������� ������"); }
    }

    public async Task<(bool ok, string err)> SignIn(string username, string password)
    {
        await Init();
        try
        {
            string email = $"{username.Trim().ToLowerInvariant()}@ort.app";
            AuthResult signInRes = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            var user = signInRes?.User;
            if (user == null) return (false, "�������� ������");

            // NEW: ���������� PointsService � �������� ������������
            PointsService.SetCurrentUid(user.UserId);

            OnSignedIn?.Invoke(user);
            return (true, null);
        }
        catch (FirebaseException fe) { return (false, MapAuthError(fe)); }
        catch (Exception e) { Debug.LogException(e); return (false, "����������� ������"); }
    }

    public void SignOut()
    {
        _auth?.SignOut();
        // NEW: ���������� �������� �����
        PointsService.SetCurrentUid(null);
        OnSignedOut?.Invoke();
    }

    private static string MapAuthError(FirebaseException fe)
    {
        var code = (AuthError)fe.ErrorCode;
        switch (code)
        {
            case AuthError.EmailAlreadyInUse: return "��� ��� ������������";
            case AuthError.WeakPassword: return "������ ������";
            case AuthError.InvalidEmail: return "������������ ���";
            case AuthError.WrongPassword: return "�������� ������";
            case AuthError.UserNotFound: return "������������ �� ������";
            default: return "������ �����������";
        }
    }
}
