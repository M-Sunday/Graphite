using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Graphite.Dialog
{
    // ===============================================================
    // Container for a dialoggraph asset
    // ===============================================================
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Dialog Graph", menuName = "Graphite/Dialog Window", order = 0)]
    public class DialogGraphContainer : ScriptableObject
    {
        public List<NodeLinkData> nodeLinks = new List<NodeLinkData>();
        [SerializeReference]
        public List<SerializedNode> nodeData = new List<SerializedNode>();
        public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();
    }

    [System.Serializable]
    public class NodeLinkData
    {
        public string baseNodeGuid;
        public string portName;
        public string targetNodeGuid;
    }


    [System.Serializable]
    public class SerializedPort
    {
        public string portName;
        public bool retriggerEnabled;

        public SerializedPort(string portName, bool retriggerEnabled)
        {
            this.portName = portName;
            this.retriggerEnabled = retriggerEnabled;
        }
    }

    [System.Serializable]
    public class SerializedNode
    {
        public string GUID;
        public List<SerializedPort> ports;
        public Vector2 position;
    }


    [System.Serializable]
    public class SerializedDialogNode : SerializedNode
    {
        public string dialogText;
        public CharacterName character = CharacterName.Maya;
        public int defaultPort = -1;
    }

    [System.Serializable]
    public class SerializedRetriggerNode : SerializedNode
    {
        public int outputCount;
    }

    [System.Serializable]
    public class SerializedEventNode : SerializedNode
    {
        public string eventKey;
        public string eventBody;

        public string eventString => eventKey + eventBody;
    }

    [System.Serializable]
    public class SerializedOptionNode : SerializedNode
    {
        public string optionText;
    }

    [System.Serializable]
    public class SerializedResponseNode : SerializedNode
    {
        public string responseText;
        public CharacterName character = CharacterName.Maya;
    }

    [System.Serializable]
    public class SerializedExitNode : SerializedNode
    {
    }

    [System.Serializable]
    public class SerializedComparisonNode : SerializedNode
    {
        public string propertyName;

        public enum PropertyType
        {
            String,
            Bool,
            Int,
            Float
        }

        public enum Comparison
        {
            Equals,
            Greater,
            Less,
        }

        public PropertyType type;
        public Comparison comparison;

        public string comparator;
    }

    [System.Serializable]
    public class ExposedProperty
    {
        public string PropertyName = "New String";
        [FormerlySerializedAs("PropertyValue")] public string DefaultValue = "New Value";
    }

    // ===============================================================
    // Inline dialog events used by the graph editor
    // ===============================================================
    public class InlineDialogEvent
    {
        public string key;
        public string description;
        public bool pauseDialog = false;
        public delegate void InlineEvent(MonoBehaviour context, string param);
        public delegate float InlineTime(string param);
        public InlineEvent output;
        public InlineTime delayTime = (string param) => 0;
    }

    public static class DialogEventDB
    {
        public static readonly InlineDialogEvent[] inlineDialogEvents = new InlineDialogEvent[]
        {
            new InlineDialogEvent()
            {
                key = "_",
                description = "pause denoted by number of underscores",
                pauseDialog = true,
                delayTime = (string param) => param.ToCharArray().ToList().FindAll(c => c == '_').Count * 0.2f + 0.2f
            },

            new InlineDialogEvent()
            {
                key = "custom:",
                description = "custom external event. Requires DialogEventRelay on context behaviour",
                pauseDialog = false,
            },
        };

        public static bool ParseEvent(string eventString, out InlineDialogEvent foundEvent, out string eventParams)
        {
            foundEvent = Array.Find(inlineDialogEvents, e => eventString.StartsWith(e.key));
            if (foundEvent != null)
            {
                eventString = eventString.Remove(0, foundEvent.key.Length);
                eventParams = eventString;
                return true;
            }
            else
            {
                eventParams = null;
                return false;
            }
        }
    }
}