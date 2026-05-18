using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

namespace Graphite.Dialog
{
    // ===============================================================
    // Base node using unity's graphview
    // ===============================================================
    public abstract class DialogNodeBase : Node
    {
        public string GUID;

        public class ToggleButton : Button
        {
            private bool _value;
            public bool Value
            {
                get => _value;
                set
                {
                    if(_value != value)
                    {
                        _value = value;
                        UpdateIcon();
                        onValueChanged?.Invoke(value);
                    }
                }
            }

            public void SetValueWithoutNotify(bool value)
            {
                _value = value;
                UpdateIcon();
            }

            Image im_on;
            Image im_off;
            public Action<bool> onValueChanged;
            public void Init(Texture tex_off, Texture tex_on, Color color_off, Color color_on, bool defaultValue)
            {
                _value = defaultValue;
                clicked += Toggle;

                im_on = new Image() { image = tex_on, tintColor = color_on };
                im_off = new Image() { image = tex_off, tintColor = color_off };

                contentContainer.Add(im_on);
                contentContainer.Add(im_off);

                UpdateIcon();
            }

            void Toggle()
            {
                Value = !Value;
            }

            void UpdateIcon()
            {
                im_on.style.display = Value ? DisplayStyle.Flex : DisplayStyle.None;
                im_off.style.display = Value ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private static Texture tex_retrigger_on
        {
            get
            {
                if (m_tex_retrigger_on == null) m_tex_retrigger_on = Resources.Load("retrigger_on", typeof(Texture)) as Texture;
                return m_tex_retrigger_on;
            }
        }
        private static Texture tex_retrigger_off
        {
            get
            {
                if (m_tex_retrigger_off == null) m_tex_retrigger_off = Resources.Load("retrigger_off", typeof(Texture)) as Texture;
                return m_tex_retrigger_off;
            }
        }
        private static Texture m_tex_retrigger_on;
        private static Texture m_tex_retrigger_off;

        private static Texture tex_default_on
        {
            get
            {
                if (m_tex_default_on == null) m_tex_default_on = Resources.Load("default_on", typeof(Texture)) as Texture;
                return m_tex_default_on;
            }
        }
        private static Texture tex_default_off
        {
            get
            {
                if (m_tex_default_off == null) m_tex_default_off = Resources.Load("default_off", typeof(Texture)) as Texture;
                return m_tex_default_off;
            }
        }
        private static Texture m_tex_default_on;
        private static Texture m_tex_default_off;

        protected static void AddRetriggerToggle(VisualElement container, bool value)
        {
            var retriggerToggle = new ToggleButton() { name = "retrigger" };
            retriggerToggle.Init(tex_retrigger_off, tex_retrigger_on, new Color(1, 1, 1, 0.2f), Color.white, value);
            retriggerToggle.style.maxWidth = 32;
            retriggerToggle.tooltip = "Can this choice be selected repeatedly in sequential evaluations";
            container.Add(retriggerToggle);
        }

        protected static void AddDefaultToggle(VisualElement container, DialogGraphView graph, DialogNodeBase node, Port port, bool value)
        {
            var defaultToggle = new ToggleButton() { name = "default" };
            defaultToggle.Init(tex_default_off, tex_default_on, new Color(1, 1, 1, 0.2f), Color.white, value);
            defaultToggle.style.maxWidth = 32;
            defaultToggle.tooltip = "Mark as default port";
            container.Add(defaultToggle);
            defaultToggle.onValueChanged += val => 
            {
                if(val) node.EnforceDefaultPort(port);
                graph.isDirty = true;
            };
        }

        void EnforceDefaultPort(Port defaultPort)
        {
            var ports = outputContainer.Children().Where(p => p is Port).ToList();
            for(int p = 0; p < ports.Count; p++)
            {
                if(ports[p] == defaultPort) continue;
                var toggle = GetDefaultToggle(ports[p]);
                if(toggle != null) toggle.SetValueWithoutNotify(false);
            }
        }

        protected static ToggleButton GetRetriggerToggle(VisualElement element)
        {
            return element.Q<ToggleButton>("retrigger");
        }

        protected static ToggleButton GetDefaultToggle(VisualElement element)
        {
            return element.Q<ToggleButton>("default");
        }

        public abstract SerializedNode SerializeNode();
        public abstract void DeserializeNode(DialogGraphView graph, SerializedNode data);

        protected List<SerializedPort> SerializePorts()
        {
            var ports = new List<SerializedPort>();
            outputContainer.Children().ToList().ForEach(p => 
            {
                if (p is Port port && port.portName != "DEFAULT")
                {
                    var retrigger = GetRetriggerToggle(p);
                    ports.Add(new SerializedPort(port.portName, retrigger != null ? retrigger.Value : true));
                }
            });
            return ports;
        }
    }
}