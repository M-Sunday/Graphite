using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace PQHD.Dialog
{
    // ===============================================================
    // Dialog node using unity's graphview
    // ===============================================================
    public class DialogNode : DialogNodeBase
    {
        public TextField dialogText;

        public ObjectField voiceLine;


        public static DialogNode Create(DialogGraphView graph, Vector2 position)
        {
            var dialogNode = new DialogNode()
            {
                title = "Dialog",
                GUID = System.Guid.NewGuid().ToString(),
            };

            dialogNode.tooltip = "Plays dialog";

            dialogNode.AddToClassList("dialog-node");
            dialogNode.titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
            dialogNode.style.flexDirection = FlexDirection.Row;
            dialogNode.style.maxWidth = 300;
            //dialogNode.style.flexGrow = 1;

            var inputPort = DialogGraphView.GeneratePort(dialogNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            dialogNode.inputContainer.Add(inputPort);

            dialogNode.mainContainer.Add(DialogGraphView.Spacer(10));

            dialogNode.mainContainer.Add(DialogGraphView.Spacer(10));


            dialogNode.dialogText = new TextField(string.Empty)
            {
                multiline = true,
            };
            dialogNode.dialogText.RegisterValueChangedCallback(evt =>
            {
                dialogNode.title = GetNodeTitle(evt.newValue);
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            //dialogNode.dialogText.SetValueWithoutNotify(dialogNode.dialogText);
            dialogNode.dialogText.style.whiteSpace = WhiteSpace.Normal;
            dialogNode.mainContainer.Add(dialogNode.dialogText);

            dialogNode.mainContainer.Add(DialogGraphView.Spacer(10));

            var addOptionsButton = new Button(() => graph.CreateOptionPair(dialogNode)) { text = "Add Options" };
            addOptionsButton.style.width = 100;
            dialogNode.mainContainer.Add(addOptionsButton);

            DialogGraphView.AddDefaultPort(dialogNode, graph);

            dialogNode.RefreshExpandedState();
            dialogNode.RefreshPorts();
            dialogNode.SetPosition(new Rect(position, graph.defaultNodeSize));

            return dialogNode;
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

                dialogNode.titleContainer.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f);
            }

            AddRetriggerToggle(generatedPort.contentContainer, retriggerEnabled);
            AddDefaultToggle(generatedPort.contentContainer, graph, dialogNode, generatedPort, isDefault);

            var oldLabel = generatedPort.contentContainer.Q<Label>("type");
            oldLabel.style.display = DisplayStyle.None;
            //generatedPort.contentContainer.Remove(oldLabel);

            var outputPortCount = dialogNode.outputContainer.Query("connector").ToList().Count;
            generatedPort.portName = $"Choice {outputPortCount}";

            var choicePortName = string.IsNullOrEmpty(overridePortName) ? $"Choice {outputPortCount}" : overridePortName;

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


        readonly static System.Text.RegularExpressions.Regex regex_removeEvents = new System.Text.RegularExpressions.Regex(@"\n|\[([^]]*)\]");
        readonly static System.Text.RegularExpressions.Regex regex_removeWhitespace = new System.Text.RegularExpressions.Regex(@"/\S.*\S/");
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
            };
        }


        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedDialogNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            for(int p = 0; p < d.ports.Count; p++)
            {
                if (d.ports[p].portName != "DEFAULT") AddChoicePort(graph, this, d.ports[p].portName, d.ports[p].retriggerEnabled, p == d.defaultPort); 
            }
            d.ports.ForEach(p => { });
            dialogText.value = d.dialogText;

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}