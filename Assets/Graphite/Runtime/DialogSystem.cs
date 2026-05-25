using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public enum CharacterName { Maya, Coco, Mymy, Vera, Jan, Unknown, Stranger, Activator }

public class DialogSystem : MonoBehaviour
{
    [Header("Current Dialog")]
    public DialogSO currentDialog; // Assign in inspector or via code

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text dialogText;
    public TMP_Text continuePromptText;
    [Space(10)]
    public RectTransform dialogPanel;

    [Header("Options UI")]
    public Canvas optionsCanvas;
    public Transform optionsGrid;
    public GameObject optionButtonPrefab;

    [Header("Input & Audio")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode advanceKey = KeyCode.Tab;
    public float typeSpeed = 0.05f;
    public AudioSource voiceSource;

    [Header("Undertale-Style Voice Settings")]
    public bool useUndertaleVoice = true;
    public float voicePitchMin = 0.9f;
    public float voicePitchMax = 1.1f;
    public float voiceVolume = 0.5f;

    [Header("Voice Timing")]
    [Tooltip("Minimum time between voice sounds (in seconds)")]
    public float minTimeBetweenSounds = 0.05f;
    [Tooltip("Play sound every X letters (higher = less frequent)")]
    public int lettersPerSound = 2;
    [Tooltip("Only play for certain character types (letters, punctuation, etc)")]
    public bool onlyPlayForLetters = true;
    [Tooltip("Add random delay variation to sounds")]
    public bool randomizeTiming = true;

    [Header("Panel Animation")]
    public float moveDistance = 100f;
    public float moveDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Reactions")]
    public ReactionManager reactionManager; // Reference to the Reaction Manager

    public static DialogSystem Instance;

    // --- Events ---
    public event System.Action OnDialogEnded;
    public event System.Action<DialogSO> OnDialogStarted;

    // Runtime state
    private int currentDialogIndex = -1;
    private bool isTyping = false;
    private bool isDialogActive = false;
    private bool waitingForOptionSelection = false;
    private bool isPaused = false;
    private bool isPlayingResponses = false;
    private bool optionResponseSelected = false;
    [System.NonSerialized]
    private List<DialogEntry> pendingOptionChain = null;
    private Vector2 originalPanelPosition;
    private Coroutine typingCoroutine, moveCoroutine, dotCoroutine;
    private Coroutine reactionAnimationCoroutine;
    private CanvasGroup canvasGroup;

    private DialogEntry currentDialogEntry;
    private List<GameObject> spawnedOptionButtons = new List<GameObject>();
    private DialogSO activeDialog;

    // Undertale voice tracking
    private int letterCounter = 0;
    private float lastVoiceTime = 0f;

    // Pause tag tracking
    private int pausePosition = -1;
    private bool pauseTriggered = false;
    private string originalProcessedText = "";

    // Track current question number
    private int currentQuestionNumber = 0;

    // References to UI elements inside the prefab
    private TMP_Text numText; // Will be found automatically
    private TMP_Text optText; // Will be found automatically

    public bool IsDialogActive => isDialogActive;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (dialogPanel)
        {
            originalPanelPosition = dialogPanel.anchoredPosition;
            canvasGroup = dialogPanel.GetComponent<CanvasGroup>() ?? dialogPanel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            dialogPanel.gameObject.SetActive(false);
        }

        if (continuePromptText) continuePromptText.gameObject.SetActive(false);
        if (optionsCanvas) optionsCanvas.gameObject.SetActive(false);

        // Find Num_Text and Opt_Text in the options canvas
        if (optionsCanvas != null)
        {
            // Find Num_Text by name
            Transform numTextTransform = optionsCanvas.transform.Find("Num_Text");
            if (numTextTransform != null)
            {
                numText = numTextTransform.GetComponent<TMP_Text>();
                if (numText != null)
                {
                    numText.gameObject.SetActive(false);
                }
            }

            // Find Opt_Text by name (this is the template for options)
            Transform optTextTransform = optionsCanvas.transform.Find("Opt_Text");
            if (optTextTransform != null)
            {
                optText = optTextTransform.GetComponent<TMP_Text>();
            }
        }

        // Initialize Reaction Manager if assigned
        if (reactionManager != null)
        {
            reactionManager.Initialize(nameText, dialogText);
        }
    }

    void Update()
    {
        if (isDialogActive && !waitingForOptionSelection && !isPlayingResponses && Input.GetKeyDown(advanceKey))
        {
            if (isTyping) 
                SkipTyping();
            else if (isPaused)
                ContinueFromPause();
            else 
                NextDialog();
        }
    }

    private IEnumerator AnimateReactionsContinuous()
    {
        while (isDialogActive && reactionManager != null && dialogPanel != null && dialogPanel.gameObject.activeInHierarchy)
        {
            reactionManager.AnimateSpecialText();
            yield return null;
        }
    }

    public void StartDialog()
    {
        StartDialog(currentDialog);
    }

    public void StartDialog(DialogSO dialog)
    {
        if (dialog == null || dialog.entries.Count == 0)
        {
            Debug.LogWarning("No dialog assigned or dialog is empty!");
            return;
        }

        activeDialog = dialog;

        // Reset letter counter and voice timer
        letterCounter = 0;
        lastVoiceTime = 0f;

        isDialogActive = true;
        currentDialogIndex = -1;
        waitingForOptionSelection = false;
        isPaused = false;
        pausePosition = -1;
        pauseTriggered = false;
        currentQuestionNumber = 0;

        if (dialogPanel)
        {
            dialogPanel.gameObject.SetActive(true);
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MovePanel(true));
        }

        // Hide number text at start
        if (numText != null)
        {
            numText.gameObject.SetActive(false);
        }

        // Start continuous reaction animation
        if (reactionAnimationCoroutine != null) StopCoroutine(reactionAnimationCoroutine);
        reactionAnimationCoroutine = StartCoroutine(AnimateReactionsContinuous());

        OnDialogStarted?.Invoke(dialog);
        NextDialog();
    }

    public void NextDialog()
    {
        if (waitingForOptionSelection || activeDialog == null) return;

        HideContinuePrompt();
        ClearOptions();

        currentDialogIndex++;

        while (currentDialogIndex < activeDialog.entries.Count)
        {
            var entry = activeDialog.entries[currentDialogIndex];
            currentDialogEntry = entry;
                DisplayDialog(entry);

                // Hide number text when displaying regular dialog
                if (numText != null)
                {
                    numText.gameObject.SetActive(false);
                }

                if (entry.hasOptions && entry.options.Count > 0)
                {
                    bool hasPauseBeforeEnd = entry.dialogText.Contains("<_pause>");
                    if (!hasPauseBeforeEnd)
                    {
                        waitingForOptionSelection = true;
                    }
                }
                return;
            }

        EndDialog();
    }

    private void DisplayDialog(DialogEntry dialog)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (nameText == null || dialogText == null)
        {
            Debug.LogError("DialogSystem: nameText or dialogText UI references are null!");
            return;
        }

        // Set name color using Reaction Manager
        if (reactionManager != null)
        {
            reactionManager.SetNameColor(dialog.character);
            reactionManager.SetCurrentCharacter(dialog.character);
        }

        nameText.text = dialog.character.ToString();

        // Process the dialog text with the tag system
        if (reactionManager != null)
        {
            // Pass the custom colors from DialogEntry
            originalProcessedText = reactionManager.ProcessTags(
                dialog.dialogText,
                dialog.customWaveColor,
                dialog.customAlertColor);
            dialogText.text = originalProcessedText;
        }
        else
        {
            originalProcessedText = dialog.dialogText;
            dialogText.text = originalProcessedText;
        }

        // Check for pause tags in the original text
        CheckForPauseTag(dialog.dialogText);

        typingCoroutine = StartCoroutine(TypeDialog(dialog));
    }

    private void CheckForPauseTag(string originalText)
    {
        pausePosition = -1;
        pauseTriggered = false;

        // Find the position of <_pause> in the original text
        int pauseTagIndex = originalText.IndexOf("<_pause>");
        if (pauseTagIndex != -1)
        {
            // Count visible characters before the pause tag
            int visibleCharsBeforePause = 0;
            bool insideTag = false;
            
            for (int i = 0; i < pauseTagIndex; i++)
            {
                if (originalText[i] == '<')
                    insideTag = true;
                else if (originalText[i] == '>')
                    insideTag = false;
                else if (!insideTag && !char.IsControl(originalText[i]))
                    visibleCharsBeforePause++;
            }
            
            // The pause should occur AFTER the character at this position
            pausePosition = visibleCharsBeforePause;
            Debug.Log($"Pause will trigger after revealing character {pausePosition}");
        }
    }

    private void PlayVoiceSound()
    {
        if (!useUndertaleVoice) return;
        if (voiceSource == null) return;
        if (activeDialog == null) return;
        if (activeDialog.voiceClip == null) return;

        if (Time.time - lastVoiceTime < minTimeBetweenSounds)
            return;

        float originalPitch = voiceSource.pitch;
        float originalVolume = voiceSource.volume;

        voiceSource.pitch = Random.Range(voicePitchMin, voicePitchMax);
        voiceSource.volume = voiceVolume;

        voiceSource.PlayOneShot(activeDialog.voiceClip);

        lastVoiceTime = Time.time;

        voiceSource.pitch = originalPitch;
        voiceSource.volume = originalVolume;
    }

    private bool ShouldPlayVoiceForChar(char c)
    {
        if (!onlyPlayForLetters)
            return true;

        return char.IsLetterOrDigit(c);
    }

    private IEnumerator TypeDialog(DialogEntry dialog)
    {
        isTyping = true;
        dialogText.maxVisibleCharacters = 0;

        letterCounter = 0;

        dialogText.ForceMeshUpdate();

        int total = dialogText.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            // Check if we've reached the pause position
            if (pausePosition != -1 && !pauseTriggered && i == pausePosition + 1)
            {
                // We just revealed the character at pausePosition, now pause
                dialogText.maxVisibleCharacters = i;
                
                if (reactionManager != null)
                {
                    reactionManager.AnimateSpecialText();
                }
                
                isTyping = false;
                isPaused = true;
                pauseTriggered = true;
                
                // Just show the continue prompt, don't show options yet
                ShowContinuePrompt();
                
                yield break;
            }

            dialogText.maxVisibleCharacters = i;

            if (reactionManager != null)
            {
                reactionManager.AnimateSpecialText();
            }

            if (i < total && i > 0)
            {
                var charInfo = dialogText.textInfo.characterInfo[i - 1];
                if (charInfo.isVisible)
                {
                    char revealedChar = charInfo.character;

                    if (ShouldPlayVoiceForChar(revealedChar))
                    {
                        letterCounter++;

                        bool shouldPlay = false;

                        if (lettersPerSound <= 1)
                        {
                            shouldPlay = true;
                        }
                        else
                        {
                            shouldPlay = (letterCounter % lettersPerSound == 0);
                        }

                        if (shouldPlay)
                        {
                            PlayVoiceSound();

                            if (randomizeTiming)
                            {
                                yield return new WaitForSeconds(Random.Range(0f, typeSpeed * 0.5f));
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        // Only show options if we completed typing AND there's no pending pause
        if (!pauseTriggered)
        {
            isTyping = false;

            if (dialog.hasOptions && dialog.options.Count > 0)
            {
                waitingForOptionSelection = true;
                ShowOptions(dialog.options);
            }
            else
            {
                ShowContinuePrompt();
            }
        }
    }

    private void ContinueFromPause()
    {
        if (!isPaused) return;

        isPaused = false;
        HideContinuePrompt();

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(ResumeTypingFromPause());
    }

    private IEnumerator ResumeTypingFromPause()
    {
        isTyping = true;
        
        int startPosition = dialogText.maxVisibleCharacters;
        int total = dialogText.textInfo.characterCount;
        
        dialogText.ForceMeshUpdate();

        for (int i = startPosition + 1; i <= total; i++)
        {
            dialogText.maxVisibleCharacters = i;

            if (reactionManager != null)
            {
                reactionManager.AnimateSpecialText();
            }

            if (i < total && i > 0)
            {
                var charInfo = dialogText.textInfo.characterInfo[i - 1];
                if (charInfo.isVisible)
                {
                    char revealedChar = charInfo.character;
                    if (ShouldPlayVoiceForChar(revealedChar))
                    {
                        letterCounter++;
                        bool shouldPlay = false;

                        if (lettersPerSound <= 1)
                        {
                            shouldPlay = true;
                        }
                        else
                        {
                            shouldPlay = (letterCounter % lettersPerSound == 0);
                        }

                        if (shouldPlay)
                        {
                            PlayVoiceSound();
                            if (randomizeTiming)
                            {
                                yield return new WaitForSeconds(Random.Range(0f, typeSpeed * 0.5f));
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // After finishing typing from pause, check if we have options
        if (currentDialogEntry != null)
        {
            if (currentDialogEntry.hasOptions && currentDialogEntry.options.Count > 0)
            {
                // Make sure we're not already waiting for options
                if (!waitingForOptionSelection)
                {
                    waitingForOptionSelection = true;
                    ShowOptions(currentDialogEntry.options);
                }
            }
            else
            {
                ShowContinuePrompt();
            }
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        if (isPaused)
        {
            isPaused = false;
            HideContinuePrompt();
        }
        
        dialogText.maxVisibleCharacters = int.MaxValue;
        
        if (reactionManager != null)
        {
            reactionManager.AnimateSpecialText();
        }
        
        isTyping = false;

        // Handle skip properly when there are options
        if (currentDialogEntry != null)
        {
            if (currentDialogEntry.hasOptions && currentDialogEntry.options.Count > 0)
            {
                if (!waitingForOptionSelection)
                {
                    waitingForOptionSelection = true;
                    ShowOptions(currentDialogEntry.options);
                }
            }
            else
            {
                ShowContinuePrompt();
            }
        }
    }

private void ShowOptions(List<DialogOption> options)
{
    if (optionsCanvas == null || optionsGrid == null || optionButtonPrefab == null)
    {
        Debug.LogError("Options UI references not set in DialogSystem!");
        ShowContinuePrompt();
        return;
    }

    ClearOptions();
    
    // Increment question number for the main Num_Text (if you want to keep it)
    currentQuestionNumber++;
    if (numText != null)
    {
        numText.text = currentQuestionNumber.ToString();
        numText.gameObject.SetActive(true);
    }
    
    optionsCanvas.gameObject.SetActive(true);

    for (int i = 0; i < options.Count; i++)
    {
        int optionIndex = i;
        GameObject buttonObj = Instantiate(optionButtonPrefab, optionsGrid);
        spawnedOptionButtons.Add(buttonObj);

        // Find ALL TMP_Text components in the button and its children
        TMP_Text[] allTextComponents = buttonObj.GetComponentsInChildren<TMP_Text>(true);
        
        // Track if we found and set both text components
        bool foundNumText = false;
        bool foundOptText = false;
        
        foreach (TMP_Text textComp in allTextComponents)
        {
            // Check by name (case insensitive)
            string componentName = textComp.gameObject.name.ToLower();
            
            // If it's Num_Text or contains "num", set the number
            if (componentName.Contains("num") || componentName == "num_text")
            {
                textComp.text = (i + 1).ToString();
                textComp.gameObject.SetActive(true);
                foundNumText = true;
            }
            // If it's Opt_Text or contains "opt", set the option text
            else if (componentName.Contains("opt") || componentName == "opt_text")
            {
                textComp.text = options[i].optionText;
                textComp.gameObject.SetActive(true);
                foundOptText = true;
            }
        }
        
        // If we didn't find them by name, try a different approach - maybe they're the only text components
        if (!foundNumText || !foundOptText)
        {
            if (allTextComponents.Length >= 2)
            {
                // Assume first text component is the number
                if (!foundNumText)
                {
                    allTextComponents[0].text = (i + 1).ToString();
                    allTextComponents[0].gameObject.SetActive(true);
                }
                
                // Assume second text component is the option text
                if (!foundOptText && allTextComponents.Length >= 2)
                {
                    allTextComponents[1].text = options[i].optionText;
                    allTextComponents[1].gameObject.SetActive(true);
                }
            }
        }

        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnOptionSelected(optionIndex));
        }
    }
}

    private void OnOptionSelected(int optionIndex)
    {
        if (currentDialogEntry == null || !currentDialogEntry.hasOptions)
            return;

        optionsCanvas.gameObject.SetActive(false);
        ClearOptions();

        DialogOption selectedOption = currentDialogEntry.options[optionIndex];

        waitingForOptionSelection = false;

        // Switch dialog set
        if (selectedOption.nextDialogSet != null)
        {
            StartDialog(selectedOption.nextDialogSet);
            return;
        }

        // If we're inside PlayOptionResponses, signal the pending chain
        // instead of starting a nested coroutine
        if (isPlayingResponses)
        {
            pendingOptionChain = selectedOption.responseDialogs;
            optionResponseSelected = true;
            return;
        }

        // Play response dialogs (top-level option from main entries)
        if (selectedOption.responseDialogs != null && selectedOption.responseDialogs.Count > 0)
        {
            isPlayingResponses = true;
            StartCoroutine(PlayOptionResponses(selectedOption.responseDialogs));
        }
        else
        {
            NextDialog();
        }
    }

    private IEnumerator PlayOptionResponses(List<DialogEntry> responses)
    {
        foreach (var entry in responses)
        {
            currentDialogEntry = entry;
            DisplayDialog(entry);

            while (isTyping)
                yield return null;

            // If this response entry itself has options, wait for selection
            // and play the chosen inline chain before continuing the outer list
            if (entry.hasOptions && entry.options.Count > 0)
            {
                optionResponseSelected = false;
                pendingOptionChain = null;

                yield return new WaitUntil(() => optionResponseSelected);

                // Play the selected option's response chain inline
                if (pendingOptionChain != null)
                {
                    foreach (var sub in pendingOptionChain)
                    {
                        currentDialogEntry = sub;
                        DisplayDialog(sub);
                        while (isTyping)
                            yield return null;
                        yield return new WaitUntil(() => Input.GetKeyDown(advanceKey));
                    }
                }

                pendingOptionChain = null;
            }
            else
            {
                yield return new WaitUntil(() => Input.GetKeyDown(advanceKey));
            }
        }

        isPlayingResponses = false;
        NextDialog();
    }

    private void ClearOptions()
{
    foreach (GameObject button in spawnedOptionButtons)
    {
        if (button != null) Destroy(button);
    }
    spawnedOptionButtons.Clear();

    if (optionsCanvas != null)
    {
        optionsCanvas.gameObject.SetActive(false);
    }
    
    // Hide the main number text when options are cleared
    if (numText != null)
    {
        numText.gameObject.SetActive(false);
    }
}

    private IEnumerator MovePanel(bool show)
    {
        Vector2 start = dialogPanel.anchoredPosition;
        Vector2 target = show ? originalPanelPosition + new Vector2(0, moveDistance) : originalPanelPosition;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        float t = 0f;
        while (t < moveDuration)
        {
            float f = moveCurve.Evaluate(t / moveDuration);
            dialogPanel.anchoredPosition = Vector2.Lerp(start, target, f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, f);
            t += Time.deltaTime;
            yield return null;
        }

        dialogPanel.anchoredPosition = target;
        canvasGroup.alpha = targetAlpha;
    }

    public void EndDialog()
    {
        isDialogActive = false;
        isTyping = false;
        waitingForOptionSelection = false;
        isPaused = false;
        pausePosition = -1;
        pauseTriggered = false;
        currentQuestionNumber = 0;

        if (reactionAnimationCoroutine != null)
        {
            StopCoroutine(reactionAnimationCoroutine);
            reactionAnimationCoroutine = null;
        }

        ClearOptions();
        isPlayingResponses = false;
        optionResponseSelected = false;
        pendingOptionChain = null;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
            dotCoroutine = null;
        }

        if (reactionManager != null)
        {
            reactionManager.ClearEffects();
        }

        // Clear text to remove any lingering vertex effects (wave, shake, etc.)
        if (dialogText != null) dialogText.text = "";
        if (nameText != null) nameText.text = "";

        // Hide number text
        if (numText != null)
        {
            numText.gameObject.SetActive(false);
        }

        HideContinuePrompt();

        if (dialogPanel != null)
        {
            moveCoroutine = StartCoroutine(MovePanel(false));
            StartCoroutine(DeactivateAfterMove());
        }

        OnDialogEnded?.Invoke();
    }

    private IEnumerator DeactivateAfterMove()
    {
        yield return new WaitForSeconds(moveDuration);
        dialogPanel.gameObject.SetActive(false);
    }

    private void ShowContinuePrompt()
    {
        if (!continuePromptText) return;
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        continuePromptText.text = "...";
        continuePromptText.gameObject.SetActive(true);
        dotCoroutine = StartCoroutine(AnimateDots());
    }

    private void HideContinuePrompt()
    {
        if (!continuePromptText) return;
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        continuePromptText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateDots()
    {
        const float jump = 6f;
        while (true)
        {
            for (int step = 0; step < 3; step++)
            {
                var sb = new StringBuilder();
                for (int d = 0; d < 3; d++)
                {
                    float offset = (d == step) ? jump * Mathf.Sin(Time.time * 10f) : 0f;
                    sb.Append(d == 0 ? "" : " ");
                    sb.Append($"<voffset={offset}>.");
                }
                continuePromptText.text = sb.ToString();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}