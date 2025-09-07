//using Firebase.Auth;
//using Firebase.Firestore;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public static class PointsService
//{
//    static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;
//    static FirebaseFirestore DB => FirebaseFirestore.DefaultInstance;

//    // ---- ЛОКАЛЬНО: мгновенно ----
//    public static int AddPoints(string subjectId, string topicId, int delta)
//    {
//        LocalPointsStore.Load();
//        var newPts = LocalPointsStore.Add(subjectId, topicId, delta);
//        LocalPointsStore.Save();
//        return newPts;
//    }

//    public static int GetPoints(string subjectId, string topicId)
//    {
//        LocalPointsStore.Load();
//        return LocalPointsStore.Get(subjectId, topicId);
//    }

//    // ---- СИНК ПРИ ВХОДЕ: подтянуть сервер, если он свежее ----
//    public static IEnumerator SyncOnEnter()
//    {
//        LocalPointsStore.Load();
//        var uid = Auth.CurrentUser?.UserId;
//        if (string.IsNullOrEmpty(uid)) yield break;

//        var docRef = DB.Collection("users").Document(uid);
//        var task = docRef.GetSnapshotAsync();
//        yield return new WaitUntil(() => task.IsCompleted);

//        if (task.Exception != null) { Debug.LogWarning("[PointsSync] enter failed: " + task.Exception.Message); yield break; }

//        var snap = task.Result;
//        if (!snap.Exists) yield break;

//        long remoteAt = 0;
//        try { remoteAt = snap.GetValue<long>("updatedAt"); } catch { remoteAt = 0; }

//        long localAt = LocalPointsStore.LocalUpdatedAt();
//        if (remoteAt > localAt)
//        {
//            Dictionary<string, object> tp = null;
//            try { tp = snap.GetValue<Dictionary<string, object>>("topicPoints"); } catch { }
//            if (tp != null)
//            {
//                LocalPointsStore.ReplaceFromFirestore(tp, remoteAt);
//                LocalPointsStore.Save();
//                Debug.Log($"[PointsSync] Pulled (remoteAt {remoteAt} > localAt {localAt})");
//            }
//        }
//        else
//        {
//            Debug.Log($"[PointsSync] Keep local (localAt {localAt} >= remoteAt {remoteAt})");
//        }
//    }

//    // ---- СИНК ПРИ ВЫХОДЕ: отправить всё одним апдейтом ----
//    public static IEnumerator SyncOnExit()
//    {
//        LocalPointsStore.Load();
//        var uid = Auth.CurrentUser?.UserId;
//        if (string.IsNullOrEmpty(uid)) yield break;

//        LocalPointsStore.SetUpdatedNow();
//        var update = LocalPointsStore.ToFirestoreUpdate();
//        LocalPointsStore.Save();

//        var docRef = DB.Collection("users").Document(uid);
//        var task = docRef.SetAsync(update, SetOptions.MergeAll);
//        yield return new WaitUntil(() => task.IsCompleted);

//        if (task.Exception != null) Debug.LogWarning("[PointsSync] exit failed: " + task.Exception.Message);
//        else Debug.Log("[PointsSync] Pushed to Firestore.");
//    }
//}

// PointsService.cs — синхронизация очков с Firestore (персонифицировано по uid)
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

public static class PointsService
{
    private static string _uid;
    private static FirebaseFirestore _db;

    public static void SetCurrentUid(string uid)
    {
        _uid = uid;
        if (string.IsNullOrEmpty(_uid))
        {
            // переключились на гостя: используем локальные очки под пустым uid
            LocalPointsStore.SetCurrentUid(null);
        }
        else
        {
            LocalPointsStore.SetCurrentUid(_uid);
        }
    }

    private static FirebaseFirestore DB => _db ??= FirebaseFirestore.DefaultInstance;

    public static int AddPoints(string subjectId, string topicId, int delta)
    {
        return LocalPointsStore.Add(subjectId, topicId, delta);
    }

    public static int GetPoints(string subjectId, string topicId)
    {
        return LocalPointsStore.Get(subjectId, topicId);
    }

    // === Sync ===
    public static IEnumerator SyncOnEnter()
    {
        if (string.IsNullOrEmpty(_uid)) yield break; // без пользователя — только локально

        var docRef = DB.Collection("users").Document(_uid);
        var task = docRef.GetSnapshotAsync();
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted || task.Result == null) yield break;
        var snap = task.Result;
        if (!snap.Exists) yield break;

        long remoteUpdated = 0;
        Dictionary<string, object> remoteMap = null;
        try
        {
            if (snap.TryGetValue("practiceUpdatedAt", out long s)) remoteUpdated = s;
            if (snap.TryGetValue("practicePoints", out Dictionary<string, object> m)) remoteMap = m;
        }
        catch (Exception e) { Debug.LogWarning("[PointsService] parse remote failed: " + e.Message); }

        long localUpdated = LocalPointsStore.LocalUpdatedAt();
        if (remoteUpdated > localUpdated && remoteMap != null)
        {
            // сервер свежее ? заменяем локально
            LocalPointsStore.ReplaceFromFirestore(remoteMap, remoteUpdated);
        }
        else if (localUpdated > remoteUpdated)
        {
            // локально свежее ? отправим
            yield return PushAll();
        }
    }

    public static IEnumerator SyncOnExit()
    {
        if (string.IsNullOrEmpty(_uid)) yield break;
        yield return PushAll();
    }

    private static IEnumerator PushAll()
    {
        if (string.IsNullOrEmpty(_uid)) yield break;
        var payload = LocalPointsStore.ToFirestoreUpdate();
        var docRef = DB.Collection("users").Document(_uid);
        var task = docRef.SetAsync(payload, SetOptions.MergeAll);
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) Debug.LogWarning("[PointsService] push failed");
    }
}
