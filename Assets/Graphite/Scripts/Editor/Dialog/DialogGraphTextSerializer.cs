using System;
using System.Collections.Generic;
using System.IO;

using System.Text;
using UnityEditor;
using UnityEngine;

namespace Graphite.Dialog
{
    public static class DialogGraphTextSerializer
    {
        private static string GetExportPath(DialogGraphContainer target)
        {
            var path = AssetDatabase.GetAssetPath(target);
            return path + ".dialog.txt";
        }

        public static void Export(DialogGraphContainer target)
        {
            if (target == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"# {target.name}");
            sb.AppendLine();

            foreach (var node in target.nodeData)
            {
                if (node is SerializedDialogNode dn)
                {
                    sb.AppendLine($"=dialog # {dn.GUID}");
                    sb.AppendLine($"{dn.character}: {Escape(dn.dialogText)}");
                    sb.AppendLine();
                }
                else if (node is SerializedOptionNode on)
                {
                    sb.AppendLine($"=option # {on.GUID}");
                    sb.AppendLine(Escape(on.optionText));
                    sb.AppendLine();
                }
                else if (node is SerializedResponseNode rn)
                {
                    sb.AppendLine($"=response # {rn.GUID}");
                    sb.AppendLine($"{rn.character}: {Escape(rn.responseText)}");
                    sb.AppendLine();
                }
            }

            var path = GetExportPath(target);
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
            Debug.Log($"Exported dialog text: {path}");
        }

        public static void Import(DialogGraphContainer target)
        {
            var path = GetExportPath(target);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Import Error", $"No dialog text file found at:\n{path}\n\nExport the graph first.", "OK");
                return;
            }

            var lines = File.ReadAllLines(path);
            var guidMap = new Dictionary<string, (string field, string value)>();

            string currentGuid = null;
            string currentType = null;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("#")) continue;
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("=dialog") || line.StartsWith("=option") || line.StartsWith("=response"))
                {
                    var hashIndex = line.LastIndexOf('#');
                    currentGuid = hashIndex > 0 ? line.Substring(hashIndex + 1).Trim() : null;
                    currentType = line.StartsWith("=dialog") ? "dialog" : line.StartsWith("=option") ? "option" : "response";
                    continue;
                }

                if (currentGuid == null) continue;

                if (currentType == "option")
                {
                    guidMap[currentGuid] = ("optionText", Unescape(line));
                }
                else
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        guidMap[currentGuid] = (line.Substring(0, colonIndex).Trim(), Unescape(line.Substring(colonIndex + 1).Trim()));
                    }
                }
            }

            Undo.RecordObject(target, "Import Dialog Text");

            foreach (var node in target.nodeData)
            {
                if (!guidMap.TryGetValue(node.GUID, out var entry)) continue;

                if (node is SerializedDialogNode dn)
                {
                    if (Enum.TryParse<CharacterName>(entry.field, out var ch))
                        dn.character = ch;
                    dn.dialogText = entry.value;
                }
                else if (node is SerializedOptionNode on)
                {
                    on.optionText = entry.value;
                }
                else if (node is SerializedResponseNode rn)
                {
                    if (Enum.TryParse<CharacterName>(entry.field, out var ch))
                        rn.character = ch;
                    rn.responseText = entry.value;
                }
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"Imported dialog text: {path}");
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\n", "\\n");
        }

        private static string Unescape(string text)
        {
            return text.Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
