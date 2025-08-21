using System;
using System.Collections.Generic;

[Serializable]
public class SubjectsJson
{
    public string updatedAt;
    public List<SubjectItem> subjects;
}

[Serializable]
public class SubjectItem
{
    public string id;
    public string title;
}
