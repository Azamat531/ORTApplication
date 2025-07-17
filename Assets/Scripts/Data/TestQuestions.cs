// Assets/Scripts/Data/TestQuestions.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TestQuestions",
    menuName = "Education/Test Questions Hierarchical")]
public class TestQuestions : ScriptableObject
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
    public class Subtopic
    {
        public string name;
        public List<Question> questions;
    }

    [System.Serializable]
    public class Topic
    {
        public string name;
        public List<Subtopic> subtopics;
    }

    [System.Serializable]
    public class Subject
    {
        public string name;
        public List<Topic> topics;
    }

    public List<Subject> subjects;
}
