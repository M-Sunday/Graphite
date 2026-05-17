using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Graphite.Dialog
{
    // ===============================================================
    // Dialog Graph editor using unity's GraphView
    // ===============================================================
    public class DialogGraphView : GraphView
    {
        public readonly Vector2 defaultNodeSize = new Vector2(150, 200);

        public Blackboard blackboard;
        public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();

        private NodeSearchWindow _searchWindow;
        private EditorWindow _editorWindow;
        public Port _dragSourcePort;
        public Vector2 _lastMousePosition;
        public Vector2 _screenOrigin;

        public bool isDirty = false;

        public DialogGraphView(EditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
            styleSheets.Add(Resources.Load<StyleSheet>("DialogGraph"));

            this.AddManipulator(new ContentDragger());
            this.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);

            AddElement(EntryNode.Create(this));

            this.RegisterCallback<MouseMoveEvent>(evt => { _lastMousePosition = evt.mousePosition; });

            AddSearchWindow(editorWindow);
            graphViewChanged = OnGraphChange;

            serializeGraphElements = Copy;
            canPasteSerializedData = CanPaste;
            unserializeAndPaste = Paste;
        }

        private void OnScrollWheel(WheelEvent evt)
        {
            if (evt.ctrlKey || evt.commandKey)
            {
                if (evt.delta.y > 0)
                    contentViewContainer.transform.scale *= 1.05f;
                else
                    contentViewContainer.transform.scale *= 0.95f;
                evt.StopPropagation();
                return;
            }

            var delta = evt.delta;
            delta.x = -delta.x;
            contentViewContainer.transform.position += delta;
            evt.StopPropagation();
        }

        private void AddSearchWindow(EditorWindow editorWindow)
        {
            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Init(this, editorWindow);
            nodeCreationRequest = context =>
            {
                _searchWindow.SetSourcePort(context.target as Port);
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition, 0, 0), _searchWindow);
            };
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach((port) => { if (startPort != port && startPort.node != port.node) compatible.Add(port); });
            return compatible;
        }

        public static Port GeneratePort(Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(int));
        }

        public Port CreatePort(Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            var port = GeneratePort(node, portDirection, capacity);

            if (portDirection == Direction.Output)
            {
                port.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0) _dragSourcePort = port;
                });
                port.RegisterCallback<MouseCaptureOutEvent>(evt =>
                {
                    if (_dragSourcePort == port)
                    {
                        schedule.Execute(() =>
                        {
                            if (_dragSourcePort != null && !_dragSourcePort.connected)
                            {
                                _searchWindow.SetSourcePort(_dragSourcePort);
                                var screenPos = this.panel?.visualTree?.worldBound.position ?? Vector2.zero;
                                SearchWindow.Open(new SearchWindowContext(
                                    screenPos + _lastMousePosition, 0, 0), _searchWindow);
                            }
                            _dragSourcePort = null;
                        }).StartingIn(50);
                    }
                });
            }

            return port;
        }

        public void CreateDialogNode(string dialogText, Vector2 position)
        {
            var node = DialogNode.Create(this, position);
            node.dialogText.value = dialogText;
            AddElement(node);
            isDirty = true;
        }

        public void CreateRetriggerNode(Vector2 position)
        {
            AddElement(RetriggerNode.Create(this, position));
            isDirty = true;
        }

        public void CreateEventNode(Vector2 position)
        {
            AddElement(EventNode.Create(this, position));
            isDirty = true;
        }

        public void CreateComparisonNode(Vector2 position)
        {
            AddElement(ComparisonNode.Create(this, position));
            isDirty = true;
        }

        public void CreateOptionNode(Vector2 position)
        {
            AddElement(OptionNode.Create(this, position));
            isDirty = true;
        }

        public void CreateResponseNode(Vector2 position)
        {
            AddElement(ResponseNode.Create(this, position));
            isDirty = true;
        }

        public void CreateExitNode(Vector2 position)
        {
            AddElement(ExitNode.Create(this, position));
            isDirty = true;
        }

        public void CreateNodeFromPort(Port sourcePort, DialogNodeBase newNode, Vector2 position)
        {
            Port outputPort = sourcePort;

            if (sourcePort.portName == "DEFAULT" && sourcePort.node is DialogNode dialogNode)
            {
                DialogNode.AddChoicePort(this, dialogNode);
                var ports = dialogNode.outputContainer.Children()
                    .Where(p => p is Port).Cast<Port>().ToList();
                outputPort = ports.LastOrDefault();
                if (outputPort == null) return;
            }

            if (outputPort.connected && outputPort.capacity != Port.Capacity.Multi)
            {
                if (outputPort.node is DialogNode dn)
                {
                    DialogNode.AddChoicePort(this, dn);
                    var ports = dn.outputContainer.Children()
                        .Where(p => p is Port).Cast<Port>().ToList();
                    var newPort = ports.LastOrDefault();
                    if (newPort == null) return;
                    outputPort = newPort;
                }
            }

            newNode.SetPosition(new Rect(position, defaultNodeSize));
            AddElement(newNode);

            var inputPort = newNode.inputContainer.Children()
                .Where(p => p is Port).Cast<Port>().FirstOrDefault();
            if (inputPort == null) return;

            var edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);

            isDirty = true;
        }

        public void CreateOptionPair(DialogNode dialogNode)
        {
            var basePos = dialogNode.GetPosition().position;

            DialogNode.AddChoicePort(this, dialogNode, "op1");
            DialogNode.AddChoicePort(this, dialogNode, "op2");

            var ports = dialogNode.outputContainer.Children()
                .Where(p => p is Port).Cast<Port>()
                .Where(p => p.portName != "DEFAULT").ToList();
            var port1 = ports.Count >= 2 ? ports[ports.Count - 2] : null;
            var port2 = ports.Count >= 1 ? ports[ports.Count - 1] : null;

            var opt1 = OptionNode.Create(this, basePos + new Vector2(350, -50));
            opt1.optionTextField.value = "op1";
            AddElement(opt1);
            ConnectPorts(port1, GetInputPort(opt1));

            var resp1 = ResponseNode.Create(this, basePos + new Vector2(550, -50));
            AddElement(resp1);
            ConnectPorts(GetDefaultPort(opt1), GetInputPort(resp1));

            var opt2 = OptionNode.Create(this, basePos + new Vector2(350, 150));
            opt2.optionTextField.value = "op2";
            AddElement(opt2);
            ConnectPorts(port2, GetInputPort(opt2));

            var resp2 = ResponseNode.Create(this, basePos + new Vector2(550, 150));
            AddElement(resp2);
            ConnectPorts(GetDefaultPort(opt2), GetInputPort(resp2));

            isDirty = true;
        }

        Port GetInputPort(Node node)
        {
            return node.inputContainer.Children()
                .Where(p => p is Port).Cast<Port>().FirstOrDefault();
        }

        Port GetDefaultPort(Node node)
        {
            return node.outputContainer.Children()
                .Where(p => p is Port).Cast<Port>()
                .FirstOrDefault(p => p.portName == "DEFAULT");
        }

        void ConnectPorts(Port from, Port to)
        {
            if (from == null || to == null) return;
            var edge = from.ConnectTo(to);
            AddElement(edge);
        }

        public static VisualElement Spacer(int size)
        {
            var s = new Label("");
            s.style.height = size;
            s.style.width = size;
            return s;
        }


        public void AddPropertyToBlackboard(ExposedProperty exposedProperty)
        {
            var localPropertyName = exposedProperty.PropertyName;
            var localPropertyValue = exposedProperty.DefaultValue;
            while (exposedProperties.Any(x => x.PropertyName == localPropertyName))
                localPropertyName = $"{localPropertyName}(1)";

            var property = new ExposedProperty();
            property.PropertyName = localPropertyName;
            property.DefaultValue = localPropertyValue;
            exposedProperties.Add(property);

            var container = new VisualElement();
            var field = new BlackboardField { text = property.PropertyName, typeText = "" };
            container.Add(field);

            var propertyValueTextField = new TextField("Default Value")
            {
                value = localPropertyValue
            };
            propertyValueTextField.RegisterValueChangedCallback(evt =>
            {
                var i = exposedProperties.FindIndex(x => x.PropertyName == property.PropertyName);
                exposedProperties[i].DefaultValue = evt.newValue;
            });

            var valueRow = new BlackboardRow(field, propertyValueTextField);
            container.Add(valueRow);

            blackboard.Add(container);
            isDirty = true;
        }

        public void ClearBlackboardAndExposedProperties()
        {
            exposedProperties.Clear();
            blackboard.Clear();
            isDirty = true;
        }

        public static void SetupPortDragTracking(Port port, DialogGraphView graph)
        {
            port.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) graph._dragSourcePort = port;
            });
            port.RegisterCallback<MouseCaptureOutEvent>(evt =>
            {
                if (graph._dragSourcePort == port)
                {
                    var releasePos = graph._lastMousePosition;
                    graph.schedule.Execute(() =>
                    {
                        if (graph._dragSourcePort != null && !graph._dragSourcePort.connected)
                        {
                            graph._searchWindow.SetSourcePort(graph._dragSourcePort);
                            var screenPos = graph._screenOrigin + releasePos;
                            SearchWindow.Open(new SearchWindowContext(
                                screenPos, 0, 0), graph._searchWindow);
                        }
                        graph._dragSourcePort = null;
                    }).StartingIn(50);
                }
            });
        }

        public static void AddDefaultPort(Node node, DialogGraphView graph = null)
        {
            var generatedPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(int));

            generatedPort.portName = "DEFAULT";

            node.outputContainer.Add(generatedPort);
            node.RefreshPorts();
            node.RefreshExpandedState();

            if (graph != null && node is DialogNodeBase)
                SetupPortDragTracking(generatedPort, graph);
        }

        public void ToggleCollapseAll()
        {
            var anyExpanded = false;
            foreach (var node in nodes.ToList())
            {
                if (node is DialogNode dn) { if (dn.expanded) anyExpanded = true; }
                else if (node is ResponseNode rn) { if (rn.expanded) anyExpanded = true; }
                else if (node is OptionNode on) { if (on.expanded) anyExpanded = true; }
            }
            foreach (var node in nodes.ToList())
            {
                if (node is DialogNode dn)
                {
                    dn.expanded = !anyExpanded;
                    dn.contentContainer.style.display = dn.expanded ? DisplayStyle.Flex : DisplayStyle.None;
                    dn.summaryLabel.style.display = dn.expanded ? DisplayStyle.None : DisplayStyle.Flex;
                    if (!dn.expanded) DialogNode.UpdateSummary(dn);
                    dn.collapseButton.text = dn.expanded ? "▼" : "►";
                    dn.RefreshExpandedState();
                }
                else if (node is ResponseNode rn)
                {
                    rn.expanded = !anyExpanded;
                    rn.contentContainer.style.display = rn.expanded ? DisplayStyle.Flex : DisplayStyle.None;
                    rn.summaryLabel.style.display = rn.expanded ? DisplayStyle.None : DisplayStyle.Flex;
                    if (!rn.expanded) ResponseNode.UpdateSummary(rn);
                    rn.collapseButton.text = rn.expanded ? "▼" : "►";
                    rn.RefreshExpandedState();
                }
                else if (node is OptionNode on)
                {
                    on.expanded = !anyExpanded;
                    on.contentContainer.style.display = on.expanded ? DisplayStyle.Flex : DisplayStyle.None;
                    on.summaryLabel.style.display = on.expanded ? DisplayStyle.None : DisplayStyle.Flex;
                    if (!on.expanded) OptionNode.UpdateSummary(on);
                    on.collapseButton.text = on.expanded ? "▼" : "►";
                    on.RefreshExpandedState();
                }
            }
            isDirty = true;
        }

        public void ResetPorts(Node node)
        {
            var ports = node.outputContainer.Children().ToList();
            ports.ForEach(p => 
            {
                if (p is Port) RemovePort(node, p as Port);
            });
        }

        public void RemovePort(Node node, Port generatedPort)
        {
            var targetEdge = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);

            if (targetEdge.Any())
            {
                var edge = targetEdge.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdge.First());
            }
            node.outputContainer.Remove(generatedPort);

            if (node.outputContainer.childCount == 0)
            {
                // add default port when all choices removed
                AddDefaultPort(node, this);
            }

            node.RefreshPorts();
            node.RefreshExpandedState();
            isDirty = true;
        }

        private GraphViewChange OnGraphChange(GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                foreach (GraphElement e in change.elementsToRemove)
                {
                    if (e is BlackboardField)
                    {
                        //actually delete the blackboard field
                        var bf = (BlackboardField)e;
                        //blackboard.Remove(bf.parent);
                        var property = exposedProperties.Find(x => x.PropertyName == bf.text);
                        exposedProperties.Remove(property);
                    }
                }
            }
            isDirty = true;
            return change;
        }

        List<GraphElement> copyElements;
        string Copy(IEnumerable<GraphElement> elements)
        {
            copyElements = elements.ToList();
            return "Copy Nodes";
        }

        bool CanPaste(string data)
        {
            return data == "Copy Nodes";
        }

        void Paste(string operation, string data)
        {
            if (copyElements == null) return;

            Dictionary<string, string> newGUIDs = new Dictionary<string, string>();

            var copiedNodes = copyElements.Where(e => e is DialogNodeBase).Cast<DialogNodeBase>().ToList();

            Vector2 offset = new Vector2(100, 100);

            List<DialogNodeBase> pastedNodes = new List<DialogNodeBase>();

            foreach (DialogNodeBase node in copiedNodes)
            {
                DialogNodeBase newNode = null;
                SerializedNode nodeData = null;
                if (node is EventNode)
                {
                    var en = node as EventNode;
                    nodeData = en.SerializeNode();
                    newNode = EventNode.Create(this, Vector2.zero);
                }
                if (node is DialogNode)
                {
                    var dn = node as DialogNode;
                    nodeData = dn.SerializeNode();
                    newNode = DialogNode.Create(this, Vector2.zero);
                }
                if (node is RetriggerNode)
                {
                    var rn = node as RetriggerNode;
                    nodeData = rn.SerializeNode();
                    newNode = RetriggerNode.Create(this, Vector2.zero);
                }
                if (node is OptionNode)
                {
                    var cn = node as OptionNode;
                    nodeData = cn.SerializeNode();
                    newNode = OptionNode.Create(this, Vector2.zero);
                }
                if (node is ResponseNode)
                {
                    var rn = node as ResponseNode;
                    nodeData = rn.SerializeNode();
                    newNode = ResponseNode.Create(this, Vector2.zero);
                }
                if (node is ExitNode)
                {
                    var en = node as ExitNode;
                    nodeData = en.SerializeNode();
                    newNode = ExitNode.Create(this, Vector2.zero);
                }

                if (newNode == null || nodeData == null) continue;

                nodeData.position += offset;
                string newGUID = Guid.NewGuid().ToString();
                newGUIDs.Add(node.GUID, newGUID);
                nodeData.GUID = newGUID;
                newNode.DeserializeNode(this, nodeData);
                pastedNodes.Add(newNode);
                AddElement(newNode);
            };

            var copiedLinks = copyElements.Where(e => e is Edge).Cast<Edge>().ToList();

            foreach(Edge link in copiedLinks) 
            {
                if (!newGUIDs.ContainsKey((link.output.node as DialogNodeBase).GUID) ||
                    !newGUIDs.ContainsKey((link.input.node as DialogNodeBase).GUID)) continue;
                var outputNode = pastedNodes.Find(n => n.GUID == newGUIDs[(link.output.node as DialogNodeBase).GUID]);
                var inputNode = pastedNodes.Find(n => n.GUID == newGUIDs[(link.input.node as DialogNodeBase).GUID]);

                var outputPort = outputNode.outputContainer.Children().ToList().Find(p => p is Port && ((Port)p).portName == link.output.portName) as Port;
                var inputPort = inputNode.inputContainer.Children().ToList().Find(p => p is Port && ((Port)p).portName == link.input.portName) as Port;

                var tempEdge = new Edge
                {
                    output = outputPort,
                    input = inputPort
                };

                tempEdge.input.Connect(tempEdge);
                tempEdge.output.Connect(tempEdge);
                AddElement(tempEdge);
            }
            isDirty = true;
        }
    }
}