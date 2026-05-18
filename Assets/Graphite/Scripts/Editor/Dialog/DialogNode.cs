using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace Graphite.Dialog
{
    // ===============================================================
    // Dialog node using unity's graphview
    // ===============================================================
    public class DialogNode : DialogNodeBase
    {
        public TextField dialogText;
        public EnumField characterField;
        public VisualElement contentContainer;
        public Button collapseButton;
        public bool expanded = true;
        public Label summaryLabel;
        private DialogGraphView _graph;

        public ObjectField voiceLine;


        public static DialogNode Create(DialogGraphView graph, Vector2 position)
        {
            var dialogNode = new DialogNode()
            {
                GUID = System.Guid.NewGuid().ToString(),
                title = "Dialog",
                _graph = graph,
            };

            dialogNode.tooltip = "Plays dialog";

            dialogNode.AddToClassList("dialog-node");
            dialogNode.titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
            dialogNode.titleContainer.style.unityTextAlign = TextAnchor.MiddleCenter;
            dialogNode.style.maxWidth = 600;
            dialogNode.style.minHeight = 200;

            AddCollapseButton(dialogNode);

            var inputPort = DialogGraphView.GeneratePort(dialogNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            dialogNode.inputContainer.Add(inputPort);

            dialogNode.contentContainer = new VisualElement();
            dialogNode.contentContainer.Add(DialogGraphView.Spacer(10));

            dialogNode.characterField = new EnumField("Character", CharacterName.Maya);
            dialogNode.characterField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
                if (dialogNode.summaryLabel != null) UpdateSummary(dialogNode);
            });
            dialogNode.contentContainer.Add(dialogNode.characterField);

            dialogNode.contentContainer.Add(DialogGraphView.Spacer(10));

            dialogNode.dialogText = new TextField(string.Empty)
            {
                multiline = true,
            };
            dialogNode.dialogText.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
                if (dialogNode.summaryLabel != null) UpdateSummary(dialogNode);
            });
            dialogNode.dialogText.style.whiteSpace = WhiteSpace.Normal;
            dialogNode.dialogText.style.height = 120;
            dialogNode.dialogText.style.minWidth = 550;
            dialogNode.contentContainer.Add(dialogNode.dialogText);

            dialogNode.contentContainer.Add(DialogGraphView.Spacer(10));

            dialogNode.mainContainer.Add(dialogNode.contentContainer);

            dialogNode.summaryLabel = new Label("");
            dialogNode.summaryLabel.style.color = Color.gray;
            dialogNode.summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            dialogNode.summaryLabel.style.paddingLeft = 5;
            dialogNode.summaryLabel.style.paddingRight = 5;
            dialogNode.summaryLabel.style.paddingTop = 3;
            dialogNode.summaryLabel.style.paddingBottom = 3;
            dialogNode.summaryLabel.style.marginTop = 5;
            dialogNode.summaryLabel.style.marginBottom = 5;
            dialogNode.summaryLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            dialogNode.summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            dialogNode.summaryLabel.style.display = DisplayStyle.None;
            dialogNode.mainContainer.Add(dialogNode.summaryLabel);

            DialogGraphView.AddDefaultPort(dialogNode, graph);

            dialogNode.RefreshExpandedState();
            dialogNode.RefreshPorts();
            dialogNode.SetPosition(new Rect(position, graph.defaultNodeSize));

            return dialogNode;
        }

        static void AddCollapseButton(DialogNode node)
        {
            node.collapseButton = new Button(() =>
            {
                node.expanded = !node.expanded;
                node.contentContainer.style.display = node.expanded ? DisplayStyle.Flex : DisplayStyle.None;
                node.summaryLabel.style.display = node.expanded ? DisplayStyle.None : DisplayStyle.Flex;
                if (!node.expanded) UpdateSummary(node);
                node.collapseButton.text = node.expanded ? "▼" : "►";
                node.RefreshExpandedState();
            });
            node.collapseButton.text = "▼";
            node.collapseButton.tooltip = "Collapse/Expand node";
            node.collapseButton.style.width = 20;
            node.collapseButton.style.height = 20;
            node.collapseButton.style.marginLeft = 5;
            node.titleContainer.Insert(0, node.collapseButton);
        }

        public static void UpdateSummary(DialogNode node)
        {
            string text = node.dialogText.value;
            string charName = node.characterField.value.ToString();
            if (string.IsNullOrEmpty(text))
                node.summaryLabel.text = $"[{charName}] [Empty]";
            else if (text.Length > 50)
                node.summaryLabel.text = $"[{charName}] {text.Substring(0, 47)}...";
            else
                node.summaryLabel.text = $"[{charName}] {text}";
        }


        public static void AddChoicePort(DialogGraphView graph, DialogNode dialogNode, string overridePortName = "", bool retriggerEnabled = true, bool isDefault = false)
        {
            var generatedPort = DialogGraphView.GeneratePort(dialogNode, Direction.Output);

            // remove default port if needed
            var defaultPort = dialogNode.outputContainer.Children().ToList().Find(p => p is Port && ((Port)p).portName == "DEFAULT") as Port;
            if (defaultPort != null)
            {
                foreach (Edge e in defaultPort.connections.ToList())
                {
                    // reassign all default connections to this new port
                    defaultPort.Disconnect(e);
                    e.output = generatedPort;
                    generatedPort.Connect(e);
                }
                dialogNode.outputContainer.Remove(defaultPort);
                graph.RemoveElement(defaultPort);
            }

            AddRetriggerToggle(generatedPort.contentContainer, retriggerEnabled);
            AddDefaultToggle(generatedPort.contentContainer, graph, dialogNode, generatedPort, isDefault);

            var oldLabel = generatedPort.contentContainer.Q<Label>("type");
            oldLabel.style.display = DisplayStyle.None;
            //generatedPort.contentContainer.Remove(oldLabel);

            // Generate unique port name to avoid collisions
            var existingNames = dialogNode.outputContainer.Children()
                .OfType<Port>()
                .Select(p => p.portName)
                .ToHashSet();

            string choicePortName;
            if (string.IsNullOrEmpty(overridePortName))
            {
                int portNum = 0;
                do
                {
                    portNum++;
                    choicePortName = $"Choice {portNum}";
                } while (existingNames.Contains(choicePortName));
            }
            else
            {
                choicePortName = overridePortName;
                if (existingNames.Contains(choicePortName))
                {
                    // Append number to make unique
                    int suffix = 1;
                    while (existingNames.Contains($"{choicePortName} {suffix}"))
                    {
                        suffix++;
                    }
                    choicePortName = $"{choicePortName} {suffix}";
                }
            }

            generatedPort.portName = choicePortName;

            var textField = new TextField
            {
                name = string.Empty,
                value = choicePortName
            };
            textField.RegisterValueChangedCallback(evt => { generatedPort.portName = evt.newValue; if (evt.newValue != evt.previousValue) graph.isDirty = true; });
            textField.style.maxWidth = 120;
            dialogNode.dialogText.style.whiteSpace = WhiteSpace.Normal;
            generatedPort.contentContainer.Add(new Label("  "));
            generatedPort.contentContainer.Add(textField);

            var deleteButton = new Button(() => graph.RemovePort(dialogNode, generatedPort)) { text = "X" };
            generatedPort.contentContainer.Add(deleteButton);

            generatedPort.portName = choicePortName;
            DialogGraphView.SetupPortDragTracking(generatedPort, graph);
            dialogNode.outputContainer.Add(generatedPort);
            dialogNode.RefreshPorts();
            dialogNode.RefreshExpandedState();
            graph.isDirty = true;
        }


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Add Option", action =>
            {
                if (_graph != null) _graph.CreateOptionPair(this);
            });
        }

        readonly static System.Text.RegularExpressions.Regex regex_removeEvents = new System.Text.RegularExpressions.Regex(@"\n|\[([^]]*)\]", System.Text.RegularExpressions.RegexOptions.Compiled);
        readonly static System.Text.RegularExpressions.Regex regex_removeWhitespace = new System.Text.RegularExpressions.Regex(@"\s+", System.Text.RegularExpressions.RegexOptions.Compiled);
        static string GetNodeTitle(string text)
        {
            text = regex_removeEvents.Replace(text, "");
            text = regex_removeWhitespace.Replace(text, "");
            if (text.Length < 24) return text;
            else return text.Substring(0, 24);
        }


        private int GetDefaultPort()
        {
            var ports = outputContainer.Children().Where(p => p is Port).ToList();
            for(int p = 0; p < ports.Count; p++)
            {
                var toggle = GetDefaultToggle(ports[p]);
                if(toggle != null && toggle.Value) return p;
            }
            return -1;
        }


        public override SerializedNode SerializeNode()
        {
            return new SerializedDialogNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                defaultPort = GetDefaultPort(),
                dialogText = dialogText.value,
                character = (CharacterName)characterField.value,
            };
        }


        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedDialogNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            if (d.ports != null)
            {
                for(int p = 0; p < d.ports.Count; p++)
                {
                    if (d.ports[p].portName != "DEFAULT")
                        AddChoicePort(graph, this, d.ports[p].portName, d.ports[p].retriggerEnabled, p == d.defaultPort && d.defaultPort < d.ports.Count);
                }
            }
            dialogText.value = d.dialogText;
            characterField.value = d.character;

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}