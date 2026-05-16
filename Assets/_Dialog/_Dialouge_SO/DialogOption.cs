using System.Collections.Generic;

[System.Serializable]
public class DialogOption
{
    public string optionText;
    public List<DialogEntry> responseDialogs = new List<DialogEntry>();
    public DialogSO nextDialogSet; // Optional: switch to different DialogSO

    // Deep clone method
    public DialogOption Clone()
    {
        DialogOption clone = new DialogOption
        {
            optionText = this.optionText,
            nextDialogSet = this.nextDialogSet, // Reference is fine to keep
            responseDialogs = new List<DialogEntry>()
        };

        // Deep clone response dialogs
        foreach (var response in this.responseDialogs)
        {
            clone.responseDialogs.Add(response.Clone());
        }

        return clone;
    }
}