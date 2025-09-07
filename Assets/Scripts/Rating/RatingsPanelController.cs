//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Firebase.Firestore;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class RatingsPanelController : MonoBehaviour
//{
//    [Header("UI")]
//    public Transform content;           // ScrollView/Viewport/Content (с VLG в инспекторе)
//    public GameObject itemPrefab;       // Префаб с RatingItemView
//    public TextMeshProUGUI statusText;

//    [Header("Options")]
//    public int maxItems = 100;
//    public bool debugLogs = false;

//    private FirebaseFirestore _db;

//    void OnEnable()
//    {
//        _db = FirebaseFirestore.DefaultInstance;
//        _ = Refresh();
//    }

//    public async Task Refresh()
//    {
//        SetStatus("Ж?кт?л??д?…"); // Загрузка
//        ClearContent();

//        try
//        {
//            // Сначала пробуем агрегат practiceTotal
//            var snap = await _db.Collection("users")
//                                .OrderByDescending("practiceTotal")
//                                .Limit(maxItems)
//                                .GetSnapshotAsync();

//            var rows = new List<RatingRow>();
//            bool anyAggregate = snap.Documents.Any(d =>
//                   d.ContainsField("practiceTotal")
//                || d.ContainsField("practice")
//                || d.ContainsField("stats")
//                || d.ContainsField("totalPoints")
//                || d.ContainsField("pointsTotal")
//                || d.ContainsField("scoreTotal"));

//            if (anyAggregate)
//            {
//                foreach (var d in snap.Documents)
//                {
//                    rows.Add(new RatingRow
//                    {
//                        Uid = d.Id,
//                        Score = GetPracticeTotalFromProfile(d),
//                        Name = GetName(d),
//                        Region = GetStr(d, "region", "oblast", "area", "province"),
//                        District = GetStr(d, "district", "raion"),
//                        School = GetStr(d, "school", "schoolName"),
//                        _ref = d.Reference
//                    });
//                }
//                if (rows.All(r => r.Score == 0))
//                    rows = await FallbackSumBySubcollections(rows, debugLogs);
//            }
//            else
//            {
//                var usersSnap = await _db.Collection("users").Limit(maxItems).GetSnapshotAsync();
//                foreach (var d in usersSnap.Documents)
//                {
//                    rows.Add(new RatingRow
//                    {
//                        Uid = d.Id,
//                        Score = GetPracticeTotalFromProfile(d),
//                        Name = GetName(d),
//                        Region = GetStr(d, "region", "oblast", "area", "province"),
//                        District = GetStr(d, "district", "raion"),
//                        School = GetStr(d, "school", "schoolName"),
//                        _ref = d.Reference
//                    });
//                }
//                rows = await FallbackSumBySubcollections(rows, debugLogs);
//            }

//            // Сортировка и рендер
//            rows = rows.OrderByDescending(r => r.Score).Take(maxItems).ToList();

//            int rank = 1;
//            foreach (var row in rows)
//            {
//                var go = Instantiate(itemPrefab, content);
//                var view = go.GetComponent<RatingItemView>();
//                if (view)
//                    view.Setup(rank.ToString("000"), row.Name, row.Region, row.District, row.School, row.Score);
//                rank++;
//            }

//            // лишь форсим пересборку, не меняя настройки
//            Canvas.ForceUpdateCanvases();
//            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

//            SetStatus(rows.Count == 0 ? "Бош." : "");
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            SetStatus("Ката / Ошибка");
//        }
//    }

//    // ---------- helpers ----------
//    private void ClearContent()
//    {
//        for (int i = content.childCount - 1; i >= 0; i--)
//            Destroy(content.GetChild(i).gameObject);
//    }
//    private void SetStatus(string s) { if (statusText) statusText.text = s ?? ""; }

//    private static string GetName(DocumentSnapshot d)
//    {
//        if (TryGetString(d, out var rn, "realName")) return rn;
//        if (TryGetString(d, out var pid, "publicId")) return pid;
//        return "—";
//    }

//    private static int GetPracticeTotalFromProfile(DocumentSnapshot d)
//    {
//        if (TryGetNumber(d, out var n, "practiceTotal")) return n;
//        if (TryGetNumber(d, out n, "totalPoints")) return n;
//        if (TryGetNumber(d, out n, "pointsTotal")) return n;
//        if (TryGetNumber(d, out n, "scoreTotal")) return n;
//        if (TryGetNestedNumber(d, out n, "practice", "total")) return n;
//        if (TryGetNestedNumber(d, out n, "practice", "points")) return n;
//        if (TryGetNestedNumber(d, out n, "stats", "practice", "total")) return n;
//        return 0;
//    }

//    private static async Task<List<RatingRow>> FallbackSumBySubcollections(List<RatingRow> rows, bool log)
//    {
//        var tasks = rows.Select(async r =>
//        {
//            if (r.Score > 0) return r;
//            if (r._ref == null)
//                r._ref = FirebaseFirestore.DefaultInstance.Collection("users").Document(r.Uid);
//            r.Score = await SumPointsFromAny(r._ref, log, r.Uid);
//            return r;
//        });
//        return (await Task.WhenAll(tasks)).ToList();
//    }

//    private static async Task<int> SumPointsFromAny(DocumentReference userRef, bool log, string uid)
//    {
//        if (userRef == null) return 0;
//        string[] colNames = { "practice", "practices", "practiceResults", "practice_sessions" };
//        string[] fieldNames = { "points", "score", "value", "total" };

//        foreach (var cn in colNames)
//        {
//            try
//            {
//                var ss = await userRef.Collection(cn).GetSnapshotAsync();
//                if (ss == null || ss.Count == 0) continue;

//                long sum = 0;
//                foreach (var doc in ss.Documents)
//                    foreach (var fn in fieldNames)
//                        if (doc.ContainsField(fn) && TryToInt(doc.GetValue<object>(fn), out int v))
//                        { sum += v; break; }

//                if (sum > 0)
//                {
//                    if (log) Debug.Log($"[Ratings] {uid}: sum from '{cn}' = {sum}");
//                    return (int)sum;
//                }
//            }
//            catch (Exception e)
//            {
//                if (log && e.Message.Contains("insufficient permissions"))
//                    Debug.LogWarning($"[Ratings] no read permission for users/{uid}/{cn}/*");
//            }
//        }
//        return 0;
//    }

//    private static bool TryGetString(DocumentSnapshot d, out string result, params string[] keys)
//    {
//        result = "";
//        foreach (var k in keys)
//            if (d.ContainsField(k))
//                try { var v = d.GetValue<object>(k); if (v is string s && !string.IsNullOrWhiteSpace(s)) { result = s; return true; } } catch { }
//        return false;
//    }
//    private static bool TryGetNumber(DocumentSnapshot d, out int result, params string[] keys)
//    {
//        result = 0;
//        foreach (var k in keys)
//            if (d.ContainsField(k))
//                try { var v = d.GetValue<object>(k); if (TryToInt(v, out result)) return true; } catch { }
//        return false;
//    }
//    private static bool TryGetNestedNumber(DocumentSnapshot d, out int result, params string[] path)
//    {
//        result = 0;
//        if (!d.ContainsField(path[0])) return false;
//        try
//        {
//            object cur = d.GetValue<object>(path[0]);
//            for (int i = 1; i < path.Length; i++)
//            {
//                if (cur is Dictionary<string, object> dict && dict.TryGetValue(path[i], out var next)) cur = next;
//                else return false;
//            }
//            return TryToInt(cur, out result);
//        }
//        catch { return false; }
//    }
//    private static bool TryToInt(object obj, out int val)
//    {
//        val = 0;
//        switch (obj)
//        {
//            case null: return false;
//            case int i: val = i; return true;
//            case long l: val = (int)l; return true;
//            case double d: val = (int)Math.Round(d); return true;
//            case float f: val = (int)Math.Round(f); return true;
//            case string s when int.TryParse(s, out var p): val = p; return true;
//            default: return false;
//        }
//    }
//    private static string GetStr(DocumentSnapshot snap, params string[] keys)
//    {
//        foreach (var k in keys)
//            if (snap.ContainsField(k))
//                try { var v = snap.GetValue<string>(k); if (!string.IsNullOrWhiteSpace(v)) return v; } catch { }
//        return "";
//    }

//    private class RatingRow
//    {
//        public string Uid, Name, Region, District, School;
//        public int Score;
//        public DocumentReference _ref;
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingsPanelController : MonoBehaviour
{
    [Header("UI")]
    public Transform content;           // ScrollView/Viewport/Content (с твоим VLG)
    public GameObject itemPrefab;       // Префаб с RatingItemView
    public TextMeshProUGUI statusText;

    [Header("Options")]
    public int maxUsersToFetch = 300;   // сколько профилей забрать для расчёта
    public bool debugLogs = false;

    private FirebaseFirestore _db;

    void OnEnable()
    {
        _db = FirebaseFirestore.DefaultInstance;
        _ = Refresh();
    }

    public async Task Refresh()
    {
        SetStatus("Ж?кт?л??д?…");
        ClearContent();

        try
        {
            // Берём пачку пользователей (сначала у кого свежее апдейт очков)
            var usersSnap = await _db.Collection("users")
                                     .OrderByDescending("practiceUpdatedAt")
                                     .Limit(maxUsersToFetch)
                                     .GetSnapshotAsync();

            var rows = new List<Row>();
            foreach (var d in usersSnap.Documents)
            {
                int total = SumPracticePointsFromProfile(d); // << ключевая строчка

                rows.Add(new Row
                {
                    Uid = d.Id,
                    Score = total,
                    Name = GetName(d),
                    Region = GetStr(d, "region", "oblast", "area", "province"),
                    District = GetStr(d, "district", "raion"),
                    School = GetStr(d, "school", "schoolName")
                });

                if (debugLogs) Debug.Log($"[Ratings] {d.Id} total={total}");
            }

            // Сортировка и отрисовка
            rows = rows.OrderByDescending(r => r.Score).ThenBy(r => r.Name).ToList();

            int rank = 1;
            foreach (var r in rows)
            {
                var go = Instantiate(itemPrefab, content);
                var view = go.GetComponent<RatingItemView>();
                if (view) view.Setup(rank.ToString("000"), r.Name, r.Region, r.District, r.School, r.Score);
                rank++;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

            SetStatus(rows.Count == 0 ? "Бош." : "");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus("Ката / Ошибка");
        }
    }

    // ---------- суммирование practicePoints ----------
    private static int SumPracticePointsFromProfile(DocumentSnapshot d)
    {
        try
        {
            // ожидаем map<string, object> где ключ = "subjectId:topicId"
            if (d.TryGetValue("practicePoints", out Dictionary<string, object> map) && map != null)
            {
                long sum = 0;
                foreach (var kv in map)
                    if (TryToInt(kv.Value, out int v)) sum += v;
                return (int)sum;
            }
        }
        catch { /* поле отсутствует/тип другой */ }
        return 0;
    }

    // ---------- утилиты ----------
    private void ClearContent()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }
    private void SetStatus(string s) { if (statusText) statusText.text = s ?? ""; }

    private static string GetName(DocumentSnapshot d)
    {
        if (TryGetString(d, out var rn, "realName")) return rn;
        if (TryGetString(d, out var pid, "publicId")) return pid;
        return "—";
    }

    private static string GetStr(DocumentSnapshot snap, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!snap.ContainsField(k)) continue;
            try { var v = snap.GetValue<string>(k); if (!string.IsNullOrWhiteSpace(v)) return v; } catch { }
        }
        return "";
    }

    private static bool TryGetString(DocumentSnapshot d, out string result, params string[] keys)
    {
        result = "";
        foreach (var k in keys)
        {
            if (!d.ContainsField(k)) continue;
            try
            {
                var v = d.GetValue<object>(k);
                if (v is string s && !string.IsNullOrWhiteSpace(s)) { result = s; return true; }
            }
            catch { }
        }
        return false;
    }

    private static bool TryToInt(object obj, out int val)
    {
        val = 0;
        switch (obj)
        {
            case null: return false;
            case int i: val = i; return true;
            case long l: val = (int)l; return true;
            case double d: val = (int)Math.Round(d); return true;
            case float f: val = (int)Math.Round(f); return true;
            case string s when int.TryParse(s, out var p): val = p; return true;
            default: return false;
        }
    }

    private class Row
    {
        public string Uid, Name, Region, District, School;
        public int Score;
    }
}
