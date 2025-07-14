using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CheckTestQuestions", menuName = "Education/Check Test Questions Hierarchical")]
public class CheckTestQuestions : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string imageUrl;
        public List<string> options;
        public int correctIndex;
    }

    [System.Serializable]
    public class Topic
    {
        public string name;
        public List<Question> questions;
    }

    [System.Serializable]
    public class Subject
    {
        public string name;
        public List<Topic> topics;
    }

    public List<Subject> subjects;
}
