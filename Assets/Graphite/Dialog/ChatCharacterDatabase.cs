using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character Database", menuName = "Dialog/Character Database")]
public class ChatCharacterDatabase : ScriptableObject
{
    public List<ChatCharacter> characters = new List<ChatCharacter>();
}

[System.Serializable]
public class ChatCharacter
{
    public string characterName;
    public Color color = new Color(0f, 0f, 0f, 1f); // fully opaque black
}