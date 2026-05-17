using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogEntry
{
    public CharacterName character;

    [TextArea(3, 10)]
    public string dialogText;

    public bool hasOptions;
    public List<DialogOption> options = new List<DialogOption>();

    [ColorUsage(true, true)]
    public Color customWaveColor;

    [ColorUsage(true, true)]
    public Color customAlertColor;

    public DialogEntry()
    {
        // Ensure colors default to white with full alpha
        customWaveColor = new Color(1f, 1f, 1f, 1f);
        customAlertColor = new Color(1f, 1f, 1f, 1f);
        hasOptions = false;
        options = new List<DialogOption>();
    }

    public DialogEntry Clone()
    {
        DialogEntry clone = new DialogEntry
        {
            character = this.character,
            dialogText = this.dialogText,
            customWaveColor = this.customWaveColor,
            customAlertColor = this.customAlertColor,
            hasOptions = this.hasOptions,
            options = new List<DialogOption>()
        };

        foreach (var option in this.options)
        {
            clone.options.Add(option.Clone());
        }

        return clone;
    }
}