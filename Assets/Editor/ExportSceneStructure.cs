#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FullProjectExporter
{
    [MenuItem("Tools/Export Full Project Structure Detailed")]
    public static void ExportAllDetailed()
    {
        var exportPath = Path.Combine(Application.dataPath, "full_project_structure_detailed.json");
        var data = new FullProjectData { assets = new List<AssetEntry>() };

        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/")) continue;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name;
            var entry = new AssetEntry
            {
                path = path,
                type = type,
                dependencies = AssetDatabase.GetDependencies(path),
                hierarchy = null
            };

            if (type == "SceneAsset")
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                entry.hierarchy = CollectHierarchyDetailed(scene, Path.GetFileNameWithoutExtension(path));
                EditorSceneManager.CloseScene(scene, true);
            }
            else if (type == "GameObject")
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                entry.hierarchy = CollectHierarchyDetailed(prefab, Path.GetFileNameWithoutExtension(path));
            }

            data.assets.Add(entry);
        }

        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(exportPath, json);
        AssetDatabase.Refresh();
        Debug.Log($"Detailed project structure exported to {exportPath}");
    }

    static List<HierarchyEntry> CollectHierarchyDetailed(Scene scene, string container)
    {
        var list = new List<HierarchyEntry>();
        foreach (var root in scene.GetRootGameObjects())
            CollectDetailed(root, container, list);
        return list;
    }

    static List<HierarchyEntry> CollectHierarchyDetailed(GameObject prefab, string container)
    {
        var list = new List<HierarchyEntry>();
        CollectDetailed(prefab, container, list);
        return list;
    }

    static void CollectDetailed(GameObject go, string container, List<HierarchyEntry> list)
    {
        var entry = new HierarchyEntry
        {
            container = container,
            path = GetFullPath(go),
            components = new List<ComponentData>()
        };

        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) continue;
            var compData = new ComponentData
            {
                type = comp.GetType().Name,
                properties = new Dictionary<string, string>()
            };
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.name == "m_Script") continue;
                string value;
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        value = prop.boolValue.ToString();
                        break;
                    case SerializedPropertyType.Integer:
                        value = prop.intValue.ToString();
                        break;
                    case SerializedPropertyType.Float:
                        value = prop.floatValue.ToString();
                        break;
                    case SerializedPropertyType.String:
                        value = prop.stringValue;
                        break;
                    case SerializedPropertyType.Color:
                        value = prop.colorValue.ToString();
                        break;
                    case SerializedPropertyType.ObjectReference:
                        value = prop.objectReferenceValue != null
                            ? prop.objectReferenceValue.name
                            : "null";
                        break;
                    case SerializedPropertyType.Enum:
                        var names = prop.enumNames;
                        var idx = prop.enumValueIndex;
                        value = (names != null && idx >= 0 && idx < names.Length)
                            ? names[idx]
                            : idx.ToString();
                        break;
                    default:
                        value = prop.propertyType.ToString();
                        break;
                }
                compData.properties[prop.name] = value;
            }
            entry.components.Add(compData);
        }

        list.Add(entry);
        foreach (Transform child in go.transform)
            CollectDetailed(child.gameObject, container, list);
    }

    static string GetFullPath(GameObject go) =>
        go.transform.parent == null
            ? go.name
            : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;

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
        public List<ComponentData> components;
    }

    [System.Serializable]
    public class ComponentData
    {
        public string type;
        public Dictionary<string, string> properties;
    }

    [System.Serializable]
    public class FullProjectData
    {
        public List<AssetEntry> assets;
    }
}
#endif
