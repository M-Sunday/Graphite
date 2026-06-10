using UnityEditor;
using UnityEngine;

namespace Graphite.Editor
{
    public class TerminalWindow : EditorWindow
    {
        [MenuItem("Window/General/Terminal", false, 49)]
        public static void ShowWindow()
        {
            var window = GetWindow<TerminalWindow>();
            window.titleContent = new GUIContent("Terminal");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Terminal", EditorStyles.boldLabel);
        }
    }
}
