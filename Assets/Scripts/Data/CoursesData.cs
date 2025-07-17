using UnityEngine;

[CreateAssetMenu(fileName = "CoursesData", menuName = "Education/Courses Data")]
public class CoursesData : ScriptableObject
{
    [System.Serializable]
    public class Subtopic
    {
        public string name;
        public string videoURL;
    }

    [System.Serializable]
    public class Topic
    {
        public string name;
        public Subtopic[] subtopics;
    }

    [System.Serializable]
    public class Subject
    {
        public string name;
        public Topic[] topics;
    }

    [Tooltip("Список предметов ? тем ? подтем")]
    public Subject[] subjects;
}
