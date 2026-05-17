using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Graphite.Dialog
{
    // ===============================================================
    // Marks graph entry point
    // ===============================================================
    public class EntryNode : DialogNodeBase
    {
        public static EntryNode Create(DialogGraphView graph)
        {
            var node = new EntryNode
            {
                title = "START",
                GUID = "ENTRYPOINT"
            };

            node.AddToClassList("entry-node");

            DialogGraphView.AddDefaultPort(node, graph);

            node.capabilities &= ~Capabilities.Deletable; // make non-deletable
            node.capabilities &= ~Capabilities.Movable; // make non-movable

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(100, 200, 100, 150));
            graph.isDirty = true;
            return node;
        }

        // do not serialize entry node, it is static
        public override SerializedNode SerializeNode()
        {
            return null;
        }

        public override void DeserializeNode(DialogGraphView graph, SerializedNode data)
        {
            
        }
    }
}