using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TMPro;
using UnityEngine;

public class TMPEffects : MonoBehaviour
{
    public enum EffectType
    {
        Wave,
        Shake,
        StopMotion,
        StopMotionWave,
        UndertaleWave
    }

    [Header("TMP Effects")]
    public EffectType effect = EffectType.Wave;
    public float waveAmplitude = 10f;
    public float waveFrequency = 2f;
    public float shakeAmount = 5f;
    public float shakeSpeed = 20f;
    public float stopMotionInterval = 0.08f;
    public float stopMotionMoveAmount = 8f;
    public float stopMotionScaleAmount = 0.2f;
    public float stopMotionWaveAmplitude = 10f;
    public float stopMotionWaveFrequency = 2f;

    [Header("Undertale Wave")]
    public float undertaleWaveAmplitude = 10f;
    public float undertaleWaveFrequency = 2f;
    public float undertaleMoveSpeed = 200f;

    private TMP_Text tmpText;
    private TMP_TextInfo textInfo;
    private float[] stopMotionTimers;
    private Vector2[] stopMotionOffsets;
    private float[] stopMotionScales;

    // --- Randomization for stop motion ---
    private int randomSeed;
    private System.Random seededRandom;

    // For undertale wave
    private float undertaleStartTime;
    private bool undertaleStarted = false;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        // Use a unique seed per instance (based on instanceID and time)
        randomSeed = GetInstanceID() ^ DateTime.Now.Millisecond ^ UnityEngine.Random.Range(0, int.MaxValue);
        seededRandom = new System.Random(randomSeed);
    }

    void Start()
    {
        if (tmpText == null) return;
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;
        InitStopMotionArrays();
    }

    void Update()
    {
        if (tmpText == null) return;

        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        switch (effect)
        {
            case EffectType.Wave:
                ApplyWave();
                break;
            case EffectType.Shake:
                ApplyShake();
                break;
            case EffectType.StopMotion:
                ApplyStopMotion();
                break;
            case EffectType.StopMotionWave:
                ApplyStopMotionWave();
                break;
            case EffectType.UndertaleWave:
                ApplyUndertaleWave();
                break;
        }
    }

    void InitStopMotionArrays()
    {
        int charCount = tmpText.textInfo.characterCount;
        stopMotionTimers = new float[charCount];
        stopMotionOffsets = new Vector2[charCount];
        stopMotionScales = new float[charCount];
        for (int i = 0; i < charCount; i++)
        {
            // Use seeded random for per-instance randomness
            stopMotionTimers[i] = (float)seededRandom.NextDouble() * stopMotionInterval;
            stopMotionOffsets[i] = Vector2.zero;
            stopMotionScales[i] = 1f;
        }
    }

    void ApplyWave()
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            float offsetY = Mathf.Sin(Time.unscaledTime * waveFrequency + i * 0.3f) * waveAmplitude;
            Vector3 offset = new Vector3(0, offsetY, 0);

            for (int j = 0; j < 4; j++)
                verts[vertIndex + j] += offset;
        }
        UpdateMesh();
    }

    void ApplyShake()
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            float offsetX = (Mathf.PerlinNoise(i, Time.unscaledTime * shakeSpeed) - 0.5f) * 2f * shakeAmount;
            float offsetY = (Mathf.PerlinNoise(i + 100, Time.unscaledTime * shakeSpeed) - 0.5f) * 2f * shakeAmount;
            Vector3 offset = new Vector3(offsetX, offsetY, 0);

            for (int j = 0; j < 4; j++)
                verts[vertIndex + j] += offset;
        }
        UpdateMesh();
    }

    void ApplyStopMotion()
    {
        int charCount = textInfo.characterCount;
        if (stopMotionTimers == null || stopMotionTimers.Length != charCount)
            InitStopMotionArrays();

        for (int i = 0; i < charCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            stopMotionTimers[i] -= Time.unscaledDeltaTime;
            if (stopMotionTimers[i] <= 0f)
            {
                stopMotionTimers[i] = stopMotionInterval;
                // Use seeded random for per-instance randomness
                stopMotionOffsets[i] = new Vector2(
                    (float)seededRandom.NextDouble() * 2f * stopMotionMoveAmount - stopMotionMoveAmount,
                    (float)seededRandom.NextDouble() * 2f * stopMotionMoveAmount - stopMotionMoveAmount
                );
                stopMotionScales[i] = 1f + ((float)seededRandom.NextDouble() * 2f * stopMotionScaleAmount - stopMotionScaleAmount);
            }

            Vector3 charMid = (verts[vertIndex] + verts[vertIndex + 2]) / 2f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = verts[vertIndex + j];
                v -= charMid;
                v *= stopMotionScales[i];
                v += charMid;
                v += (Vector3)stopMotionOffsets[i];
                verts[vertIndex + j] = v;
            }
        }
        UpdateMesh();
    }

    void ApplyStopMotionWave()
    {
        int charCount = textInfo.characterCount;
        if (stopMotionTimers == null || stopMotionTimers.Length != charCount)
            InitStopMotionArrays();

        for (int i = 0; i < charCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            stopMotionTimers[i] -= Time.unscaledDeltaTime;
            if (stopMotionTimers[i] <= 0f)
            {
                stopMotionTimers[i] = stopMotionInterval;
                stopMotionOffsets[i] = new Vector2(
                    (float)seededRandom.NextDouble() * 2f * stopMotionMoveAmount - stopMotionMoveAmount,
                    (float)seededRandom.NextDouble() * 2f * stopMotionMoveAmount - stopMotionMoveAmount
                );
                stopMotionScales[i] = 1f + ((float)seededRandom.NextDouble() * 2f * stopMotionScaleAmount - stopMotionScaleAmount);
            }

            Vector3 charMid = (verts[vertIndex] + verts[vertIndex + 2]) / 2f;

            float choppyTime = Mathf.Floor(Time.unscaledTime / stopMotionInterval) * stopMotionInterval;
            float waveOffsetY = Mathf.Sin(choppyTime * stopMotionWaveFrequency + i * 0.3f) * stopMotionWaveAmplitude;
            Vector3 waveOffset = new Vector3(0, waveOffsetY, 0);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = verts[vertIndex + j];
                v -= charMid;
                v *= stopMotionScales[i];
                v += charMid;
                v += (Vector3)stopMotionOffsets[i];
                v += waveOffset;
                verts[vertIndex + j] = v;
            }
        }
        UpdateMesh();
    }

    void ApplyUndertaleWave()
    {
        if (!undertaleStarted)
        {
            undertaleStartTime = Time.unscaledTime;
            undertaleStarted = true;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        float canvasWidth = 1920f;
        if (canvas != null && canvas.pixelRect.width > 0)
            canvasWidth = canvas.pixelRect.width;

        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        float textWidth = 0f;
        if (textInfo.characterCount > 0)
        {
            int lastVisible = -1;
            for (int i = textInfo.characterCount - 1; i >= 0; i--)
            {
                if (textInfo.characterInfo[i].isVisible)
                {
                    lastVisible = i;
                    break;
                }
            }
            if (lastVisible >= 0)
            {
                int matIndex = textInfo.characterInfo[lastVisible].materialReferenceIndex;
                int vertIndex = textInfo.characterInfo[lastVisible].vertexIndex;
                Vector3[] verts = textInfo.meshInfo[matIndex].vertices;
                float right = verts[vertIndex + 2].x;
                float left = textInfo.meshInfo[0].vertices[0].x;
                textWidth = right - left;
            }
        }

        float duration = (canvasWidth + textWidth) / undertaleMoveSpeed;
        float t = (Time.unscaledTime - undertaleStartTime) / duration;
        float startX = -canvasWidth / 2f - textWidth;
        float endX = canvasWidth / 2f + textWidth;
        float currentX = Mathf.Lerp(startX, endX, t);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            float offsetY = Mathf.Sin(Time.unscaledTime * undertaleWaveFrequency + i * 0.3f) * undertaleWaveAmplitude;

            for (int j = 0; j < 4; j++)
            {
                verts[vertIndex + j].x = currentX;
                verts[vertIndex + j].y += offsetY;
            }
        }
        UpdateMesh();

        if (t >= 1f)
        {
            undertaleStartTime = Time.unscaledTime;
        }
    }

    void UpdateMesh()
    {
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TMPEffects))]
public class TMPEffectsEditor : Editor
{
    private int effectTab = 0;
    private readonly string[] effectTabs = {
        "Wave",
        "Shake",
        "Stop Motion",
        "Stop Motion Wave",
        "Undertale Wave"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("TMP Effect Type", EditorStyles.boldLabel);

        effectTab = (int)((TMPEffects)target).effect;
        effectTab = GUILayout.Toolbar(effectTab, effectTabs);
        ((TMPEffects)target).effect = (TMPEffects.EffectType)effectTab;

        EditorGUILayout.Space(15);

        switch ((TMPEffects.EffectType)effectTab)
        {
            case TMPEffects.EffectType.Wave:
                EditorGUILayout.LabelField("Wave Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waveAmplitude"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waveFrequency"));
                break;
            case TMPEffects.EffectType.Shake:
                EditorGUILayout.LabelField("Shake Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeSpeed"));
                break;
            case TMPEffects.EffectType.StopMotion:
                EditorGUILayout.LabelField("Stop Motion Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionMoveAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionScaleAmount"));
                break;
            case TMPEffects.EffectType.StopMotionWave:
                EditorGUILayout.LabelField("Stop Motion Wave Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionMoveAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionScaleAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionWaveAmplitude"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopMotionWaveFrequency"));
                break;
            case TMPEffects.EffectType.UndertaleWave:
                EditorGUILayout.LabelField("Undertale Wave Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("undertaleWaveAmplitude"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("undertaleWaveFrequency"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("undertaleMoveSpeed"));
                break;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("This component animates TMP text with various effects. Select an effect tab to configure its settings.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
