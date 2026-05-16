using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace PQHD.Dialog
{
    // ===============================================================
    // Node that runs a different output based on how many times this graph has been run
    // ===============================================================
    public class RetriggerNode : DialogNodeBase
    {
        public int outputCount;


        public static RetriggerNode Create(DialogGraphView graph, Vector2 position)
        {
            var retriggerNode = new RetriggerNode()
            {
                name = "Retrigger",
                title = "Retrigger",
                GUID = System.Guid.NewGuid().ToString(),
                outputCount = 0
            };

            retriggerNode.tooltip = "Switches output based on how many times the graph has been run";

            retriggerNode.AddToClassList("retrigger-node");

            //retriggerNode.style.flexDirection = FlexDirection.Row;
            //retriggerNode.style.maxWidth = 300;
            //dialogNode.style.flexGrow = 1;

            var inputPort = DialogGraphView.GeneratePort(retriggerNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            retriggerNode.inputContainer.Add(inputPort);


            var removeButton = new Button(() => 
            {
                if (retriggerNode.outputCount > 0)
                {
                    Port port = retriggerNode.outputContainer.ElementAt(retriggerNode.outputContainer.childCount - 2) as Port;
                    if (port != null)
                    {
                        graph.RemovePort(retriggerNode, port);
                        retriggerNode.outputCount--;
                    }
                }
            });
            removeButton.text = "-";
            retriggerNode.titleContainer.Add(removeButton);

            var addButton = new Button(() => { AddChoicePort(graph, retriggerNode); });
            addButton.text = "+";
            retriggerNode.titleContainer.Add(addButton);

            DialogGraphView.AddDefaultPort(retriggerNode, graph);

            retriggerNode.RefreshExpandedState();
            retriggerNode.RefreshPorts();
            retriggerNode.SetPosition(new Rect(position, graph.defaultNodeSize));

            return retriggerNode;
        }

        public static void AddChoicePort(DialogGraphView graph, RetriggerNode node)
        {
            node.outputCount++;
            var generatedPort = DialogGraphView.GeneratePort(node, Direction.Output);

            generatedPort.portName = node.outputCount.ToString(CultureInfo.InvariantCulture);
            node.outputContainer.Add(generatedPort);
            Port defaultPort = node.outputContainer.Children().ToList().Find(v => v is Port && ((Port)v).portName == "DEFAULT") as Port;
            if (defaultPort != null) defaultPort.BringToFront();

            node.RefreshPorts();
            node.RefreshExpandedState();
            graph.isDirty = true;
        }

        public override SerializedNode SerializeNode()
        {
            return new SerializedRetriggerNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                outputCount = this.outputCount
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedRetriggerNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            outputCount = 0;
            for (int i = 0; i < d.outputCount; i++)
            {
                AddChoicePort(graph, this);
            }

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}