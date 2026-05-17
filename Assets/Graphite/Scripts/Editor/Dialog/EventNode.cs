using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Graphite.Dialog
{
    // ===============================================================
    // Node that triggers an event instantaneously
    // ===============================================================
    public class EventNode : DialogNodeBase
    {

        public PopupField<string> eventPopup;
        public TextField eventText;

        public static EventNode Create(DialogGraphView graph, Vector2 position)
        {
            var eventNode = new EventNode()
            {
                name = "Event",
                title = "Event",
                GUID = System.Guid.NewGuid().ToString()
            };

            eventNode.tooltip = "Triggers an event instantaneously";

            eventNode.AddToClassList("event-node");

            var inputPort = DialogGraphView.GeneratePort(eventNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            eventNode.inputContainer.Add(inputPort);

            DialogGraphView.AddDefaultPort(eventNode, graph);

            eventNode.mainContainer.Add(DialogGraphView.Spacer(10));

            eventNode.mainContainer.Add(DialogGraphView.Spacer(10));

            VisualElement bodyElement = new VisualElement();
            bodyElement.style.flexDirection = FlexDirection.Row;

            List<string> eventKeys = new List<string>();
            DialogEventDB.inlineDialogEvents.ToList().ForEach(e => eventKeys.Add(e.key));
            eventNode.eventPopup = new PopupField<string>(eventKeys, 0);
            eventNode.eventPopup.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            bodyElement.Add(eventNode.eventPopup);

            eventNode.eventText = new TextField();
            eventNode.eventText.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            eventNode.eventText.style.flexGrow = 1;
            bodyElement.Add(eventNode.eventText);

            eventNode.mainContainer.Add(bodyElement);

            eventNode.RefreshExpandedState();
            eventNode.RefreshPorts();
            eventNode.SetPosition(new Rect(position, graph.defaultNodeSize));

            return eventNode;
        }

        public override SerializedNode SerializeNode()
        {
            return new SerializedEventNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                eventKey = this.eventPopup.value,
                eventBody = this.eventText.value
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedEventNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            eventPopup.value = d.eventKey;
            eventText.value = d.eventBody;

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}