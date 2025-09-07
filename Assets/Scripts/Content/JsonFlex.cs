using UnityEngine;
using System.Collections.Generic;

public static class JsonFlex
{
    // ”ниверсальный разбор массива без обЄртки: [ {...}, {...} ]
    public static List<T> FromArray<T>(string json)
    {
        try
        {
            string wrapped = "{ \"array\": " + json + "}";
            var w = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return w?.array != null ? new List<T>(w.array) : null;
        }
        catch
        {
            return null;
        }
    }

    // subjects.json Ч либо массив, либо {"subjects":[...]}
    public static List<SubjectData> ParseSubjects(string json)
    {
        // пробуем Ђголыйї массив
        var a = FromArray<SubjectData>(json);
        if (a != null) return a;

        // пробуем обЄртку {"subjects":[...]}
        try
        {
            var w = JsonUtility.FromJson<SubjectsArrayWrapper>(json);
            return w?.subjects ?? new List<SubjectData>();
        }
        catch { return new List<SubjectData>(); }
    }

    // topics.json Ч либо массив, либо {"topics":[...]}
    public static List<TopicData> ParseTopics(string json)
    {
        var a = FromArray<TopicData>(json);
        if (a != null) return a;

        try
        {
            var w = JsonUtility.FromJson<TopicsArrayWrapper>(json);
            return w?.topics ?? new List<TopicData>();
        }
        catch { return new List<TopicData>(); }
    }

    // subtopics.json Ч финальный формат:
    // { "subtopics": [ { "id":"1", "title":"...", "answers":["ј","Ѕ", ...] }, ... ] }
    // на вс€кий: поддержим {"items":[...]} и Ђголыйї массив
    public static List<SubtopicIndex> ParseSubtopics(string json)
    {
        try
        {
            var w = JsonUtility.FromJson<SubtopicsIndexWrapper>(json);
            if (w?.subtopics != null) return w.subtopics;
        }
        catch { }

        try
        {
            var w2 = JsonUtility.FromJson<ItemsIndexWrapper>(json);
            if (w2?.items != null) return w2.items;
        }
        catch { }

        var a = FromArray<SubtopicIndex>(json);
        return a ?? new List<SubtopicIndex>();
    }

    // ===== ¬спомогательные обЄртки дл€ JsonUtility =====
    [System.Serializable] private class Wrapper<T> { public T[] array; }

    [System.Serializable]
    private class SubjectsArrayWrapper
    {
        public List<SubjectData> subjects;
    }

    [System.Serializable]
    private class TopicsArrayWrapper
    {
        public List<TopicData> topics;
    }

    [System.Serializable]
    private class SubtopicsIndexWrapper
    {
        public List<SubtopicIndex> subtopics;
    }

    [System.Serializable]
    private class ItemsIndexWrapper
    {
        public List<SubtopicIndex> items;
    }
}
