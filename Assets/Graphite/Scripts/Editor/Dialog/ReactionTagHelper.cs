using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graphite.Dialog
{
    public static class ReactionTagHelper
    {
        public static void Setup(TextField textField)
        {
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Comma && evt.shiftKey)
                {
                    evt.StopPropagation();
                    evt.PreventDefault();

                    int cursorIndex = textField.cursorIndex;

                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Insert '<' literal"), false, () =>
                    {
                        InsertText(textField, cursorIndex, "<");
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("wave"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "wave");
                    });
                    menu.AddItem(new GUIContent("alert"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "alert");
                    });
                    menu.AddItem(new GUIContent("shake"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "shake");
                    });
                    menu.AddItem(new GUIContent("glitch"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "glitch");
                    });
                    menu.AddItem(new GUIContent("rainbow"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "rainbow");
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("bold"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "b");
                    });
                    menu.AddItem(new GUIContent("italic"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "i");
                    });
                    menu.AddItem(new GUIContent("underline"), false, () =>
                    {
                        InsertTag(textField, cursorIndex, "u");
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("pause"), false, () =>
                    {
                        InsertText(textField, cursorIndex, "<_pause>");
                    });
                    menu.ShowAsContext();
                }
            });
        }

        private static void InsertTag(TextField textField, int cursor, string tag)
        {
            string text = textField.value;
            string before = text.Substring(0, cursor);
            string after = text.Substring(cursor);

            textField.value = before + $"<{tag}></{tag}>" + after;
            textField.cursorIndex = cursor + $"<{tag}>".Length;
        }

        private static void InsertText(TextField textField, int cursor, string insert)
        {
            string text = textField.value;
            string before = text.Substring(0, cursor);
            string after = text.Substring(cursor);

            textField.value = before + insert + after;
            textField.cursorIndex = cursor + insert.Length;
        }
    }
}
