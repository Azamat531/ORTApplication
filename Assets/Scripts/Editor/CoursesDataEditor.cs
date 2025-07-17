
// Assets/Editor/CoursesDataEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(CoursesData))]
public class CoursesDataEditor : Editor
{
    private ReorderableList subjectsList;
    private Dictionary<int, bool> subjectFoldouts = new Dictionary<int, bool>();
    private Dictionary<string, ReorderableList> topicLists = new Dictionary<string, ReorderableList>();
    private Dictionary<string, bool> topicFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, ReorderableList> subtopicLists = new Dictionary<string, ReorderableList>();

    void OnEnable()
    {
        var subjectsProp = serializedObject.FindProperty("subjects");
        subjectsList = new ReorderableList(serializedObject, subjectsProp, true, true, true, true);

        subjectsList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Subjects");

        subjectsList.elementHeightCallback = index =>
        {
            // Base height for subject line
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            bool expandedSubject = subjectFoldouts.ContainsKey(index) && subjectFoldouts[index];
            if (!expandedSubject)
                return height;

            var element = subjectsProp.GetArrayElementAtIndex(index);
            var topicsProp = element.FindPropertyRelative("topics");
            string topicKey = $"topics_{index}";
            if (topicLists.TryGetValue(topicKey, out var tlist))
                height += tlist.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
            return height;
        };

        subjectsList.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = subjectsProp.GetArrayElementAtIndex(index);
            // Subject foldout
            if (!subjectFoldouts.ContainsKey(index)) subjectFoldouts[index] = true;
            Rect foldRect = new Rect(rect.x, rect.y, 12, EditorGUIUtility.singleLineHeight);
            subjectFoldouts[index] = EditorGUI.Foldout(foldRect, subjectFoldouts[index], GUIContent.none);

            // Subject name
            Rect nameRect = new Rect(rect.x + 16, rect.y, rect.width - 16, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(nameRect, element.FindPropertyRelative("name"), GUIContent.none);

            if (!subjectFoldouts[index]) return;

            // Topics list
            var topicsProp = element.FindPropertyRelative("topics");
            string topicKey2 = $"topics_{index}";
            if (!topicLists.TryGetValue(topicKey2, out var tlist))
            {
                tlist = new ReorderableList(serializedObject, topicsProp, true, true, true, true);
                tlist.drawHeaderCallback = r => EditorGUI.LabelField(r, "Topics");

                tlist.elementHeightCallback = i =>
                {
                    float h = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    string subKey = $"subtopics_{index}_{i}";
                    bool expandedTopic = topicFoldouts.ContainsKey(subKey) && topicFoldouts[subKey];
                    if (expandedTopic && subtopicLists.TryGetValue(subKey, out var sList))
                        h += sList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
                    return h;
                };

                tlist.drawElementCallback = (r2, i2, a2, f2) =>
                {
                    var topicEl = topicsProp.GetArrayElementAtIndex(i2);
                    string subKey = $"subtopics_{index}_{i2}";
                    // Topic foldout
                    if (!topicFoldouts.ContainsKey(subKey)) topicFoldouts[subKey] = true;
                    Rect tfold = new Rect(r2.x, r2.y, 12, EditorGUIUtility.singleLineHeight);
                    topicFoldouts[subKey] = EditorGUI.Foldout(tfold, topicFoldouts[subKey], GUIContent.none);

                    // Topic name
                    Rect topicNameRect = new Rect(r2.x + 16, r2.y, r2.width - 16, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(topicNameRect, topicEl.FindPropertyRelative("name"), GUIContent.none);

                    if (!topicFoldouts[subKey]) return;

                    // Subtopics list
                    var subsProp = topicEl.FindPropertyRelative("subtopics");
                    if (!subtopicLists.TryGetValue(subKey, out var sList))
                    {
                        sList = new ReorderableList(serializedObject, subsProp, true, true, true, true);
                        sList.drawHeaderCallback = rr => EditorGUI.LabelField(rr, "Subtopics");
                        sList.elementHeightCallback = j => (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing * 2;
                        sList.drawElementCallback = (rr, j, a3, f3) =>
                        {
                            var subEl = subsProp.GetArrayElementAtIndex(j);
                            // Draw name field
                            var rName = new Rect(rr.x, rr.y, rr.width, EditorGUIUtility.singleLineHeight);
                            EditorGUI.PropertyField(rName, subEl.FindPropertyRelative("name"), new GUIContent("Name"));
                            // Draw URL field below
                            var rURL = new Rect(rr.x, rr.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                                                rr.width, EditorGUIUtility.singleLineHeight);
                            EditorGUI.PropertyField(rURL, subEl.FindPropertyRelative("videoURL"), new GUIContent("Video URL"));
                        };
                        sList.onAddCallback = ll =>
                        {
                            subsProp.arraySize++;
                            serializedObject.ApplyModifiedProperties();
                            var newSub = subsProp.GetArrayElementAtIndex(subsProp.arraySize - 1);
                            newSub.FindPropertyRelative("name").stringValue = string.Empty;
                            newSub.FindPropertyRelative("videoURL").stringValue = string.Empty;
                            serializedObject.ApplyModifiedProperties();
                        };
                        subtopicLists[subKey] = sList;
                    }
                    Rect subRect = new Rect(
                        r2.x + 16,
                        r2.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                        r2.width - 16,
                        sList.GetHeight());
                    sList.DoList(subRect);
                };

                tlist.onAddCallback = l2 =>
                {
                    topicsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                    var newTopic = topicsProp.GetArrayElementAtIndex(topicsProp.arraySize - 1);
                    newTopic.FindPropertyRelative("name").stringValue = string.Empty;
                    newTopic.FindPropertyRelative("subtopics").arraySize = 0;
                    serializedObject.ApplyModifiedProperties();
                };
                topicLists[topicKey2] = tlist;
            }

            Rect listRect = new Rect(
                rect.x,
                rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                rect.width,
                tlist.GetHeight());
            tlist.DoList(listRect);
        };

        subjectsList.onAddCallback = list =>
        {
            subjectsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newSubj = subjectsProp.GetArrayElementAtIndex(subjectsProp.arraySize - 1);
            newSubj.FindPropertyRelative("name").stringValue = string.Empty;
            newSubj.FindPropertyRelative("topics").arraySize = 0;
            serializedObject.ApplyModifiedProperties();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        subjectsList.DoLayoutList();
        if (serializedObject.ApplyModifiedProperties())
        {
            topicLists.Clear();
            subtopicLists.Clear();
            subjectFoldouts.Clear();
        }
    }
}
