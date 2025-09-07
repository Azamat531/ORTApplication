// ============================================
// File: Assets/Scripts/Models.cs
// Minimal DTOs to match final JSON (subjects/topics/subtopics)
// Final subtopics.json uses: { id: string, title: string, answers: ["�","�", ...] }
// ============================================
using System;
using System.Collections.Generic;

[Serializable]
public class SubjectData
{
    public string id;
    public string name;
}

[Serializable]
public class TopicData
{
    public string id;
    public string name;
}

[Serializable]
public class SubtopicIndex
{
    public string id;                 // ��������� id ("1", "2", ...)
    public string title;              // "������� 1"
    public List<string> answers;      // ["�","�","�"]
}
