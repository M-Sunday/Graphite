using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;
using System.Linq;
using System.Reflection;

namespace Graphite.Dialog
{
    // ===============================================================
    // Checks the value of a property
    // ===============================================================
    public class ComparisonNode : DialogNodeBase
    {


        public PopupField<string> propertyField;
        public EnumField typeField;
        public EnumField comparisonField;
        public TextField comparatorField;
        private Label stringEqualsLabel;

        private bool showComparisonOptions => ((SerializedComparisonNode.PropertyType)typeField.value) is SerializedComparisonNode.PropertyType.Float or SerializedComparisonNode.PropertyType.Int;

        public static ComparisonNode Create(DialogGraphView graph, Vector2 position)
        {
            var comparisonNode = new ComparisonNode()
            {
                name = "Comparison",
                title = "Comparison",
                GUID = System.Guid.NewGuid().ToString()
            };

            comparisonNode.tooltip = "Compares the value of a property";

            comparisonNode.AddToClassList("comparison-node");

            var inputPort = DialogGraphView.GeneratePort(comparisonNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            comparisonNode.inputContainer.Add(inputPort);

            Port truePort = DialogGraphView.GeneratePort(comparisonNode, Direction.Output, Port.Capacity.Single);
            truePort.portName = "True";
            comparisonNode.outputContainer.Add(truePort);
            Port falsePort = DialogGraphView.GeneratePort(comparisonNode, Direction.Output, Port.Capacity.Single);
            falsePort.portName = "False";
            comparisonNode.outputContainer.Add(falsePort);


            comparisonNode.mainContainer.Add(DialogGraphView.Spacer(10));

            VisualElement bodyElement = new VisualElement();
            bodyElement.style.flexDirection = FlexDirection.Column;

            List<string> propKeys = graph.exposedProperties.Select(e => e.PropertyName).ToList();
            if (propKeys.Count == 0)
                propKeys.Add("No Properties Defined");
            comparisonNode.propertyField = new PopupField<string>(propKeys, 0);
            comparisonNode.propertyField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            bodyElement.Add(comparisonNode.propertyField);

            comparisonNode.typeField = new EnumField(SerializedComparisonNode.PropertyType.String);
            comparisonNode.typeField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue)
                { 
                    graph.isDirty = true;
                    comparisonNode.UpdateComparisonVisibility();
                }
            });
            bodyElement.Add(comparisonNode.typeField);

            comparisonNode.comparisonField = new EnumField(SerializedComparisonNode.Comparison.Equals);
            comparisonNode.comparisonField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            bodyElement.Add(comparisonNode.comparisonField);

            comparisonNode.stringEqualsLabel = new Label("Equals");
            bodyElement.Add(comparisonNode.stringEqualsLabel);

            comparisonNode.comparatorField = new TextField("Value");
            comparisonNode.comparatorField.Remove(comparisonNode.comparatorField.labelElement);
            comparisonNode.comparatorField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != evt.previousValue) graph.isDirty = true;
            });
            bodyElement.Add(comparisonNode.comparatorField);

            comparisonNode.UpdateComparisonVisibility();

            comparisonNode.mainContainer.Add(bodyElement);

            comparisonNode.RefreshExpandedState();
            comparisonNode.RefreshPorts();
            comparisonNode.SetPosition(new Rect(position, graph.defaultNodeSize));

            return comparisonNode;
        }

        private void UpdateComparisonVisibility()
        {
            bool showComparisonOptions = ((SerializedComparisonNode.PropertyType)typeField.value) is SerializedComparisonNode.PropertyType.Float or SerializedComparisonNode.PropertyType.Int;
            comparisonField.style.display = showComparisonOptions ? DisplayStyle.Flex : DisplayStyle.None;
            stringEqualsLabel.style.display = showComparisonOptions ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public override SerializedNode SerializeNode()
        {
            return new SerializedComparisonNode()
            {
                GUID = this.GUID,
                position = this.GetPosition().position,
                ports = SerializePorts(),
                propertyName = this.propertyField.value,
                type = (SerializedComparisonNode.PropertyType)typeField.value,
                comparison = (SerializedComparisonNode.Comparison)comparisonField.value,
                comparator = comparatorField.value,
            };
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            var d = data as SerializedComparisonNode;
            graph.ResetPorts(this);
            GUID = d.GUID;
            SetPosition(new Rect(d.position, Vector2.zero));
            propertyField.value = d.propertyName;
            typeField.value = d.type;
            comparisonField.value = d.comparison;
            comparatorField.value = d.comparator;

            // Recreate True and False ports
            var truePort = DialogGraphView.GeneratePort(this, Direction.Output, Port.Capacity.Single);
            truePort.portName = "True";
            outputContainer.Add(truePort);

            var falsePort = DialogGraphView.GeneratePort(this, Direction.Output, Port.Capacity.Single);
            falsePort.portName = "False";
            outputContainer.Add(falsePort);

            RefreshPorts();
            RefreshExpandedState();
            UpdateComparisonVisibility();
        }
    }
}