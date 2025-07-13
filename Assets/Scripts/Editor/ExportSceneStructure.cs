using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ExportSceneStructure
{
    [MenuItem("Tools/Export Scene & Prefab Structure")]
    public static void Export()
    {
        var exportPath = Path.Combine(Application.dataPath, "scene_structure.json");
        var allEntries = new List<SceneEntry>();

        // Проходим по всем открытым сценам
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
                CollectHierarchy(root, scene.name, allEntries);
        }

        // Проходим по всем префабам в Assets
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            CollectHierarchy(prefab, Path.GetFileNameWithoutExtension(path), allEntries);
        }

        // Сериализуем в JSON
        var json = JsonUtility.ToJson(new SceneCollection { scenes = allEntries }, prettyPrint: true);
        File.WriteAllText(exportPath, json);
        AssetDatabase.Refresh();
        Debug.Log($"Scene structure exported to {exportPath}");
    }

    static void CollectHierarchy(GameObject go, string containerName, List<SceneEntry> list)
    {
        var entry = new SceneEntry
        {
            container = containerName,
            path = GetFullPath(go),
            components = new List<string>()
        };
        foreach (var comp in go.GetComponents<Component>())
            entry.components.Add(comp == null ? "Missing" : comp.GetType().Name);

        list.Add(entry);
        foreach (Transform child in go.transform)
            CollectHierarchy(child.gameObject, containerName, list);
    }

    static string GetFullPath(GameObject go)
    {
        return go.transform.parent == null
            ? go.name
            : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    [System.Serializable]
    public class SceneEntry
    {
        public string container;
        public string path;
        public List<string> components;
    }

    [System.Serializable]
    public class SceneCollection
    {
        public List<SceneEntry> scenes;
    }
}