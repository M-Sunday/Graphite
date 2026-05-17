using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Graphite.Dialog
{
    public class ExitNode : DialogNodeBase
    {
        public VisualElement contentContainer;
        public Button collapseButton;
        public bool expanded = true;
        public Label summaryLabel;

        public static ExitNode Create(DialogGraphView graph, Vector2 position)
        {
            var node = new ExitNode()
            {
                title = "EXIT",
                GUID = System.Guid.NewGuid().ToString()
            };

            node.tooltip = "End of conversation";
            node.AddToClassList("exit-node");

            node.titleContainer.style.backgroundColor = new Color(0.7f, 0.1f, 0.1f);

            AddCollapseButton(node);

            var inputPort = DialogGraphView.GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            inputPort.portColor = Color.red;
            node.inputContainer.Add(inputPort);

            node.contentContainer = new VisualElement();

            var label = new Label("End of conversation");
            label.style.color = Color.red;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 10;
            label.style.marginBottom = 10;
            label.style.whiteSpace = WhiteSpace.Normal;
            node.contentContainer.Add(label);

            node.mainContainer.Add(node.contentContainer);

            node.summaryLabel = new Label("[Exit Node]");
            node.summaryLabel.style.color = new Color(1f, 0.5f, 0.5f);
            node.summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            node.summaryLabel.style.paddingLeft = 5;
            node.summaryLabel.style.paddingRight = 5;
            node.summaryLabel.style.paddingTop = 3;
            node.summaryLabel.style.paddingBottom = 3;
            node.summaryLabel.style.marginTop = 5;
            node.summaryLabel.style.marginBottom = 5;
            node.summaryLabel.style.backgroundColor = new Color(0.3f, 0.1f, 0.1f, 0.3f);
            node.summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            node.summaryLabel.style.display = DisplayStyle.None;
            node.mainContainer.Add(node.summaryLabel);

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(position, graph.defaultNodeSize));

            return node;
        }

        static void AddCollapseButton(ExitNode node)
        {
            node.collapseButton = new Button(() =>
            {
                node.expanded = !node.expanded;
                node.contentContainer.style.display = node.expanded ? DisplayStyle.Flex : DisplayStyle.None;
                node.summaryLabel.style.display = node.expanded ? DisplayStyle.None : DisplayStyle.Flex;
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

        public override SerializedNode SerializeNode()
        {
            return new SerializedExitNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedExitNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
