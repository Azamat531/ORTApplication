// Assets/Scripts/Editor/TestQuestionsEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(TestQuestions))]
public class TestQuestionsEditor : Editor
{
    private ReorderableList subjectsList;
    private Dictionary<int, bool> subjectFoldouts = new();
    private Dictionary<string, ReorderableList> topicLists = new();
    private Dictionary<string, bool> topicFoldouts = new();
    private Dictionary<string, ReorderableList> subtopicLists = new();
    private Dictionary<string, bool> subtopicListFoldouts = new();    // для скрытия/показа entire Subtopics list
    private Dictionary<string, ReorderableList> questionLists = new();
    private Dictionary<string, bool> questionFoldouts = new();        // для скрытия/показа Questions под каждым Subtopic

    void OnEnable()
    {
        var subjectsProp = serializedObject.FindProperty("subjects");
        subjectsList = new ReorderableList(serializedObject, subjectsProp,
            true, true, true, true);

        subjectsList.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Subjects");
        subjectsList.elementHeightCallback = i =>
        {
            float h = EditorGUIUtility.singleLineHeight + 4;
            if (subjectFoldouts.TryGetValue(i, out var subExp) && subExp)
            {
                string tKey = $"topics_{i}";
                if (topicLists.TryGetValue(tKey, out var tList))
                    h += tList.GetHeight() + 8;
            }
            return h;
        };
        subjectsList.drawElementCallback = (r, i, a, f) =>
        {
            var subjEl = subjectsProp.GetArrayElementAtIndex(i);
            if (!subjectFoldouts.ContainsKey(i)) subjectFoldouts[i] = false;
            subjectFoldouts[i] = EditorGUI.Foldout(
                new Rect(r.x, r.y, 12, EditorGUIUtility.singleLineHeight),
                subjectFoldouts[i], GUIContent.none);

            EditorGUI.PropertyField(
                new Rect(r.x + 16, r.y, r.width - 16, EditorGUIUtility.singleLineHeight),
                subjEl.FindPropertyRelative("name"), GUIContent.none);

            if (!subjectFoldouts[i]) return;

            var topicsProp = subjEl.FindPropertyRelative("topics");
            string tKey = $"topics_{i}";
            if (!topicLists.TryGetValue(tKey, out var tList))
                topicLists[tKey] = tList = CreateTopicList(topicsProp, i);

            tList.DoList(new Rect(
                r.x + 10,
                r.y + EditorGUIUtility.singleLineHeight + 4,
                r.width - 10,
                tList.GetHeight()));
        };
        subjectsList.onAddCallback = l =>
        {
            subjectsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newEl = subjectsProp.GetArrayElementAtIndex(subjectsProp.arraySize - 1);
            newEl.FindPropertyRelative("name").stringValue = string.Empty;
            newEl.FindPropertyRelative("topics").arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        };
    }

    private ReorderableList CreateTopicList(SerializedProperty topicsProp, int subjIdx)
    {
        var list = new ReorderableList(serializedObject, topicsProp,
            true, true, true, true);
        list.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Topics");
        list.elementHeightCallback = i =>
        {
            float h = EditorGUIUtility.singleLineHeight + 4;
            string stKey = $"subtopicsList_{subjIdx}_{i}";
            if (subtopicListFoldouts.TryGetValue(stKey, out var exp) && exp)
            {
                if (subtopicLists.TryGetValue(stKey, out var stList))
                    h += stList.GetHeight() + 8;
            }
            return h;
        };
        list.drawElementCallback = (r, i, a, f) =>
        {
            var topicEl = topicsProp.GetArrayElementAtIndex(i);

            // Foldout entire subtopics list
            string stKey = $"subtopicsList_{subjIdx}_{i}";
            if (!subtopicListFoldouts.ContainsKey(stKey)) subtopicListFoldouts[stKey] = false;
            subtopicListFoldouts[stKey] = EditorGUI.Foldout(
                new Rect(r.x, r.y, 12, EditorGUIUtility.singleLineHeight),
                subtopicListFoldouts[stKey], GUIContent.none);

            // Topic name
            EditorGUI.PropertyField(
                new Rect(r.x + 16, r.y, r.width - 16, EditorGUIUtility.singleLineHeight),
                topicEl.FindPropertyRelative("name"), GUIContent.none);

            if (!subtopicListFoldouts[stKey]) return;

            var subsProp = topicEl.FindPropertyRelative("subtopics");
            if (!subtopicLists.TryGetValue(stKey, out var stList))
                subtopicLists[stKey] = stList = CreateSubtopicList(subsProp, subjIdx, i);

            stList.DoList(new Rect(
                r.x + 16,
                r.y + EditorGUIUtility.singleLineHeight + 4,
                r.width - 16,
                stList.GetHeight()));
        };
        list.onAddCallback = l =>
        {
            topicsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newEl = topicsProp.GetArrayElementAtIndex(topicsProp.arraySize - 1);
            newEl.FindPropertyRelative("name").stringValue = string.Empty;
            newEl.FindPropertyRelative("subtopics").arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        };
        return list;
    }

    private ReorderableList CreateSubtopicList(SerializedProperty subsProp, int subjIdx, int topicIdx)
    {
        var list = new ReorderableList(serializedObject, subsProp,
            true, true, true, true);
        list.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Subtopics");
        list.elementHeightCallback = i =>
        {
            float h = EditorGUIUtility.singleLineHeight + 4;
            string qKey = $"questions_{subjIdx}_{topicIdx}_{i}";
            if (questionFoldouts.TryGetValue(qKey, out var exp) && exp)
            {
                if (questionLists.TryGetValue(qKey, out var qList))
                    h += qList.GetHeight() + 8;
            }
            return h;
        };
        list.drawElementCallback = (r, i, a, f) =>
        {
            var subEl = subsProp.GetArrayElementAtIndex(i);
            string qKey = $"questions_{subjIdx}_{topicIdx}_{i}";

            // Foldout per individual subtopic ? questions
            if (!questionFoldouts.ContainsKey(qKey)) questionFoldouts[qKey] = false;
            questionFoldouts[qKey] = EditorGUI.Foldout(
                new Rect(r.x, r.y, 12, EditorGUIUtility.singleLineHeight),
                questionFoldouts[qKey], GUIContent.none);

            // Subtopic name
            EditorGUI.PropertyField(
                new Rect(r.x + 16, r.y, r.width - 16, EditorGUIUtility.singleLineHeight),
                subEl.FindPropertyRelative("name"), GUIContent.none);

            if (!questionFoldouts[qKey]) return;

            // Questions list
            var qProp = subEl.FindPropertyRelative("questions");
            if (!questionLists.TryGetValue(qKey, out var qList))
                questionLists[qKey] = qList = CreateQuestionList(qProp);

            qList.DoList(new Rect(
                r.x + 16,
                r.y + EditorGUIUtility.singleLineHeight + 4,
                r.width - 16,
                qList.GetHeight()));
        };
        list.onAddCallback = l =>
        {
            subsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newEl = subsProp.GetArrayElementAtIndex(subsProp.arraySize - 1);
            newEl.FindPropertyRelative("name").stringValue = string.Empty;
            newEl.FindPropertyRelative("questions").arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        };
        return list;
    }

    private ReorderableList CreateQuestionList(SerializedProperty qProp)
    {
        var list = new ReorderableList(serializedObject, qProp,
            true, true, true, true);
        list.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Questions");
        list.elementHeightCallback = i =>
        {
            var el = qProp.GetArrayElementAtIndex(i);
            float h = 4;
            h += EditorGUI.GetPropertyHeight(el.FindPropertyRelative("questionText"));
            h += EditorGUI.GetPropertyHeight(el.FindPropertyRelative("imageUrl"));
            h += EditorGUI.GetPropertyHeight(el.FindPropertyRelative("correctIndex"));
            h += EditorGUI.GetPropertyHeight(el.FindPropertyRelative("options"));
            return h + 10;
        };
        list.drawElementCallback = (r, i, a, f) =>
        {
            var el = qProp.GetArrayElementAtIndex(i);
            float y = r.y + 2;
            float lineH = EditorGUIUtility.singleLineHeight + 4;

            EditorGUI.PropertyField(
                new Rect(r.x, y, r.width, lineH),
                el.FindPropertyRelative("questionText"), new GUIContent("Question"));
            y += lineH;
            EditorGUI.PropertyField(
                new Rect(r.x, y, r.width, lineH),
                el.FindPropertyRelative("imageUrl"), new GUIContent("Image URL"));
            y += lineH;
            EditorGUI.PropertyField(
                new Rect(r.x, y, r.width, lineH),
                el.FindPropertyRelative("correctIndex"), new GUIContent("Correct Index"));
            y += lineH;
            EditorGUI.PropertyField(
                new Rect(r.x, y, r.width,
                          EditorGUI.GetPropertyHeight(el.FindPropertyRelative("options"))),
                el.FindPropertyRelative("options"), new GUIContent("Options"));
        };
        list.onAddCallback = l =>
        {
            qProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newEl = qProp.GetArrayElementAtIndex(qProp.arraySize - 1);
            newEl.FindPropertyRelative("questionText").stringValue = string.Empty;
            newEl.FindPropertyRelative("imageUrl").stringValue = string.Empty;
            newEl.FindPropertyRelative("correctIndex").intValue = 0;
            var opts = newEl.FindPropertyRelative("options");
            opts.ClearArray();
            for (int k = 0; k < 4; k++)
            {
                opts.InsertArrayElementAtIndex(k);
                opts.GetArrayElementAtIndex(k).stringValue = string.Empty;
            }
            serializedObject.ApplyModifiedProperties();
        };
        return list;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        subjectsList.DoLayoutList();
        if (serializedObject.ApplyModifiedProperties())
        {
            // очищаем только списки, но сохраняем все состояния foldout
            topicLists.Clear();
            subtopicLists.Clear();
            questionLists.Clear();
        }
    }
}
