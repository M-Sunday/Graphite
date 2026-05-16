﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Manages all visual text reactions and effects for dialogue
/// </summary>
public class ReactionManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 3f;
    public float waveAmplitude = 5f;

    [Header("Alert Settings")]
    public float alertShakeMagnitude = 2f;
    public float alertShakeSpeed = 20f;

    [Header("Shake Settings")]
    public float shakeMagnitude = 6f;
    public float shakeSpeed = 25f;

    [Header("Glitch/Sick Settings")]
    public float glitchSpeed = 25f;
    public float glitchAmplitude = 3f;

    [Header("Rainbow Settings")]
    public float rainbowSpeed = 2f;
    public float rainbowSaturation = 1f;
    public float rainbowValue = 1f;

    [Header("Character Name Colors")]
    public Color mayaColor = new Color(0.8f, 0.2f, 0.2f);
    public Color cocoColor = new Color(0.2f, 0.8f, 0.2f);
    public Color mymyColor = new Color(0.2f, 0.2f, 0.8f);
    public Color veraColor = new Color(0.8f, 0.2f, 0.8f);
    public Color janColor = new Color(0.8f, 0.8f, 0.2f);
    public Color unknownColor = new Color(0.8f, 0.8f, 0.2f);
    public Color strangerColor = new Color(0.8f, 0.2f, 0.2f);

    [Header("Character Text Effects")]
    public float mayaShakeIntensity = 0.8f;
    public float mayaShakeSpeed = 15f;
    public float janWaveIntensity = 2f;
    public float janWaveSpeed = 2f;

    // References
    private TMP_Text dialogueText;
    private TMP_Text nameText;

    // Runtime state for character indices with effects
    private HashSet<int> wavyCharIndices = new();
    private HashSet<int> alertCharIndices = new();
    private HashSet<int> glitchCharIndices = new();
    private HashSet<int> shakeCharIndices = new();
    private HashSet<int> rainbowCharIndices = new();
    private Dictionary<int, float> sizeCharFactors = new();
    private Dictionary<int, Color> colorOverrides = new();
    
    // Colors for wave and alert effects from DialogueEntry
    private Color currentWaveColor = Color.white;
    private Color currentAlertColor = Color.white;
    private Dictionary<int, Color> waveCharColors = new();
    private Dictionary<int, Color> alertCharColors = new();

    private CharacterName currentCharacter;

    // Regex patterns for tag parsing
    private static readonly Regex TagRegex = new Regex(@"<(\/?(?:wave|alert|shake|glitch|rainbow|b|i|u|_pause)|(?:color|size)=([^>]+))>", RegexOptions.Compiled);

    // Cache for mesh updates
    private int lastVisibleCharacterCount = -1;

    public void Initialize(TMP_Text nameTextRef, TMP_Text dialogueTextRef)
    {
        nameText = nameTextRef;
        dialogueText = dialogueTextRef;
    }

    /// <summary>
    /// Sets the name text color based on character
    /// </summary>
    public void SetNameColor(CharacterName character)
    {
        if (nameText == null) return;

        switch (character)
        {
            case CharacterName.Maya:
                nameText.color = mayaColor;
                break;
            case CharacterName.Coco:
                nameText.color = cocoColor;
                break;
            case CharacterName.Mymy:
                nameText.color = mymyColor;
                break;
            case CharacterName.Vera:
                nameText.color = veraColor;
                break;
            case CharacterName.Jan:
                nameText.color = janColor;
                break;
            case CharacterName.Unknown:
                nameText.color = unknownColor;
                break;
            case CharacterName.Stranger:
                nameText.color = strangerColor;
                break;
        }
    }

    /// <summary>
    /// Process XML-style tags and return formatted text with indices for special effects
    /// </summary>
    public string ProcessTags(string input, Color waveColor, Color alertColor)
    {
        // Store the current wave and alert colors from the dialogue entry
        currentWaveColor = waveColor;
        currentAlertColor = alertColor;
        
        // Clear previous effect indices
        wavyCharIndices.Clear();
        alertCharIndices.Clear();
        glitchCharIndices.Clear();
        shakeCharIndices.Clear();
        rainbowCharIndices.Clear();
        sizeCharFactors.Clear();
        colorOverrides.Clear();
        waveCharColors.Clear();
        alertCharColors.Clear();

        var sb = new System.Text.StringBuilder();

        // Stack to track active effects
        Stack<string> effectStack = new Stack<string>();
        Dictionary<string, string> tagParams = new Dictionary<string, string>();

        int visIdx = 0;
        int i = 0;

        while (i < input.Length)
        {
            if (input[i] == '<')
            {
                // Find the closing '>'
                int endTag = input.IndexOf('>', i);
                if (endTag == -1) break;

                string tag = input.Substring(i + 1, endTag - i - 1);

                if (tag.StartsWith("/"))
                {
                    // Closing tag
                    string closingTag = tag.Substring(1);
                    if (effectStack.Count > 0 && effectStack.Peek() == closingTag)
                    {
                        effectStack.Pop();
                    }
                }
                else
                {
                    // Opening tag - check if it has parameters
                    int equalsIndex = tag.IndexOf('=');
                    if (equalsIndex != -1)
                    {
                        string tagName = tag.Substring(0, equalsIndex);
                        string paramValue = tag.Substring(equalsIndex + 1);

                        tagParams[tagName] = paramValue;
                        effectStack.Push(tagName);
                    }
                    else
                    {
                        // Skip _pause tag in reaction effects (handled by DialogueSystem)
                        if (tag == "_pause")
                        {
                            // Don't add to effect stack, just skip the tag
                            i = endTag + 1;
                            continue;
                        }
                        
                        effectStack.Push(tag);
                    }
                }

                i = endTag + 1;
                continue;
            }
            else
            {
                // Regular character - apply current effects
                char c = input[i];

                // Record which effects apply to this character
                foreach (string effect in effectStack)
                {
                    switch (effect)
                    {
                        case "wave":
                            wavyCharIndices.Add(visIdx);
                            waveCharColors[visIdx] = currentWaveColor;
                            break;
                        case "alert":
                            alertCharIndices.Add(visIdx);
                            alertCharColors[visIdx] = currentAlertColor;
                            break;
                        case "shake":
                            shakeCharIndices.Add(visIdx);
                            break;
                        case "glitch":
                            glitchCharIndices.Add(visIdx);
                            break;
                        case "rainbow":
                            rainbowCharIndices.Add(visIdx);
                            break;
                        case "b":
                            sb.Append("<b>");
                            break;
                        case "i":
                            sb.Append("<i>");
                            break;
                        case "u":
                            sb.Append("<u>");
                            break;
                        case "color":
                            if (tagParams.ContainsKey("color"))
                            {
                                Color color = ParseColor(tagParams["color"]);
                                colorOverrides[visIdx] = color;
                                sb.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>");
                            }
                            break;
                        case "size":
                            if (tagParams.ContainsKey("size") && float.TryParse(tagParams["size"], out float sizeFactor))
                            {
                                sizeCharFactors[visIdx] = sizeFactor;
                                sb.Append($"<size={sizeFactor * 100}%>");
                            }
                            break;
                    }
                }

                // Add the character
                sb.Append(c);

                // Close any tags that were opened for this character
                foreach (string effect in effectStack)
                {
                    switch (effect)
                    {
                        case "b":
                            sb.Append("</b>");
                            break;
                        case "i":
                            sb.Append("</i>");
                            break;
                        case "u":
                            sb.Append("</u>");
                            break;
                        case "color":
                            sb.Append("</color>");
                            break;
                        case "size":
                            sb.Append("</size>");
                            break;
                    }
                }

                visIdx++;
                i++;
            }
        }

        return sb.ToString();
    }

    private Color ParseColor(string colorString)
    {
        if (colorString.StartsWith("#") && ColorUtility.TryParseHtmlString(colorString, out Color hexColor))
        {
            return hexColor;
        }

        switch (colorString.ToLower())
        {
            case "red": return Color.red;
            case "green": return Color.green;
            case "blue": return Color.blue;
            case "yellow": return Color.yellow;
            case "cyan": return Color.cyan;
            case "magenta": return Color.magenta;
            case "white": return Color.white;
            case "black": return Color.black;
            case "gray": return Color.gray;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Set the current character for character-specific effects
    /// </summary>
    public void SetCurrentCharacter(CharacterName character)
    {
        currentCharacter = character;
    }

    /// <summary>
    /// Clear all effect indices
    /// </summary>
    public void ClearEffects()
    {
        wavyCharIndices.Clear();
        alertCharIndices.Clear();
        glitchCharIndices.Clear();
        shakeCharIndices.Clear();
        rainbowCharIndices.Clear();
        sizeCharFactors.Clear();
        colorOverrides.Clear();
        waveCharColors.Clear();
        alertCharColors.Clear();
        lastVisibleCharacterCount = -1;
    }

    /// <summary>
    /// Animate all special text effects
    /// </summary>
    public void AnimateSpecialText()
    {
        if (dialogueText == null) return;

        dialogueText.ForceMeshUpdate();
        var textInfo = dialogueText.textInfo;

        int visibleCount = dialogueText.maxVisibleCharacters < 0 ? textInfo.characterCount : Mathf.Min(dialogueText.maxVisibleCharacters, textInfo.characterCount);

        if (visibleCount == lastVisibleCharacterCount && visibleCount == 0)
            return;

        lastVisibleCharacterCount = visibleCount;

        for (int i = 0; i < visibleCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
            Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
            Vector3 offset = Vector3.zero;

            offset += GetCharacterSpecificOffset(i);
            offset += GetTagBasedOffset(i);

            // Apply wave color effect
            if (waveCharColors.TryGetValue(i, out Color waveColor))
            {
                for (int v = 0; v < 4; v++)
                {
                    int colorIndex = vertexIndex + v;
                    colors[colorIndex] = waveColor;
                }
            }

            // Apply alert color effect with pulsing
            if (alertCharColors.TryGetValue(i, out Color alertColor))
            {
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 5f + i);
                Color pulsedColor = alertColor * pulse;
                
                for (int v = 0; v < 4; v++)
                {
                    int colorIndex = vertexIndex + v;
                    colors[colorIndex] = pulsedColor;
                }
            }

            // Apply rainbow color effect
            if (rainbowCharIndices.Contains(i))
            {
                float hue = (Time.time * rainbowSpeed + i * 0.1f) % 1f;
                Color rainbowColor = Color.HSVToRGB(hue, rainbowSaturation, rainbowValue);

                for (int v = 0; v < 4; v++)
                {
                    int colorIndex = vertexIndex + v;
                    colors[colorIndex] = rainbowColor;
                }
            }

            // Apply color override
            if (colorOverrides.TryGetValue(i, out Color overrideColor))
            {
                if (!waveCharColors.ContainsKey(i) && !alertCharColors.ContainsKey(i) && !rainbowCharIndices.Contains(i))
                {
                    for (int v = 0; v < 4; v++)
                    {
                        int colorIndex = vertexIndex + v;
                        colors[colorIndex] = overrideColor;
                    }
                }
            }

            for (int v = 0; v < 4; v++)
                verts[vertexIndex + v] += offset;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            if (textInfo.meshInfo[m].mesh != null)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                textInfo.meshInfo[m].mesh.colors32 = textInfo.meshInfo[m].colors32;
                dialogueText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }
        }
    }

    private Vector3 GetCharacterSpecificOffset(int charIndex)
    {
        Vector3 offset = Vector3.zero;

        if (currentCharacter == CharacterName.Maya || currentCharacter == CharacterName.Stranger)
        {
            float shakeX = Mathf.PerlinNoise(Time.time * mayaShakeSpeed + charIndex * 0.1f, 0) * 2 - 1;
            float shakeY = Mathf.PerlinNoise(0, Time.time * mayaShakeSpeed + charIndex * 0.1f) * 2 - 1;
            offset += new Vector3(shakeX, shakeY, 0) * mayaShakeIntensity;
        }
        else if (currentCharacter == CharacterName.Jan || currentCharacter == CharacterName.Unknown)
        {
            float waveX = Mathf.Sin(Time.time * janWaveSpeed + charIndex * 0.3f) * janWaveIntensity;
            float waveY = Mathf.Cos(Time.time * janWaveSpeed * 1.25f + charIndex * 0.2f) * janWaveIntensity;
            offset += new Vector3(waveX, waveY, 0);
        }

        return offset;
    }

    private Vector3 GetTagBasedOffset(int charIndex)
    {
        Vector3 offset = Vector3.zero;

        if (wavyCharIndices.Contains(charIndex))
            offset += new Vector3(0, Mathf.Sin(Time.time * waveSpeed + charIndex * 0.2f) * waveAmplitude, 0);

        if (alertCharIndices.Contains(charIndex))
        {
            float sx = Mathf.PerlinNoise(Time.time * alertShakeSpeed, charIndex) * 2 - 1;
            float sy = Mathf.PerlinNoise(charIndex, Time.time * alertShakeSpeed) * 2 - 1;
            offset += new Vector3(sx, sy, 0) * alertShakeMagnitude;
        }

        if (glitchCharIndices.Contains(charIndex))
        {
            float gx = Mathf.Sin(Time.time * glitchSpeed + charIndex) * 0.5f;
            float gy = Mathf.Cos(Time.time * glitchSpeed + charIndex * 1.5f) * glitchAmplitude;
            offset += new Vector3(gx, gy, 0);
        }

        if (shakeCharIndices.Contains(charIndex))
        {
            float sx = Mathf.PerlinNoise(Time.time * shakeSpeed, charIndex * 1.3f) * 2 - 1;
            float sy = Mathf.PerlinNoise(charIndex * 1.3f, Time.time * shakeSpeed) * 2 - 1;
            offset += new Vector3(sx, sy, 0) * shakeMagnitude;
        }

        return offset;
    }
}