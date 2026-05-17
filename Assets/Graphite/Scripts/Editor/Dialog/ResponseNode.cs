using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Graphite.Dialog
{
    public class ResponseNode : DialogNodeBase
    {
        public TextField responseTextField;
        public EnumField characterField;
        public VisualElement contentContainer;
        public Button collapseButton;
        public bool expanded = true;
        public Label summaryLabel;

        public static ResponseNode Create(DialogGraphView graph, Vector2 position)
        {
            var node = new ResponseNode()
            {
                title = "Response",
                GUID = System.Guid.NewGuid().ToString()
            };

            node.tooltip = "Character dialogue in response to an option";
            node.AddToClassList("response-node");

            node.titleContainer.style.backgroundColor = new Color(0.6f, 0.2f, 0.8f);

            AddCollapseButton(node);

            var inputPort = DialogGraphView.GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            inputPort.portColor = new Color(0.6f, 0.3f, 0.9f);
            node.inputContainer.Add(inputPort);

            DialogGraphView.AddDefaultPort(node, graph);

            node.contentContainer = new VisualElement();
            node.contentContainer.Add(DialogGraphView.Spacer(6));

            node.characterField = new EnumField("Character", CharacterName.Maya);
            node.characterField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
                if (node.summaryLabel != null) UpdateSummary(node);
            });
            node.contentContainer.Add(node.characterField);

            node.responseTextField = new TextField("Text");
            node.responseTextField.multiline = true;
            node.responseTextField.style.height = 120;
            node.responseTextField.style.minWidth = 450;
            node.responseTextField.style.whiteSpace = WhiteSpace.Normal;
            node.responseTextField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
                if (node.summaryLabel != null) UpdateSummary(node);
            });
            node.contentContainer.Add(node.responseTextField);

            node.contentContainer.Add(DialogGraphView.Spacer(6));

            node.mainContainer.Add(node.contentContainer);

            node.summaryLabel = new Label("");
            node.summaryLabel.style.color = Color.gray;
            node.summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            node.summaryLabel.style.paddingLeft = 5;
            node.summaryLabel.style.paddingRight = 5;
            node.summaryLabel.style.paddingTop = 3;
            node.summaryLabel.style.paddingBottom = 3;
            node.summaryLabel.style.marginTop = 5;
            node.summaryLabel.style.marginBottom = 5;
            node.summaryLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            node.summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            node.summaryLabel.style.display = DisplayStyle.None;
            node.mainContainer.Add(node.summaryLabel);

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(position, graph.defaultNodeSize));

            return node;
        }

        static void AddCollapseButton(ResponseNode node)
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

        static void UpdateSummary(ResponseNode node)
        {
            string text = node.responseTextField.value;
            string charName = node.characterField.value.ToString();
            if (string.IsNullOrEmpty(text))
                node.summaryLabel.text = $"[{charName}] [Empty]";
            else if (text.Length > 50)
                node.summaryLabel.text = $"[{charName}] {text.Substring(0, 47)}...";
            else
                node.summaryLabel.text = $"[{charName}] {text}";
        }

        public override SerializedNode SerializeNode()
        {
            return new SerializedResponseNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                responseText = this.responseTextField.value,
                character = (CharacterName)this.characterField.value,
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedResponseNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            responseTextField.value = d.responseText;
            characterField.value = d.character;

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
