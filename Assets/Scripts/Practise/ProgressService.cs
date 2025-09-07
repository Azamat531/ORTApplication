using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Хранение и чтение прогресса пользователя по теме (как в Alcumus):
/// users/{uid}/practiceProgress/{subjectId}_{topicId}
/// Поля: attempts, correct, wrong, streak, bestStreak, rating, lastQ, lastAt
/// </summary>
public static class ProgressService
{
    static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;
    static FirebaseFirestore DB => FirebaseFirestore.DefaultInstance;

    public static async Task RecordAttempt(string subjectId, string topicId, string topicName, int q, bool ok)
    {
        var uid = Auth.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) { Debug.LogWarning("[Progress] no user (not signed in)"); return; }

        var docId = $"{subjectId}_{topicId}";
        var docRef = DB.Collection("users").Document(uid)
                       .Collection("practiceProgress").Document(docId);

        Debug.Log($"[Progress] WRITE start uid={uid} doc={docId} q={q} ok={ok}");

        try
        {
            await DB.RunTransactionAsync(async tr =>
            {
                var snap = await tr.GetSnapshotAsync(docRef);
                int attempts = 0, correct = 0, wrong = 0, streak = 0, bestStreak = 0, rating = 1200;

                if (snap.Exists)
                {
                    attempts = snap.TryGetValue("attempts", out int v1) ? v1 : 0;
                    correct = snap.TryGetValue("correct", out int v2) ? v2 : 0;
                    wrong = snap.TryGetValue("wrong", out int v3) ? v3 : 0;
                    streak = snap.TryGetValue("streak", out int v4) ? v4 : 0;
                    bestStreak = snap.TryGetValue("bestStreak", out int v5) ? v5 : 0;
                    rating = snap.TryGetValue("rating", out int v6) ? v6 : 1200;
                }

                attempts += 1;
                if (ok) { correct += 1; streak += 1; rating += 12; }
                else { wrong += 1; streak = 0; rating -= 8; }
                if (streak > bestStreak) bestStreak = streak;
                rating = Mathf.Clamp(rating, 800, 2000);

                var data = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "subjectId", subjectId },
                    { "topicId", topicId },
                    { "topicName", topicName },
                    { "attempts", attempts },
                    { "correct", correct },
                    { "wrong", wrong },
                    { "streak", streak },
                    { "bestStreak", bestStreak },
                    { "rating", rating },
                    { "lastQ", q },
                    { "lastAt", Timestamp.GetCurrentTimestamp() }
                };

                if (snap.Exists) tr.Update(docRef, data); else tr.Set(docRef, data);

                Debug.Log($"[Progress] WRITE ok attempts={attempts} correct={correct} rating={rating}");
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Progress] WRITE failed: " + e.Message);
        }
    }

    /// <summary>
    /// Возвращает процент 0..1 как correct/attempts. Если нет попыток — 0.
    /// </summary>
    public static async Task<float> GetTopicPercent01(string subjectId, string topicId)
    {
        var uid = Auth.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) { Debug.LogWarning("[Progress] percent: no user"); return 0f; }

        var docId = $"{subjectId}_{topicId}";
        try
        {
            var snap = await DB.Collection("users").Document(uid)
                               .Collection("practiceProgress")
                               .Document(docId)
                               .GetSnapshotAsync();
            if (!snap.Exists) { Debug.Log($"[Progress] READ percent doc not found: {docId}"); return 0f; }
            int attempts = snap.TryGetValue("attempts", out int a) ? a : 0;
            int correct = snap.TryGetValue("correct", out int c) ? c : 0;
            float p = attempts > 0 ? Mathf.Clamp01((float)correct / attempts) : 0f;
            Debug.Log($"[Progress] READ percent {docId}: attempts={attempts} correct={correct} p={p}");
            return p;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Progress] READ failed: " + e.Message);
            return 0f;
        }
    }
}
