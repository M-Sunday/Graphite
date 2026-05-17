using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Graphite.Dialog
{
    public class OptionNode : DialogNodeBase
    {
        public TextField optionTextField;
        public VisualElement contentContainer;
        public Button collapseButton;
        public bool expanded = true;
        public Label summaryLabel;

        public static OptionNode Create(DialogGraphView graph, Vector2 position)
        {
            var node = new OptionNode()
            {
                title = "Option",
                GUID = System.Guid.NewGuid().ToString()
            };

            node.tooltip = "A dialogue option the player can select";
            node.AddToClassList("choice-node");

            node.titleContainer.style.backgroundColor = new Color(0.9f, 0.5f, 0.1f);

            AddCollapseButton(node);

            var inputPort = DialogGraphView.GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            inputPort.portColor = new Color(1f, 0.6f, 0.2f);
            node.inputContainer.Add(inputPort);

            DialogGraphView.AddDefaultPort(node, graph);

            node.contentContainer = new VisualElement();
            node.contentContainer.Add(DialogGraphView.Spacer(6));

            node.optionTextField = new TextField("Text");
            node.optionTextField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
                if (node.summaryLabel != null) UpdateSummary(node);
            });
            node.optionTextField.style.height = 60;
            node.optionTextField.style.minWidth = 250;
            node.contentContainer.Add(node.optionTextField);

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

        static void AddCollapseButton(OptionNode node)
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

        static void UpdateSummary(OptionNode node)
        {
            string text = node.optionTextField.value;
            if (string.IsNullOrEmpty(text))
                node.summaryLabel.text = "[Empty]";
            else if (text.Length > 50)
                node.summaryLabel.text = "→ " + text.Substring(0, 47) + "...";
            else
                node.summaryLabel.text = "→ " + text;
        }

        public override SerializedNode SerializeNode()
        {
            return new SerializedOptionNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                optionText = this.optionTextField.value,
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedOptionNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            optionTextField.value = d.optionText;

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
