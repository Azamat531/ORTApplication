//using UnityEngine;
//using UnityEditor;
//using UnityEditorInternal;
//using System.Collections.Generic;

//[CustomEditor(typeof(CheckTestQuestions))]
//public class CheckTestQuestionsEditor : Editor
//{
//    private ReorderableList subjectsList;
//    private Dictionary<int, bool> subjectFoldouts = new();
//    private Dictionary<string, ReorderableList> topicLists = new();
//    private Dictionary<string, bool> topicFoldouts = new();
//    private Dictionary<string, ReorderableList> questionLists = new();
//    private bool needsClear = false;

//    void OnEnable()
//    {
//        var subjectsProp = serializedObject.FindProperty("subjects");
//        subjectsList = new ReorderableList(serializedObject, subjectsProp, true, true, true, true);

//        subjectsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Subjects");

//        subjectsList.elementHeightCallback = index =>
//        {
//            float height = EditorGUIUtility.singleLineHeight + 4;
//            bool expanded = subjectFoldouts.ContainsKey(index) && subjectFoldouts[index];
//            if (!expanded) return height;

//            string topicKey = $"topics_{index}";
//            if (topicLists.TryGetValue(topicKey, out var tlist))
//                height += tlist.GetHeight() + 8;

//            return height;
//        };

//        subjectsList.drawElementCallback = (rect, index, active, focused) =>
//        {
//            var subjectsProp = serializedObject.FindProperty("subjects");
//            var subjectEl = subjectsProp.GetArrayElementAtIndex(index);

//            if (!subjectFoldouts.ContainsKey(index)) subjectFoldouts[index] = true;
//            Rect foldRect = new Rect(rect.x, rect.y, 12, EditorGUIUtility.singleLineHeight);
//            subjectFoldouts[index] = EditorGUI.Foldout(foldRect, subjectFoldouts[index], GUIContent.none);

//            Rect nameRect = new Rect(rect.x + 16, rect.y, rect.width - 16, EditorGUIUtility.singleLineHeight);
//            EditorGUI.PropertyField(nameRect, subjectEl.FindPropertyRelative("name"), GUIContent.none);

//            if (!subjectFoldouts[index]) return;

//            var topicsProp = subjectEl.FindPropertyRelative("topics");
//            string topicKey = $"topics_{index}";

//            if (!topicLists.TryGetValue(topicKey, out var tlist))
//            {
//                tlist = CreateTopicList(topicsProp, index);
//                topicLists[topicKey] = tlist;
//            }

//            Rect listRect = new Rect(rect.x + 10, rect.y + EditorGUIUtility.singleLineHeight + 4, rect.width - 10, tlist.GetHeight());
//            tlist.DoList(listRect);
//        };

//        subjectsList.onAddCallback = list =>
//        {
//            var subjectsProp = serializedObject.FindProperty("subjects");
//            subjectsProp.arraySize++;
//            serializedObject.ApplyModifiedProperties();
//            var newSub = subjectsProp.GetArrayElementAtIndex(subjectsProp.arraySize - 1);
//            newSub.FindPropertyRelative("name").stringValue = "";
//            newSub.FindPropertyRelative("topics").arraySize = 0;
//        };
//    }

//    private ReorderableList CreateTopicList(SerializedProperty topicsProp, int subjectIndex)
//    {
//        var list = new ReorderableList(serializedObject, topicsProp, true, true, true, true);

//        list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Topics");

//        list.elementHeightCallback = index =>
//        {
//            float h = EditorGUIUtility.singleLineHeight + 4;
//            string qKey = $"questions_{subjectIndex}_{index}";
//            if (topicFoldouts.TryGetValue(qKey, out var expanded) && expanded)
//                if (questionLists.TryGetValue(qKey, out var qList))
//                    h += qList.GetHeight() + 6;
//            return h;
//        };

//        list.drawElementCallback = (r, i, a, f) =>
//        {
//            var topicEl = topicsProp.GetArrayElementAtIndex(i);
//            string qKey = $"questions_{subjectIndex}_{i}";
//            if (!topicFoldouts.ContainsKey(qKey)) topicFoldouts[qKey] = true;

//            Rect fold = new Rect(r.x, r.y, 12, EditorGUIUtility.singleLineHeight);
//            topicFoldouts[qKey] = EditorGUI.Foldout(fold, topicFoldouts[qKey], GUIContent.none);

//            Rect nameRect = new Rect(r.x + 16, r.y, r.width - 16, EditorGUIUtility.singleLineHeight);
//            EditorGUI.PropertyField(nameRect, topicEl.FindPropertyRelative("name"), GUIContent.none);

//            if (!topicFoldouts[qKey]) return;

//            var questionsProp = topicEl.FindPropertyRelative("questions");

//            if (!questionLists.TryGetValue(qKey, out var qList))
//            {
//                qList = CreateQuestionList(questionsProp);
//                questionLists[qKey] = qList;
//            }

//            Rect listRect = new Rect(r.x + 16, r.y + EditorGUIUtility.singleLineHeight + 4, r.width - 16, qList.GetHeight());
//            qList.DoList(listRect);
//        };

//        list.onAddCallback = l =>
//        {
//            topicsProp.arraySize++;
//            serializedObject.ApplyModifiedProperties();
//            var newTopic = topicsProp.GetArrayElementAtIndex(topicsProp.arraySize - 1);
//            newTopic.FindPropertyRelative("name").stringValue = "";
//            newTopic.FindPropertyRelative("questions").arraySize = 0;
//        };

//        return list;
//    }

//    private ReorderableList CreateQuestionList(SerializedProperty questionsProp)
//    {
//        var list = new ReorderableList(serializedObject, questionsProp, true, true, true, true);

//        list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Questions");

//        list.elementHeightCallback = i =>
//        {
//            var q = questionsProp.GetArrayElementAtIndex(i);
//            float height = 4;
//            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("questionText"));
//            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("imageUrl"));
//            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("correctIndex"));
//            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("options"));
//            return height + 10;
//        };

//        list.drawElementCallback = (r, i, a, f) =>
//        {
//            var q = questionsProp.GetArrayElementAtIndex(i);
//            float y = r.y + 2;
//            float spacing = EditorGUIUtility.singleLineHeight + 4;

//            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("questionText"), new GUIContent("Вопрос"));
//            y += spacing;

//            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("imageUrl"), new GUIContent("URL картинки"));
//            y += spacing;

//            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("correctIndex"), new GUIContent("Правильный индекс"));
//            y += spacing;

//            EditorGUI.PropertyField(new Rect(r.x, y, r.width, EditorGUI.GetPropertyHeight(q.FindPropertyRelative("options"))),
//                q.FindPropertyRelative("options"), new GUIContent("Варианты"));
//        };

//        list.onAddCallback = l =>
//        {
//            questionsProp.arraySize++;
//            serializedObject.ApplyModifiedProperties();
//            var newQ = questionsProp.GetArrayElementAtIndex(questionsProp.arraySize - 1);
//            newQ.FindPropertyRelative("questionText").stringValue = "";
//            newQ.FindPropertyRelative("imageUrl").stringValue = "";
//            newQ.FindPropertyRelative("correctIndex").intValue = 0;

//            var optionsProp = newQ.FindPropertyRelative("options");
//            optionsProp.ClearArray();
//            for (int o = 0; o < 4; o++)
//            {
//                optionsProp.InsertArrayElementAtIndex(o);
//                optionsProp.GetArrayElementAtIndex(o).stringValue = "";
//            }

//            serializedObject.ApplyModifiedProperties();
//        };

//        return list;
//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();
//        subjectsList.DoLayoutList();
//        if (needsClear)
//        {
//            topicLists.Clear();
//            questionLists.Clear();
//            subjectFoldouts.Clear();
//            topicFoldouts.Clear();
//            needsClear = false;
//        }
//        serializedObject.ApplyModifiedProperties();
//    }
//}

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(CheckTestQuestions))]
public class CheckTestQuestionsEditor : Editor
{
    private ReorderableList subjectsList;
    private Dictionary<int, bool> subjectFoldouts = new();
    private Dictionary<string, ReorderableList> topicLists = new();
    private Dictionary<string, bool> topicFoldouts = new();
    private Dictionary<string, ReorderableList> questionLists = new();

    void OnEnable()
    {
        var subjectsProp = serializedObject.FindProperty("subjects");
        subjectsList = new ReorderableList(serializedObject, subjectsProp, true, true, true, true);

        subjectsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Subjects");

        subjectsList.elementHeightCallback = index =>
        {
            float height = EditorGUIUtility.singleLineHeight + 4;
            bool expanded = subjectFoldouts.ContainsKey(index) && subjectFoldouts[index];
            if (!expanded) return height;

            string topicKey = $"topics_{index}";
            if (topicLists.TryGetValue(topicKey, out var tlist))
                height += tlist.GetHeight() + 8;

            return height;
        };

        subjectsList.drawElementCallback = (rect, index, active, focused) =>
        {
            var subjectsProp = serializedObject.FindProperty("subjects");
            var subjectEl = subjectsProp.GetArrayElementAtIndex(index);

            if (!subjectFoldouts.ContainsKey(index)) subjectFoldouts[index] = true;
            Rect foldRect = new Rect(rect.x, rect.y, 12, EditorGUIUtility.singleLineHeight);
            subjectFoldouts[index] = EditorGUI.Foldout(foldRect, subjectFoldouts[index], GUIContent.none);

            Rect nameRect = new Rect(rect.x + 16, rect.y, rect.width - 16, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(nameRect, subjectEl.FindPropertyRelative("name"), GUIContent.none);

            if (!subjectFoldouts[index]) return;

            var topicsProp = subjectEl.FindPropertyRelative("topics");
            string topicKey = $"topics_{index}";

            if (!topicLists.TryGetValue(topicKey, out var tlist))
            {
                tlist = CreateTopicList(topicsProp, index);
                topicLists[topicKey] = tlist;
            }

            Rect listRect = new Rect(rect.x + 10, rect.y + EditorGUIUtility.singleLineHeight + 4, rect.width - 10, tlist.GetHeight());
            tlist.DoList(listRect);
        };

        subjectsList.onAddCallback = list =>
        {
            var subjectsProp = serializedObject.FindProperty("subjects");
            subjectsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newSub = subjectsProp.GetArrayElementAtIndex(subjectsProp.arraySize - 1);
            newSub.FindPropertyRelative("name").stringValue = "";
            newSub.FindPropertyRelative("topics").arraySize = 0;
        };

        subjectsList.onRemoveCallback = list =>
        {
            if (EditorUtility.DisplayDialog("Удаление предмета", "Вы уверены, что хотите удалить этот предмет?", "Да", "Нет"))
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }
        };
    }

    private ReorderableList CreateTopicList(SerializedProperty topicsProp, int subjectIndex)
    {
        var list = new ReorderableList(serializedObject, topicsProp, true, true, true, true);

        list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Topics");

        list.elementHeightCallback = index =>
        {
            float h = EditorGUIUtility.singleLineHeight + 4;
            string qKey = $"questions_{subjectIndex}_{index}";
            if (topicFoldouts.TryGetValue(qKey, out var expanded) && expanded)
                if (questionLists.TryGetValue(qKey, out var qList))
                    h += qList.GetHeight() + 6;
            return h;
        };

        list.drawElementCallback = (r, i, a, f) =>
        {
            var topicEl = topicsProp.GetArrayElementAtIndex(i);
            string qKey = $"questions_{subjectIndex}_{i}";
            if (!topicFoldouts.ContainsKey(qKey)) topicFoldouts[qKey] = true;

            Rect fold = new Rect(r.x, r.y, 12, EditorGUIUtility.singleLineHeight);
            topicFoldouts[qKey] = EditorGUI.Foldout(fold, topicFoldouts[qKey], GUIContent.none);

            Rect nameRect = new Rect(r.x + 16, r.y, r.width - 16, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(nameRect, topicEl.FindPropertyRelative("name"), GUIContent.none);

            if (!topicFoldouts[qKey]) return;

            var questionsProp = topicEl.FindPropertyRelative("questions");

            if (!questionLists.TryGetValue(qKey, out var qList))
            {
                qList = CreateQuestionList(questionsProp);
                questionLists[qKey] = qList;
            }

            Rect listRect = new Rect(r.x + 16, r.y + EditorGUIUtility.singleLineHeight + 4, r.width - 16, qList.GetHeight());
            qList.DoList(listRect);
        };

        list.onAddCallback = l =>
        {
            topicsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newTopic = topicsProp.GetArrayElementAtIndex(topicsProp.arraySize - 1);
            newTopic.FindPropertyRelative("name").stringValue = "";
            newTopic.FindPropertyRelative("questions").arraySize = 0;
        };

        list.onRemoveCallback = l =>
        {
            if (EditorUtility.DisplayDialog("Удаление темы", "Вы уверены, что хотите удалить эту тему?", "Да", "Нет"))
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
            }
        };

        return list;
    }

    private ReorderableList CreateQuestionList(SerializedProperty questionsProp)
    {
        var list = new ReorderableList(serializedObject, questionsProp, true, true, true, true);

        list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Questions");

        list.elementHeightCallback = i =>
        {
            var q = questionsProp.GetArrayElementAtIndex(i);
            float height = 4;
            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("questionText"));
            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("imageUrl"));
            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("correctIndex"));
            height += EditorGUI.GetPropertyHeight(q.FindPropertyRelative("options"));
            return height + 10;
        };

        list.drawElementCallback = (r, i, a, f) =>
        {
            var q = questionsProp.GetArrayElementAtIndex(i);
            float y = r.y + 2;
            float spacing = EditorGUIUtility.singleLineHeight + 4;

            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("questionText"), new GUIContent("Вопрос"));
            y += spacing;

            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("imageUrl"), new GUIContent("URL картинки"));
            y += spacing;

            EditorGUI.PropertyField(new Rect(r.x, y, r.width, spacing), q.FindPropertyRelative("correctIndex"), new GUIContent("Правильный индекс"));
            y += spacing;

            EditorGUI.PropertyField(new Rect(r.x, y, r.width, EditorGUI.GetPropertyHeight(q.FindPropertyRelative("options"))),
                q.FindPropertyRelative("options"), new GUIContent("Варианты"));
        };

        list.onAddCallback = l =>
        {
            questionsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            var newQ = questionsProp.GetArrayElementAtIndex(questionsProp.arraySize - 1);
            newQ.FindPropertyRelative("questionText").stringValue = "";
            newQ.FindPropertyRelative("imageUrl").stringValue = "";
            newQ.FindPropertyRelative("correctIndex").intValue = 0;

            var optionsProp = newQ.FindPropertyRelative("options");
            optionsProp.ClearArray();
            for (int o = 0; o < 4; o++)
            {
                optionsProp.InsertArrayElementAtIndex(o);
                optionsProp.GetArrayElementAtIndex(o).stringValue = "";
            }

            serializedObject.ApplyModifiedProperties();
        };

        return list;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        subjectsList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
