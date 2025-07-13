using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FullProjectExporter
{
    [MenuItem("Tools/Export Full Project Structure")]
    public static void ExportAll()
    {
        var exportPath = Path.Combine(Application.dataPath, "full_project_structure.json");
        var data = new FullProjectData { assets = new List<AssetEntry>() };

        // Перечисляем все активы в проекте
        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/")) continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name;
            var entry = new AssetEntry
            {
                path = path,
                type = type,
                dependencies = AssetDatabase.GetDependencies(path)
            };

            // Если сцена, экспортируем её иерархию
            if (type == "SceneAsset")
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                entry.hierarchy = CollectHierarchy(scene, Path.GetFileNameWithoutExtension(path));
                EditorSceneManager.CloseScene(scene, true);
            }
            // Если префаб, тоже экспортируем
            else if (type == "GameObject")
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                entry.hierarchy = CollectHierarchy(go, Path.GetFileNameWithoutExtension(path));
            }

            data.assets.Add(entry);
        }

        // Сериализуем в JSON
        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path.Combine(Application.dataPath, "full_project_structure.json"), json);
        AssetDatabase.Refresh();
        Debug.Log($"Full project structure exported to {exportPath}");
    }

    static List<HierarchyEntry> CollectHierarchy(Scene scene, string container)
    {
        var list = new List<HierarchyEntry>();
        foreach (var root in scene.GetRootGameObjects())
            Collect(root, container, list);
        return list;
    }

    static List<HierarchyEntry> CollectHierarchy(GameObject prefab, string container)
    {
        var list = new List<HierarchyEntry>();
        Collect(prefab, container, list);
        return list;
    }

    static void Collect(GameObject go, string container, List<HierarchyEntry> list)
    {
        var entry = new HierarchyEntry
        {
            container = container,
            path = GetFullPath(go),
            components = new List<string>()
        };
        foreach (var comp in go.GetComponents<Component>())
            entry.components.Add(comp ? comp.GetType().Name : "Missing");
        list.Add(entry);
        foreach (Transform child in go.transform)
            Collect(child.gameObject, container, list);
    }

    static string GetFullPath(GameObject go)
    {
        return go.transform.parent == null
            ? go.name
            : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    [System.Serializable]
    public class AssetEntry
    {
        public string path;
        public string type;
        public string[] dependencies;
        public List<HierarchyEntry> hierarchy;
    }

    [System.Serializable]
    public class HierarchyEntry
    {
        public string container;
        public string path;
        public List<string> components;
    }

    [System.Serializable]
    public class FullProjectData
    {
        public List<AssetEntry> assets;
    }
}