using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Graphite.Dialog
{
    // ===============================================================
    // Node search window for dialog graphs
    // ===============================================================
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogGraphView _graphView;
        private EditorWindow _window;
        private Port _sourcePort;

        public void Init(DialogGraphView graph, EditorWindow window)
        {
            _graphView = graph;
            _window = window;
        }

        public void SetSourcePort(Port port)
        {
            _sourcePort = port;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>()
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeEntry(new GUIContent("Dialog Node"))
            {
                userData = new DialogNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Retrigger Node"))
            {
                userData = new RetriggerNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Event Node"))
            {
                userData = new EventNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Comparison Node"))
            {
                userData = new ComparisonNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Option Node"))
            {
                userData = new OptionNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Response Node"))
            {
                userData = new ResponseNode(), level = 1
            },
            new SearchTreeEntry(new GUIContent("Exit Node"))
            {
                userData = new ExitNode(), level = 1
            }
        };
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var worldMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent,
                context.screenMousePosition - _window.position.position);
            var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);

            if (SearchTreeEntry.userData is DialogNode)
            {
                var node = DialogNode.Create(_graphView, Vector2.zero);
                node.dialogText.value = "Dialog Node";
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is RetriggerNode)
            {
                var node = RetriggerNode.Create(_graphView, Vector2.zero);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is EntryNode)
            {
                var node = EntryNode.Create(_graphView);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is ComparisonNode)
            {
                var node = ComparisonNode.Create(_graphView, Vector2.zero);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is OptionNode)
            {
                var node = OptionNode.Create(_graphView, Vector2.zero);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is ResponseNode)
            {
                var node = ResponseNode.Create(_graphView, Vector2.zero);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else if (SearchTreeEntry.userData is ExitNode)
            {
                var node = ExitNode.Create(_graphView, Vector2.zero);
                if (_sourcePort != null)
                    _graphView.CreateNodeFromPort(_sourcePort, node, localMousePosition);
                else
                {
                    node.SetPosition(new Rect(localMousePosition, _graphView.defaultNodeSize));
                    _graphView.AddElement(node);
                }
                _sourcePort = null;
                _graphView.isDirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}