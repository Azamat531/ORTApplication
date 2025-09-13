// Assets/Scripts/Services/CourseProgress.cs
using UnityEngine;

public static class CourseProgress
{
    private static string _currentUid = "";

    /// <summary>
    /// ������������� �������� ������������ (uid �� Firebase).
    /// </summary>
    public static void SetUser(string uid)
    {
        _currentUid = string.IsNullOrEmpty(uid) ? "" : uid;
    }

    // ================== ��������������� ������ ==================
    private static string Key(string raw)
    {
        return string.IsNullOrEmpty(_currentUid) ? raw : $"{_currentUid}_{raw}";
    }

    // ================== ������� ==================
    public static void MarkSubtopicDone(string subjectId, string topicId, string subId)
    {
        PlayerPrefs.SetInt(Key($"sub_{subjectId}_{topicId}_{subId}"), 1);
        PlayerPrefs.Save();
    }

    public static bool IsSubtopicDone(string subjectId, string topicId, string subId)
    {
        return PlayerPrefs.GetInt(Key($"sub_{subjectId}_{topicId}_{subId}"), 0) == 1;
    }

    // ================== ���� ==================
    public static void MarkTopicDone(string subjectId, string topicId)
    {
        PlayerPrefs.SetInt(Key($"topic_{subjectId}_{topicId}"), 1);
        PlayerPrefs.Save();
    }

    public static bool IsTopicDone(string subjectId, string topicId)
    {
        return PlayerPrefs.GetInt(Key($"topic_{subjectId}_{topicId}"), 0) == 1;
    }

    // ================== ������� ==================
    /// <summary>
    /// ������� ���� �������� �������� ������������.
    /// </summary>
    public static void ClearAll()
    {
        if (string.IsNullOrEmpty(_currentUid))
            return;

        // � PlayerPrefs ��� ������ "������� �� �� ��������",
        // ��� ��� ����� ���� ���������� �������, ���� ��� ������������ �������:
        PlayerPrefs.DeleteAll();
    }
}
