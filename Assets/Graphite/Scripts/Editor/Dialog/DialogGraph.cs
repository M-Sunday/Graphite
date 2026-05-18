using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace Graphite.Dialog
{
    public class DialogGraph : EditorWindow
    {
        [MenuItem("Dialog/Dialogue Window", false, 2000)]
        public static void OpenDialogGraphWindow()
        {
            var window = GetWindow<DialogGraph>();
            window.titleContent = new GUIContent("Dialogue Window");
        }

        public DialogGraphContainer target;

        private VisualElement _noGraphView;
        private DialogGraphView _graphView;
        private VisualElement _toolbar;

        private Button _saveButton;
        private Button _helpButton;
        private static EditorWindow _helpWindow;
        private bool _isLoading;

        private void OnEnable()
        {
            _graphView = null;
            GenerateDialogGraphSelector();

            if (target != null)
            {
                SelectDialogGraphTarget(target);
            }

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (_graphView != null && _graphView.isDirty && target != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to {target.name} before closing?", "Save", "Discard"))
                {
                    Save();
                }
            }

            if (rootVisualElement.Contains(_noGraphView)) rootVisualElement.Remove(_noGraphView);
            if (rootVisualElement.Contains(_graphView)) rootVisualElement.Remove(_graphView);
            if (rootVisualElement.Contains(_toolbar)) rootVisualElement.Remove(_toolbar);
        }

        private void OnUndoRedo()
        {
            if (target != null && _graphView != null)
            {
                _isLoading = true;
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                saveUtility.LoadGraph(target);
                _graphView.isDirty = false;
                _isLoading = false;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (target == null) return;

            bool ctrl = evt.ctrlKey || evt.commandKey;

            if (ctrl && evt.keyCode == KeyCode.S)
            {
                Save();
                evt.StopPropagation();
            }
            else if (ctrl && evt.keyCode == KeyCode.Z && !evt.shiftKey)
            {
                Undo.PerformUndo();
                evt.StopPropagation();
            }
            else if (ctrl && (evt.keyCode == KeyCode.Y || (evt.keyCode == KeyCode.Z && evt.shiftKey)))
            {
                Undo.PerformRedo();
                evt.StopPropagation();
            }
            else if (ctrl && evt.keyCode == KeyCode.A)
            {
                if (_graphView == null) return;
                _graphView.ClearSelection();
                foreach (var node in _graphView.nodes.ToList())
                    _graphView.AddToSelection(node);
                evt.StopPropagation();
            }
        }

        void GenerateDialogGraphSelector()
        {
            _noGraphView = new Toolbar();

            var createButton = new Button(() => CreateNewDialogGraph());
            createButton.text = "Create New Dialog Graph";
            _noGraphView.Add(createButton);

            rootVisualElement.Add(_noGraphView);
        }

        void CreateNewDialogGraph()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (folderPath.Contains("."))
                folderPath = folderPath.Remove(folderPath.LastIndexOf('/'));

            if (string.IsNullOrEmpty(folderPath)) folderPath = "Assets";

            string path = folderPath + "/New Dialog Graph.asset";

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var newGraph = ScriptableObject.CreateInstance<DialogGraphContainer>();
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.Refresh();

            Selection.activeObject = newGraph;
            EditorGUIUtility.PingObject(newGraph);

            SelectDialogGraphTarget(newGraph);
        }

        public void SelectDialogGraphTarget(DialogGraphContainer target)
        {
            this.target = target;

            if (rootVisualElement.Contains(_noGraphView)) rootVisualElement.Remove(_noGraphView);

            _isLoading = true;
            ConstructGraphView();
            GenerateToolbar();

            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.LoadGraph(target);
            _graphView.isDirty = false;
            _isLoading = false;
        }

        private void ConstructGraphView()
        {
            _graphView = new DialogGraphView(this)
            {
                name = "Dialog Graph"
            };
            _graphView.StretchToParentSize();
            GenerateMiniMap();
            GenerateBlackboard();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            _toolbar = new Toolbar();

            _saveButton = new ToolbarButton(() => Save()) { text = "Save" };
            _saveButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _saveButton.style.marginLeft = 4;
            _saveButton.style.marginRight = 4;
            _saveButton.style.paddingLeft = 10;
            _saveButton.style.paddingRight = 10;
            _toolbar.Add(_saveButton);

            _toolbar.Add(new ToolbarSpacer());

            var collapseAllButton = new ToolbarButton(() =>
            {
                if (_graphView != null) _graphView.ToggleCollapseAll();
            }) { text = "Collapse All" };
            collapseAllButton.tooltip = "Collapse/expand all nodes";
            collapseAllButton.style.marginLeft = 4;
            collapseAllButton.style.marginRight = 4;
            collapseAllButton.style.paddingLeft = 10;
            collapseAllButton.style.paddingRight = 10;
            _toolbar.Add(collapseAllButton);

            var showAllButton = new ToolbarButton(() =>
            {
                if (_graphView != null) _graphView.FrameAll();
            }) { text = "Show All" };
            showAllButton.tooltip = "Frame entire graph";
            showAllButton.style.marginLeft = 4;
            showAllButton.style.marginRight = 4;
            showAllButton.style.paddingLeft = 10;
            showAllButton.style.paddingRight = 10;
            _toolbar.Add(showAllButton);

            var zoomSelectedButton = new ToolbarButton(() =>
            {
                if (_graphView != null) _graphView.FrameSelection();
            }) { text = "Zoom Selected" };
            zoomSelectedButton.tooltip = "Frame selected nodes";
            zoomSelectedButton.style.marginLeft = 4;
            zoomSelectedButton.style.marginRight = 4;
            zoomSelectedButton.style.paddingLeft = 10;
            zoomSelectedButton.style.paddingRight = 10;
            _toolbar.Add(zoomSelectedButton);

            _toolbar.Add(new ToolbarSpacer());

            var exportButton = new ToolbarButton(() =>
            {
                if (target != null) DialogGraphTextSerializer.Export(target);
            }) { text = "Export" };
            exportButton.tooltip = "Export dialog to text file";
            exportButton.style.marginLeft = 4;
            exportButton.style.marginRight = 4;
            exportButton.style.paddingLeft = 10;
            exportButton.style.paddingRight = 10;
            _toolbar.Add(exportButton);

            var importButton = new ToolbarButton(() =>
            {
                if (target != null)
                {
                    DialogGraphTextSerializer.Import(target);
                    _graphView.isDirty = true;
                }
            }) { text = "Import" };
            importButton.tooltip = "Import changes from text file";
            importButton.style.marginLeft = 4;
            importButton.style.marginRight = 4;
            importButton.style.paddingLeft = 10;
            importButton.style.paddingRight = 10;
            _toolbar.Add(importButton);

            _toolbar.Add(new ToolbarSpacer());

            var blackboardToggle = new ToolbarToggle
            {
                text = "Blackboard",
                tooltip = "Toggle Blackboard panel",
                value = false
            };
            blackboardToggle.style.marginLeft = 4;
            blackboardToggle.style.marginRight = 4;
            blackboardToggle.style.paddingLeft = 10;
            blackboardToggle.style.paddingRight = 10;
            blackboardToggle.RegisterValueChangedCallback(evt =>
            {
                if (_graphView == null || _graphView.blackboard == null) return;
                _graphView.blackboard.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            _toolbar.Add(blackboardToggle);

            _toolbar.Add(new ToolbarSpacer());

            _helpButton = new ToolbarButton(() => OpenHelpWindow()) { text = "Help" };
            _helpButton.tooltip = "Toggle help window";
            _helpButton.style.marginLeft = 4;
            _helpButton.style.marginRight = 4;
            _helpButton.style.paddingLeft = 10;
            _helpButton.style.paddingRight = 10;
            _toolbar.Add(_helpButton);

            rootVisualElement.Add(_toolbar);
        }

        private void ToggleBlackboard()
        {
            if (_graphView == null || _graphView.blackboard == null) return;
            var bb = _graphView.blackboard;
            bb.style.display = bb.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnGUI()
        {
            if (_graphView != null)
            {
                var graphOrigin = _graphView.worldBound.position;
                _graphView._screenOrigin = GUIUtility.GUIToScreenPoint(graphOrigin);
            }

            if(_saveButton != null) _saveButton.SetEnabled(_graphView != null && _graphView.isDirty);

            if (!_isLoading && _graphView != null && _graphView.isDirty && target != null)
            {
                Undo.RecordObject(target, "Graph Change");
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                saveUtility.SaveGraph(target);
                _graphView.isDirty = false;
            }
        }

        private void GenerateMiniMap()
        {
            var minimap = new MiniMap { anchored = true };
            var coords = _graphView.contentViewContainer.WorldToLocal(new Vector2(maxSize.x - 10, 30));
            minimap.SetPosition(new Rect(coords.x, coords.y, 200, 140));
            _graphView.Add(minimap);
        }

        private void GenerateBlackboard()
        {
            var blackboard = new Blackboard();
            blackboard.Add(new BlackboardSection { title = "Exposed Properties" });

            blackboard.addItemRequested = _blackboard => { _graphView.AddPropertyToBlackboard(new ExposedProperty()); };
            blackboard.editTextRequested = (blackboard1, element, newValue) =>
            {
                var oldName = ((BlackboardField)element).text;
                if (_graphView.exposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error", "Property name already exists", "ok");
                    return;
                }

                var index = _graphView.exposedProperties.FindIndex(x => x.PropertyName == oldName);
                _graphView.exposedProperties[index].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;
            };

            blackboard.SetPosition(new Rect(10, 30, 200, 300));
            blackboard.style.display = DisplayStyle.None;

            _graphView.Add(blackboard);
            _graphView.blackboard = blackboard;
        }

        void Save()
        {
            if (target == null)
            {
                EditorUtility.DisplayDialog("Target graph asset missing", "", "ok");
                return;
            }

            Undo.RecordObject(target, "Save Graph");
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.SaveGraph(target);
            AssetDatabase.SaveAssets();
            _graphView.isDirty = false;
            Debug.Log($"Dialog graph saved: {target.name}");
        }

        void OpenHelpWindow()
        {
            if(_helpWindow == null)
            {
                _helpWindow = DialogGraphHelp.OpenDialogGraphHelpWindow();
            }
            else
            {
                _helpWindow.Close();
                _helpWindow = null;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OpenDialogGraph(int instanceID, int line)
        {
            var asset = UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
            if (asset is DialogGraphContainer)
            {
                var window = GetWindow<DialogGraph>();
                if (window == null) window = CreateWindow<DialogGraph>();

                window.rootVisualElement.Clear();

                window.SelectDialogGraphTarget(asset as DialogGraphContainer);

                return true;
            }
            return false;
        }
#endif
    }

    public class DialogGraphHelp : EditorWindow
    {
        public static EditorWindow OpenDialogGraphHelpWindow()
        {
            var window = GetWindow<DialogGraphHelp>();
            window.titleContent = new GUIContent("Dialog Graph Help");
            window.minSize = new Vector2(320, 400);
            return window;
        }

        private void OnEnable()
        {
            AddHelpText();
        }

        void AddHelpText()
        {
            var scroll = new ScrollView();
            scroll.style.paddingLeft = 10;
            scroll.style.paddingRight = 10;
            scroll.style.paddingTop = 10;
            scroll.style.paddingBottom = 10;

            AddSection(scroll, "Keyboard Shortcuts", new[]
            {
                "Ctrl+S  — Save graph",
                "Ctrl+Z  — Undo",
                "Ctrl+Y / Ctrl+Shift+Z  — Redo",
                "Ctrl+A  — Select all nodes",
            });

            AddSection(scroll, "Node Types", new[]
            {
                "Entry     — Graph entry point (auto-created)",
                "Dialog    — Character speaks a line of dialogue",
                "Option    — A player choice (linked from Dialog ports)",
                "Response  — Character reply after an option is selected",
                "Retrigger — Re-evaluate a branch",
                "Event     — Fire a custom event",
                "Comparison — Conditional branch based on a property",
                "Exit      — End of a dialog branch",
            });

            AddSection(scroll, "Reaction Tags", new[]
            {
                "<wave></wave>       — Wavy text animation",
                "<alert></alert>     — Shake with pulsing color",
                "<shake></shake>     — Shake effect",
                "<glitch></glitch>   — Glitch displacement",
                "<rainbow></rainbow> — Rainbow cycling colors",
                "<b></b>             — Bold text",
                "<i></i>             — Italic text",
                "<u></u>             — Underlined text",
                "<_pause>            — Pause typing at this point",
                "<color=red></color> — Custom text color",
                "<size=1.5></size>   — Scale text (1.0 = normal)",
            });

            AddSection(scroll, "Inline Events", new[]
            {
                "[event]  — Fire a custom event via DialogEventRelay",
                "[____]   — Pause (each _ = 0.2s + 0.2s base)",
            });

            AddSection(scroll, "Text Export / Import", new[]
            {
                "Export Text — Saves a .dialog.txt file next to your",
                "              graph asset with all dialog content.",
                "",
                "Import Text — Reads the .dialog.txt file back and",
                "              updates node content (text, characters).",
                "",
                "Use this to edit dialog in VS Code, translate,",
                "batch-edit, or review without opening Unity.",
            });

            rootVisualElement.Add(scroll);
        }

        void AddSection(VisualElement parent, string title, string[] lines)
        {
            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginTop = 12;
            titleLabel.style.marginBottom = 4;
            titleLabel.style.fontSize = 13;
            parent.Add(titleLabel);

            foreach (var line in lines)
            {
                var label = new Label(line);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginLeft = 10;
                label.style.marginBottom = 1;
                label.style.fontSize = 11;
                parent.Add(label);
            }
        }
    }
}
