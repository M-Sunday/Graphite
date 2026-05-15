using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class QuickAccessMenu
{
    private const string PrefsKey = "QuickAccess_ScenePaths";
    private const string GeneratedPath = "Assets/Editor/QuickAccessScenes_Generated.cs";

    static QuickAccessMenu()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(GeneratedPath))
                Regenerate();
        };
    }

    [MenuItem("Quick Access/Add Scene", false, 2200)]
    private static void AddScene()
    {
        string path = EditorUtility.OpenFilePanel("Select a Scene", "Assets/Scenes", "unity");
        if (string.IsNullOrEmpty(path)) return;

        string relativePath = GetRelativePath(path);
        if (string.IsNullOrEmpty(relativePath))
        {
            EditorUtility.DisplayDialog("Error", "Scene must be inside the project's Assets folder.", "OK");
            return;
        }

        var scenes = LoadScenes();
        if (scenes.Contains(relativePath))
        {
            EditorUtility.DisplayDialog("Already Added", $"\"{Path.GetFileNameWithoutExtension(relativePath)}\" is already in Quick Access.", "OK");
            return;
        }

        scenes.Add(relativePath);
        SaveScenes(scenes);
        Regenerate();
    }

    [MenuItem("Quick Access/Clear All", false, 2202)]
    private static void ClearAll()
    {
        if (EditorUtility.DisplayDialog("Clear All", "Remove all scenes from Quick Access?", "Clear", "Cancel"))
        {
            SaveScenes(new List<string>());
            Regenerate();
        }
    }

    public static void OpenSceneByIndex(int index)
    {
        var scenes = LoadScenes();
        if (index >= 0 && index < scenes.Count)
            LoadScene(scenes[index]);
    }

    public static void DeleteSceneByIndex(int index)
    {
        var scenes = LoadScenes();
        if (index >= 0 && index < scenes.Count)
        {
            if (EditorUtility.DisplayDialog("Remove Scene", $"Remove \"{Path.GetFileNameWithoutExtension(scenes[index])}\" from Quick Access?", "Remove", "Cancel"))
            {
                scenes.RemoveAt(index);
                SaveScenes(scenes);
                Regenerate();
            }
        }
    }

    public static void Regenerate()
    {
        var scenes = LoadScenes();
        string code = "using UnityEngine;\nusing UnityEditor;\n\n";
        code += "public static class QuickAccessScenes\n{\n";

        if (scenes.Count == 0)
        {
            code += "    [MenuItem(\"Quick Access/My Scenes/(empty)\", false, 2300)]\n";
            code += "    private static void OpenSlot0() { }\n\n";
        }
        else
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                string name = Sanitize(Path.GetFileNameWithoutExtension(scenes[i]));
                int basePrio = 2300 + i * 10;
                code += $"    [MenuItem(\"Quick Access/My Scenes/{name}/Open\", false, {basePrio})]\n";
                code += $"    private static void OpenSlot{i}() => QuickAccessMenu.OpenSceneByIndex({i});\n\n";
                code += $"    [MenuItem(\"Quick Access/My Scenes/{name}/Delete\", false, {basePrio + 1})]\n";
                code += $"    private static void DeleteSlot{i}() => QuickAccessMenu.DeleteSceneByIndex({i});\n\n";
            }
        }

        code += "}\n";
        File.WriteAllText(GeneratedPath, code);
        AssetDatabase.Refresh();
    }

    private static string Sanitize(string name)
    {
        return name.Replace("/", "\\");
    }

    private static List<string> LoadAndValidateScenes()
    {
        var scenes = LoadScenes();
        bool changed = false;
        for (int i = scenes.Count - 1; i >= 0; i--)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenes[i]));
            if (!File.Exists(fullPath))
            {
                scenes.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) { SaveScenes(scenes); Regenerate(); }
        return scenes;
    }

    private static string GetRelativePath(string absolutePath)
    {
        string dataPath = Application.dataPath;
        string projectPath = Path.GetDirectoryName(dataPath);
        if (absolutePath.Replace('/', '\\').StartsWith(projectPath.Replace('/', '\\') + "\\"))
            return absolutePath.Substring(projectPath.Length + 1).Replace('\\', '/');
        return null;
    }

    private static List<string> LoadScenes()
    {
        string data = EditorPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(data)) return new List<string>();
        return new List<string>(data.Split('|'));
    }

    private static void SaveScenes(List<string> scenes)
    {
        EditorPrefs.SetString(PrefsKey, string.Join("|", scenes));
    }

    private static void LoadScene(string path)
    {
        if (EditorApplication.isPlaying)
            SceneManager.LoadScene(Path.GetFileNameWithoutExtension(path));
        else
            EditorSceneManager.OpenScene(path);
    }
}
