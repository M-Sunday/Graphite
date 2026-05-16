using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace PQHD.Dialog
{
    // ===============================================================
    // Class to serialize the data in a dialog graph
    // ===============================================================
    public class GraphSaveUtility
    {
        private DialogGraphView _targetGraphView;

        private List<Edge> edges => _targetGraphView.edges.ToList();
        private List<DialogNodeBase> nodes => _targetGraphView.nodes.ToList().Cast<DialogNodeBase>().ToList();

        public static GraphSaveUtility GetInstance(DialogGraphView targetGraphView)
        {
            return new GraphSaveUtility
            {
                _targetGraphView = targetGraphView
            };
        }

        public void SaveGraph(DialogGraphContainer target)
        {
            // if (!edges.Any()) return;
            if (target == null) return;

            target.nodeLinks = new List<NodeLinkData>();
            target.nodeData = new List<SerializedNode>();
            target.exposedProperties = new List<ExposedProperty>();

            var connectedPorts = edges.Where(x => x.input.node != null).ToArray();
            for (int i = 0; i < connectedPorts.Length; i++)
            {
                var outputNode = connectedPorts[i].output.node as DialogNodeBase;
                var inputNode = connectedPorts[i].input.node as DialogNodeBase;

                target.nodeLinks.Add(new NodeLinkData
                {
                    baseNodeGuid = outputNode.GUID,
                    portName = connectedPorts[i].output.portName,
                    targetNodeGuid = inputNode.GUID
                });
            }

            foreach (var node in nodes)
            {
                var data = node.SerializeNode();
                if(data != null) target.nodeData.Add(data);
            }

            // add start node to beginning
            target.nodeData.Insert(0, new SerializedNode() { GUID = "ENTRYPOINT", ports = new List<SerializedPort>() { new SerializedPort("DEFAULT", true) } });

            target.exposedProperties.AddRange(_targetGraphView.exposedProperties);

            EditorUtility.SetDirty(target);
        }

        public void LoadGraph(DialogGraphContainer container)
        {
            if (container == null) return;

            ClearGraph(container);
            CreateNodes(container);
            ConnectNodes(container);
            CreateExposedProperties(container);
        }

        private void ClearGraph(DialogGraphContainer container)
        {
            foreach (var node in nodes)
            {
                if (node is EntryNode) continue;
                edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));

                _targetGraphView.RemoveElement(node);
            }
        }

        private void CreateNodes(DialogGraphContainer container)
        {
            foreach (var nodeData in container.nodeData)
            {
                DialogNodeBase node = null;
                if (nodeData is SerializedDialogNode)
                {
                    node = DialogNode.Create(_targetGraphView, nodeData.position);
                    ((DialogNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedRetriggerNode)
                {
                    node = RetriggerNode.Create(_targetGraphView, nodeData.position);
                    ((RetriggerNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedEventNode)
                {
                    node = EventNode.Create(_targetGraphView, nodeData.position);
                    ((EventNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedComparisonNode)
                {
                    node = ComparisonNode.Create(_targetGraphView, nodeData.position);
                    ((ComparisonNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedOptionNode)
                {
                    node = OptionNode.Create(_targetGraphView, nodeData.position);
                    ((OptionNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedResponseNode)
                {
                    node = ResponseNode.Create(_targetGraphView, nodeData.position);
                    ((ResponseNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData is SerializedExitNode)
                {
                    node = ExitNode.Create(_targetGraphView, nodeData.position);
                    ((ExitNode)node).DeserializeNode(_targetGraphView, nodeData);
                }

                if (nodeData == null || node == null) continue;

                _targetGraphView.AddElement(node);
            }
        }

        private void ConnectNodes(DialogGraphContainer container)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var connections = container.nodeLinks.Where(x => x.baseNodeGuid == nodes[i].GUID).ToList();
                for (int c = 0; c < connections.Count; c++)
                {
                    var targetNodeGuid = connections[c].targetNodeGuid;
                    var targetNode = nodes.First(x => x.GUID == targetNodeGuid);
                    LinkNodes((Port)nodes[i].outputContainer.Children().ToList().Find(p => p is Port && ((Port)p).portName == connections[c].portName), (Port)targetNode.inputContainer[0]);

                    targetNode.SetPosition(new Rect(container.nodeData.First(x => x != null && x.GUID == targetNode.GUID).position, _targetGraphView.defaultNodeSize));
                }
            }
        }

        private void LinkNodes(Port output, Port input)
        {
            if(output == null || input == null)
            {
                // failed to find connecting ports
                return;
            }

            var tempEdge = new Edge
            {
                output = output,
                input = input
            };

            tempEdge.input.Connect(tempEdge);
            tempEdge.output.Connect(tempEdge);
            _targetGraphView.Add(tempEdge);
        }

        private void CreateExposedProperties(DialogGraphContainer container)
        {
            _targetGraphView.ClearBlackboardAndExposedProperties();
            foreach (var exposedProperty in container.exposedProperties)
            {
                _targetGraphView.AddPropertyToBlackboard(exposedProperty);
            }
        }
    }
}