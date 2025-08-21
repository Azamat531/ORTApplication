
using System;
using System.Collections.Generic;

[Serializable] public class SubjectData { public string id; public string name; }
[Serializable] public class TopicData { public string id; public string name; }

// Вопрос: одна картинка + правильный ответ буквой ("А","Б","В","Г","Д")
[Serializable]
public class QuestionMin
{
    public string imageUrl;
    public string correctAnswer;
}

// Подтема: видео + список минимальных вопросов
[Serializable]
public class SubtopicData
{
    public int id;
    public string title;
    public string videoURL;
    public List<QuestionMin> questions;
}

// Обёртки (если вдруг какой-то JSON будет завернут объектом)
[Serializable] public class SubjectsArrayWrapper { public List<SubjectData> subjects; }
[Serializable] public class TopicsArrayWrapper { public List<TopicData> topics; }
[Serializable] public class SubtopicsWrapper { public List<SubtopicData> subtopics; }
