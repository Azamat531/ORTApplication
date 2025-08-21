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
        catch { return null; }
    }

    // subjects.json Ч либо массив, либо {"subjects":[...]}
    public static List<SubjectData> ParseSubjects(string json)
    {
        var a = FromArray<SubjectData>(json);
        if (a != null) return a;
        try { var w = JsonUtility.FromJson<SubjectsArrayWrapper>(json); return w?.subjects; }
        catch { return null; }
    }

    // topics_{subjectId}.json Ч либо массив, либо {"topics":[...]}
    public static List<TopicData> ParseTopics(string json)
    {
        var a = FromArray<TopicData>(json);
        if (a != null) return a;
        try { var w = JsonUtility.FromJson<TopicsArrayWrapper>(json); return w?.topics; }
        catch { return null; }
    }

    // content/{sid}/topic_{tid}.json Ч либо массив, либо {"subtopics":[...]}
    public static List<SubtopicData> ParseSubtopics(string json)
    {
        var a = FromArray<SubtopicData>(json);
        if (a != null) return a;
        try { var w = JsonUtility.FromJson<SubtopicsWrapper>(json); return w?.subtopics; }
        catch { return null; }
    }

    [System.Serializable] private class Wrapper<T> { public T[] array; }
}
