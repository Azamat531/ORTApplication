//using System;
//using System.Collections.Generic;
//using UnityEngine;

//[Serializable] public class TopicPoint { public string topicId; public int value; }
//[Serializable] public class SubjectPoints { public string subjectId; public List<TopicPoint> topics = new(); }
//[Serializable] public class PointsData { public List<SubjectPoints> subjects = new(); public long updatedAt; }

//public static class LocalPointsStore
//{
//    const string Key = "practice_points_v1";
//    static PointsData _data;

//    public static void Load()
//    {
//        var json = PlayerPrefs.GetString(Key, "");
//        _data = string.IsNullOrEmpty(json)
//            ? new PointsData { updatedAt = 0 }
//            : (JsonUtility.FromJson<PointsData>(json) ?? new PointsData { updatedAt = 0 });
//    }

//    public static void Save()
//    {
//        var json = JsonUtility.ToJson(_data);
//        PlayerPrefs.SetString(Key, json);
//        PlayerPrefs.Save();
//    }

//    static SubjectPoints GetOrCreateSubject(string subjectId)
//    {
//        subjectId = (subjectId ?? "").Trim();
//        var s = _data.subjects.Find(x => x.subjectId == subjectId);
//        if (s == null) { s = new SubjectPoints { subjectId = subjectId }; _data.subjects.Add(s); }
//        return s;
//    }

//    static TopicPoint GetOrCreateTopic(SubjectPoints s, string topicId)
//    {
//        topicId = (topicId ?? "").Trim();
//        var t = s.topics.Find(x => x.topicId == topicId);
//        if (t == null) { t = new TopicPoint { topicId = topicId, value = 0 }; s.topics.Add(t); }
//        return t;
//    }

//    public static int Get(string subjectId, string topicId)
//    {
//        if (_data == null) Load();
//        var s = _data.subjects.Find(x => x.subjectId == (subjectId ?? "").Trim());
//        var t = s?.topics.Find(x => x.topicId == (topicId ?? "").Trim());
//        return t?.value ?? 0;
//    }

//    public static int Set(string subjectId, string topicId, int value)
//    {
//        if (_data == null) Load();
//        var s = GetOrCreateSubject(subjectId);
//        var t = GetOrCreateTopic(s, topicId);
//        t.value = Mathf.Clamp(value, 0, 100);
//        _data.updatedAt = Now();
//        return t.value;
//    }

//    public static int Add(string subjectId, string topicId, int delta)
//    {
//        var cur = Get(subjectId, topicId);
//        return Set(subjectId, topicId, cur + delta);
//    }

//    public static long LocalUpdatedAt() { if (_data == null) Load(); return _data.updatedAt; }
//    public static void SetUpdatedNow() { if (_data == null) Load(); _data.updatedAt = Now(); }

//    public static Dictionary<string, object> ToFirestoreUpdate()
//    {
//        var dict = new Dictionary<string, object>();
//        if (_data == null) Load();
//        foreach (var s in _data.subjects)
//            foreach (var t in s.topics)
//                dict[$"topicPoints.{s.subjectId}.{t.topicId}"] = Mathf.Clamp(t.value, 0, 100);
//        dict["updatedAt"] = _data.updatedAt;
//        return dict;
//    }

//    public static void ReplaceFromFirestore(Dictionary<string, object> topicPoints, long updatedAt)
//    {
//        var map = new Dictionary<string, SubjectPoints>();

//        foreach (var kv in topicPoints)
//        {
//            var sid = kv.Key;
//            if (!map.TryGetValue(sid, out var sp)) { sp = new SubjectPoints { subjectId = sid }; map[sid] = sp; }

//            if (kv.Value is Dictionary<string, object> topics)
//            {
//                foreach (var kv2 in topics)
//                {
//                    var tid = kv2.Key;
//                    int val = ConvertToIntSafe(kv2.Value);
//                    var tp = sp.topics.Find(x => x.topicId == tid);
//                    if (tp == null) { tp = new TopicPoint { topicId = tid, value = val }; sp.topics.Add(tp); }
//                    else tp.value = val;
//                }
//            }
//        }

//        _data = new PointsData { subjects = new List<SubjectPoints>(map.Values), updatedAt = updatedAt };
//    }

//    static int ConvertToIntSafe(object o)
//    {
//        try
//        {
//            if (o is int i) return i;
//            if (o is long l) return (int)l;
//            if (o is double d) return Mathf.RoundToInt((float)d);
//            if (int.TryParse(o?.ToString() ?? "0", out var r)) return r;
//        }
//        catch { }
//        return 0;
//    }

//    static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//}

// LocalPointsStore.cs — персонифицированное хранилище очков (по uid)
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TopicPoint { public string topicId; public int value; }
[Serializable]
public class SubjectPoints { public string subjectId; public List<TopicPoint> topics = new(); }
[Serializable]
public class PointsData { public List<SubjectPoints> subjects = new(); public long updatedAt; }

public static class LocalPointsStore
{
    private const string KeyPrefix = "practice_points_v1"; // итоговый ключ: practice_points_v1_{uid}

    private static string _uid;
    private static PointsData _data;

    private static string Key => string.IsNullOrEmpty(_uid) ? KeyPrefix : ($"{KeyPrefix}_{_uid}");

    public static void SetCurrentUid(string uid)
    {
        _uid = uid;
        _data = null; // заставим перечитать для нового пользователя
        Load();
    }

    public static void Load()
    {
        if (_data != null) return;
        var json = PlayerPrefs.GetString(Key, null);
        if (string.IsNullOrEmpty(json)) { _data = new PointsData { subjects = new List<SubjectPoints>(), updatedAt = NowMs() }; return; }
        try { _data = JsonUtility.FromJson<PointsData>(json) ?? new PointsData(); }
        catch { _data = new PointsData(); }
        if (_data.subjects == null) _data.subjects = new List<SubjectPoints>();
    }

    public static void Save()
    {
        if (_data == null) return;
        _data.updatedAt = NowMs();
        var json = JsonUtility.ToJson(_data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static long LocalUpdatedAt()
    {
        Load();
        return _data?.updatedAt ?? 0L;
    }

    public static int Get(string subjectId, string topicId)
    {
        Load();
        var sp = _data.subjects.Find(s => s.subjectId == subjectId);
        if (sp == null) return 0;
        var tp = sp.topics.Find(t => t.topicId == topicId);
        return tp?.value ?? 0;
    }

    public static int Set(string subjectId, string topicId, int value)
    {
        Load();
        var sp = _data.subjects.Find(s => s.subjectId == subjectId);
        if (sp == null) { sp = new SubjectPoints { subjectId = subjectId }; _data.subjects.Add(sp); }
        var tp = sp.topics.Find(t => t.topicId == topicId);
        if (tp == null) { tp = new TopicPoint { topicId = topicId, value = 0 }; sp.topics.Add(tp); }
        tp.value = Mathf.Max(0, value);
        Save();
        return tp.value;
    }

    public static int Add(string subjectId, string topicId, int delta)
    {
        int v = Get(subjectId, topicId) + delta;
        return Set(subjectId, topicId, v);
    }

    // === Firestore bridge helpers ===
    public static Dictionary<string, object> ToFirestoreUpdate()
    {
        Load();
        var map = new Dictionary<string, object>();
        foreach (var s in _data.subjects)
        {
            if (s == null || string.IsNullOrEmpty(s.subjectId)) continue;
            foreach (var t in s.topics)
            {
                if (t == null || string.IsNullOrEmpty(t.topicId)) continue;
                string key = s.subjectId + ":" + t.topicId; // flat key
                map[key] = t.value;
            }
        }
        return new Dictionary<string, object>
        {
            { "practicePoints", map },
            { "practiceUpdatedAt", _data.updatedAt }
        };
    }

    public static void ReplaceFromFirestore(Dictionary<string, object> flat, long updatedAt)
    {
        _data = new PointsData { subjects = new List<SubjectPoints>(), updatedAt = updatedAt };
        if (flat != null)
        {
            foreach (var kv in flat)
            {
                // key = subjectId:topicId
                var parts = kv.Key.Split(':');
                if (parts.Length != 2) continue;
                string sid = parts[0]; string tid = parts[1];
                int val = 0; try { val = Convert.ToInt32(kv.Value); } catch { }

                var sp = _data.subjects.Find(s => s.subjectId == sid);
                if (sp == null) { sp = new SubjectPoints { subjectId = sid }; _data.subjects.Add(sp); }
                var tp = sp.topics.Find(t => t.topicId == tid);
                if (tp == null) { tp = new TopicPoint { topicId = tid, value = 0 }; sp.topics.Add(tp); }
                tp.value = val;
            }
        }
        Save();
    }

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
