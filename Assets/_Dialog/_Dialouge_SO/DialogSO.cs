using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialog", menuName = "Dialog/Dialog")]
public class DialogSO : ScriptableObject
{
    [Header("Actor")]
    public string actorName = "New Actor";

    [Header("Voice Settings")]
    public AudioClip voiceClip;

    [Header("Dialog Entries")]
    public List<DialogEntry> entries = new List<DialogEntry>();

#if UNITY_EDITOR
    [TextArea(2, 5)]
    public string description;
#endif

    public AudioClip GetNextVoiceClip()
    {
        return voiceClip;
    }

    public DialogSO CreateRuntimeCopy()
    {
        DialogSO copy = CreateInstance<DialogSO>();
        copy.actorName = this.actorName;
        copy.voiceClip = this.voiceClip;
        copy.description = this.description;
        copy.entries = new List<DialogEntry>();

        foreach (var entry in this.entries)
        {
            copy.entries.Add(entry.Clone());
        }

        return copy;
    }
}