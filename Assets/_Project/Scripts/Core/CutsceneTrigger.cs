using System;
using System.Collections;
using UnityEngine;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 9.4: Cutscene trigger system for house flow.
///
/// This system plays DOTween-based cutscenes when triggered by HouseFlowController.
/// Cutscenes are defined in Cutscenes.csv and include various types:
/// - TextReveal: Text fades in with typewriter effect
/// - CharacterReaction: Character sprite changes expression + text
/// - CameraPan: Camera moves to show different area
/// - Dialogue: Two characters exchange lines
/// - ReactionShot: Single character reacts to previous event
///
/// ARCHITECTURE:
/// 1. HouseFlowController calls CutsceneTrigger.PlayCutscene(cutsceneData, onComplete)
/// 2. CutsceneTrigger loads cutscene config from CutsceneData (CSV)
/// 3. Plays appropriate DOTween animation based on CutsceneType
/// 4. Shows Arabic text with animation
/// 5. Calls onComplete callback when cutscene duration expires
///
/// USAGE:
/// 1. Add CutsceneTrigger component to a GameObject in the scene
/// 2. Assign cutscene UI prefabs (text panel, character sprites, etc.)
/// 3. HouseFlowController will call PlayCutscene() automatically during house sequences
/// </summary>
public class CutsceneTrigger : MonoBehaviour
{
    public static CutsceneTrigger Instance { get; private set; }

    #region Inspector Fields

    [Header("Cutscene UI")]
    [Tooltip("Main cutscene panel/background")]
    [SerializeField] private GameObject cutscenePanel;

    [Tooltip("Text display for cutscene dialogue/narration")]
    [SerializeField] private RTLTextMeshPro cutsceneText;

    [Tooltip("Character sprite/image (for CharacterReaction, ReactionShot)")]
    [SerializeField] private UnityEngine.UI.Image characterSprite;

    [Tooltip("Secondary character sprite (for Dialogue type)")]
    [SerializeField] private UnityEngine.UI.Image secondaryCharacterSprite;

    [Tooltip("Secondary text (for Dialogue type)")]
    [SerializeField] private RTLTextMeshPro secondaryText;

    [Header("Sprite Libraries")]
    [Tooltip("Character expression ScriptableObjects (assign all characters' expressions)")]
    [SerializeField] private CharacterExpressionSO[] characterExpressions;

    [Header("Animation Settings")]
    [Tooltip("Default cutscene duration if not specified in data")]
    [SerializeField] private float defaultDuration = 3f;

    [Tooltip("Text reveal speed (characters per second) for Typewriter animation")]
    [SerializeField] private float typewriterSpeed = 20f;

    [Header("Visual Feedback")]
    [Tooltip("Color for normal text")]
    [SerializeField] private Color normalTextColor = Color.white;

    [Tooltip("Color for emphasized text")]
    [SerializeField] private Color emphasizedTextColor = new Color(1f, 0.85f, 0f);

    [Header("Camera")]
    [Tooltip("Camera to pan (for CameraPan type)")]
    [SerializeField] private Camera cutsceneCamera;

    [Tooltip("Camera pan positions (start and end transforms)")]
    [SerializeField] private Transform cameraStartPosition;

    [Tooltip("Camera pan position end")]
    [SerializeField] private Transform cameraEndPosition;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging")]
    [SerializeField] private bool debugLogging = false;

    #endregion

    #region State

    private CutsceneData currentCutsceneData;
    private Action<string> onCompleteCallback;
    private bool isCutscenePlaying = false;
    private Coroutine cutsceneCoroutine;

    #endregion

    #region Events

    /// <summary>
    /// Fires when a cutscene starts playing.
    /// (cutsceneID, cutsceneType, duration)
    /// </summary>
    public static Action<string, CutsceneType, float> OnCutsceneStarted;

    /// <summary>
    /// Fires when a cutscene completes.
    /// (cutsceneID)
    /// </summary>
    public static Action<string> OnCutsceneCompleted;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("[CutsceneTrigger] Duplicate instance! Destroying.");
            Destroy(gameObject);
            return;
        }

        // Ensure UI is hidden on start
        if (cutscenePanel != null)
            cutscenePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (cutsceneCoroutine != null)
            StopCoroutine(cutsceneCoroutine);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a cutscene with the given configuration.
    /// Called by HouseFlowController when a Cutscene element is triggered.
    /// </summary>
    /// <param name="cutsceneData">Cutscene configuration from CSV</param>
    /// <param name="onComplete">Callback: (cutsceneID)</param>
    public void PlayCutscene(CutsceneData cutsceneData, Action<string> onComplete)
    {
        if (isCutscenePlaying)
        {
            Debug.LogWarning("[CutsceneTrigger] Cutscene already playing! Ignoring PlayCutscene call.");
            return;
        }

        currentCutsceneData = cutsceneData;
        onCompleteCallback = onComplete;
        isCutscenePlaying = true;

        float duration = cutsceneData.Duration > 0 ? cutsceneData.Duration : defaultDuration;

        if (debugLogging)
            Debug.Log($"[CutsceneTrigger] Playing cutscene: {cutsceneData.ID} | Type: {cutsceneData.CutsceneType} | Duration: {duration}s");
        if (debugLogging)
            Debug.Log($"[CutsceneTrigger] Text: \"{cutsceneData.TextAR}\"");

        // Fire event
        OnCutsceneStarted?.Invoke(cutsceneData.ID, cutsceneData.CutsceneType, duration);

        // Show cutscene UI
        ShowCutsceneUI();

        // Start cutscene coroutine
        if (cutsceneCoroutine != null)
            StopCoroutine(cutsceneCoroutine);
        cutsceneCoroutine = StartCoroutine(PlayCutsceneCoroutine(cutsceneData, duration));
    }

    /// <summary>
    /// Cancels the currently playing cutscene.
    /// </summary>
    public void CancelActiveCutscene()
    {
        if (!isCutscenePlaying) return;

        if (debugLogging)
            Debug.Log("[CutsceneTrigger] Cutscene cancelled externally.");

        isCutscenePlaying = false;
        if (cutsceneCoroutine != null)
            StopCoroutine(cutsceneCoroutine);

        // Callback anyway
        onCompleteCallback?.Invoke(currentCutsceneData?.ID ?? "Unknown");
        OnCutsceneCompleted?.Invoke(currentCutsceneData?.ID ?? "Unknown");

        HideCutsceneUI();
    }

    #endregion

    #region Cutscene Logic

    /// <summary>
    /// Main cutscene playback coroutine.
    /// </summary>
    private IEnumerator PlayCutsceneCoroutine(CutsceneData cutsceneData, float duration)
    {
        // Play animation based on type
        switch (cutsceneData.CutsceneType)
        {
            case CutsceneType.TextReveal:
                yield return PlayTextReveal(cutsceneData.TextAR, duration);
                break;

            case CutsceneType.CharacterReaction:
                yield return PlayCharacterReaction(cutsceneData.TextAR, duration);
                break;

            case CutsceneType.CameraPan:
                yield return PlayCameraPan(cutsceneData.TextAR, duration);
                break;

            case CutsceneType.Dialogue:
                yield return PlayDialogue(cutsceneData.TextAR, duration);
                break;

            case CutsceneType.ReactionShot:
                yield return PlayReactionShot(cutsceneData.TextAR, duration);
                break;

            default:
                Debug.LogWarning($"[CutsceneTrigger] Unknown cutscene type: {cutsceneData.CutsceneType}. Using TextReveal.");
                yield return PlayTextReveal(cutsceneData.TextAR, duration);
                break;
        }

        // Cutscene complete
        if (isCutscenePlaying)
        {
            if (debugLogging)
                Debug.Log($"[CutsceneTrigger] Cutscene complete: {cutsceneData.ID}");

            isCutscenePlaying = false;
            OnCutsceneCompleted?.Invoke(cutsceneData.ID);
            onCompleteCallback?.Invoke(cutsceneData.ID);

            // Hide UI after brief delay
            yield return new WaitForSeconds(0.5f);
            HideCutsceneUI();
        }
    }

    /// <summary>
    /// TextReveal: Text fades in with typewriter effect.
    /// </summary>
    private IEnumerator PlayTextReveal(string text, float duration)
    {
        if (cutsceneText == null)
        {
            Debug.LogWarning("[CutsceneTrigger] CutsceneText not assigned!");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        // Typewriter effect - use Rune API to safely handle UTF-16 surrogate pairs
        cutsceneText.text = "";
        cutsceneText.color = normalTextColor;

#if UNITY_2023_1_OR_NEWER
        var runes = text.EnumerateRunes().ToArray();
        int totalRunes = runes.Length;
#else
        // Fallback for Unity 2022: manually skip surrogate pairs
        int totalRunes = CountTextElements(text);
#endif

        float charDelay = 1f / typewriterSpeed;
        float estimatedTime = totalRunes * charDelay;

        // If estimated time is less than duration, add pause at end
        // If more, speed up to fit duration
        if (estimatedTime > duration)
        {
            charDelay = duration / totalRunes;
        }

        for (int i = 0; i < totalRunes; i++)
        {
            if (!isCutscenePlaying) yield break;

            // Safe substring that respects UTF-16 surrogate pairs
            cutsceneText.text = SafeSubstring(text, i + 1);
            yield return new WaitForSeconds(charDelay);
        }

        // If typewriter finished early, wait for remaining duration
        float remaining = duration - (totalRunes * charDelay);
        if (remaining > 0)
        {
            yield return new WaitForSeconds(remaining);
        }
    }

    /// <summary>
    /// Safely extracts a substring without breaking UTF-16 surrogate pairs.
    /// </summary>
    private string SafeSubstring(string text, int elementCount)
    {
        int charIndex = 0;
        int elementsFound = 0;

        while (elementsFound < elementCount && charIndex < text.Length)
        {
            // Check if current char is a high surrogate
            if (char.IsHighSurrogate(text[charIndex]) && charIndex + 1 < text.Length && char.IsLowSurrogate(text[charIndex + 1]))
            {
                charIndex += 2; // Skip both surrogate chars
            }
            else
            {
                charIndex += 1;
            }
            elementsFound++;
        }

        return text.Substring(0, charIndex);
    }

    /// <summary>
    /// Counts visible text elements (respects surrogate pairs).
    /// </summary>
    private int CountTextElements(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        int count = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i += 2; // Surrogate pair = 1 element
            }
            else
            {
                i += 1;
            }
            count++;
        }
        return count;
    }

    /// <summary>
    /// CharacterReaction: Character sprite changes expression + text.
    /// Now uses CharacterExpressionSO system from CSV data.
    /// </summary>
    private IEnumerator PlayCharacterReaction(string text, float duration)
    {
        // Show character sprite if assigned
        if (characterSprite != null)
        {
            characterSprite.gameObject.SetActive(true);

            // Get sprite from expression system using CSV data
            string charName = currentCutsceneData?.CharacterName ?? "";
            string exprName = currentCutsceneData?.ExpressionName ?? "Neutral";

            Sprite sprite = GetCharacterSprite(exprName, charName);
            if (sprite != null)
            {
                characterSprite.sprite = sprite;
            }

            // Pop-in animation
            characterSprite.transform.localScale = Vector3.zero;
            characterSprite.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        // Show text
        if (cutsceneText != null)
        {
            cutsceneText.text = text;
            cutsceneText.color = emphasizedTextColor;

            // Fade in text
            CanvasGroup textGroup = cutsceneText.GetComponent<CanvasGroup>();
            if (textGroup != null)
            {
                textGroup.alpha = 0f;
                textGroup.DOFade(1f, 0.5f);
            }
        }

        // Wait for duration
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// Gets character sprite by expression name from the expression library.
    /// </summary>
    /// <param name="expressionName">Expression name (case-insensitive)</param>
    /// <param name="characterName">Optional character name to search specific SO</param>
    /// <returns>Sprite if found, null otherwise</returns>
    private Sprite GetCharacterSprite(string expressionName, string characterName = null)
    {
        if (characterExpressions == null || characterExpressions.Length == 0)
        {
            Debug.LogWarning("[CutsceneTrigger] No character expressions assigned!");
            return null;
        }

        // If character name provided, search for that specific character
        if (!string.IsNullOrEmpty(characterName))
        {
            foreach (var charExpr in characterExpressions)
            {
                if (charExpr != null && charExpr.characterName.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                {
                    return charExpr.GetExpressionSprite(expressionName);
                }
            }
        }

        // Otherwise, use first character's expressions (fallback)
        if (characterExpressions[0] != null)
        {
            return characterExpressions[0].GetExpressionSprite(expressionName);
        }

        return null;
    }

    /// <summary>
    /// CameraPan: Camera moves to show different area.
    /// </summary>
    private IEnumerator PlayCameraPan(string text, float duration)
    {
        // Show text
        if (cutsceneText != null)
        {
            cutsceneText.text = text;
            cutsceneText.color = normalTextColor;
        }

        // Pan camera if positions assigned
        if (cutsceneCamera != null && cameraStartPosition != null && cameraEndPosition != null)
        {
            // Set start position
            cutsceneCamera.transform.position = cameraStartPosition.position;

            // Smooth pan to end
            Vector3 startPos = cameraStartPosition.position;
            Vector3 endPos = cameraEndPosition.position;

            float elapsed = 0f;
            while (elapsed < duration && isCutscenePlaying)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutSine);

                cutsceneCamera.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                yield return null;
            }
        }
        else
        {
            // No camera setup, just wait
            yield return new WaitForSeconds(duration);
        }
    }

    /// <summary>
    /// Dialogue: Two characters exchange lines.
    /// For now, shows single text with emphasis.
    /// TODO: Split text into two parts for back-and-forth
    /// </summary>
    private IEnumerator PlayDialogue(string text, float duration)
    {
        // Show both texts if available
        if (cutsceneText != null)
        {
            cutsceneText.text = text;
            cutsceneText.color = normalTextColor;

            CanvasGroup textGroup = cutsceneText.GetComponent<CanvasGroup>();
            if (textGroup != null)
            {
                textGroup.alpha = 0f;
                textGroup.DOFade(1f, 0.3f);
            }
        }

        if (secondaryText != null)
        {
            // For now, show same text or empty
            // TODO: Parse dialogue from CSV with two parts
            secondaryText.text = "";
        }

        // Wait for duration
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// ReactionShot: Single character reacts to previous event.
    /// Similar to CharacterReaction but shorter and punchier.
    /// </summary>
    private IEnumerator PlayReactionShot(string text, float duration)
    {
        // Quick character sprite show
        if (characterSprite != null)
        {
            Sprite sprite = GetCharacterSprite("Neutral");
            if (sprite != null)
            {
                characterSprite.gameObject.SetActive(true);
                characterSprite.sprite = sprite;

                // Quick punch-in
                characterSprite.transform.localScale = Vector3.one * 0.8f;
                characterSprite.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }

        // Quick text flash
        if (cutsceneText != null)
        {
            cutsceneText.text = text;
            cutsceneText.color = emphasizedTextColor;

            // Flash effect
            cutsceneText.transform.localScale = Vector3.one * 1.2f;
            cutsceneText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        // Wait for duration
        yield return new WaitForSeconds(duration);
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Shows the cutscene UI panel.
    /// </summary>
    private void ShowCutsceneUI()
    {
        if (cutscenePanel == null)
        {
            Debug.LogError("[CutsceneTrigger] CutscenePanel not assigned!");
            return;
        }

        cutscenePanel.SetActive(true);

        // Fade in
        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.3f);
        }
    }

    /// <summary>
    /// Hides the cutscene UI panel.
    /// </summary>
    private void HideCutsceneUI()
    {
        if (cutscenePanel == null) return;

        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                cutscenePanel.SetActive(false);
            });
        }
        else
        {
            cutscenePanel.SetActive(false);
        }

        // Reset character sprites
        if (characterSprite != null)
            characterSprite.gameObject.SetActive(false);
        if (secondaryCharacterSprite != null)
            secondaryCharacterSprite.gameObject.SetActive(false);
    }

    #endregion

    #region Inspector Buttons

    [Button("Test TextReveal")]
    private void TestTextReveal()
    {
        if (Application.isPlaying)
        {
            CutsceneData testData = new CutsceneData
            {
                ID = "Test_TextReveal",
                HouseLevel = 1,
                CutsceneType = CutsceneType.TextReveal,
                TextAR = "شربت القهوة وخلصت! ☕",
                Duration = 3f,
                Animation = AnimationType.Typewriter
            };

            PlayCutscene(testData, (id) =>
            {
                Debug.Log($"[CutsceneTrigger Test] Cutscene completed: {id}");
            });
        }
        else
        {
            Debug.LogWarning("[CutsceneTrigger] Enter Play mode to test.");
        }
    }

    [Button("Test CharacterReaction")]
    private void TestCharacterReaction()
    {
        if (Application.isPlaying)
        {
            CutsceneData testData = new CutsceneData
            {
                ID = "Test_Reaction",
                HouseLevel = 1,
                CutsceneType = CutsceneType.CharacterReaction,
                TextAR = "خالة أم محمد ابتسمت! 😊",
                Duration = 2f,
                Animation = AnimationType.Bounce
            };

            PlayCutscene(testData, (id) =>
            {
                Debug.Log($"[CutsceneTrigger Test] Cutscene completed: {id}");
            });
        }
        else
        {
            Debug.LogWarning("[CutsceneTrigger] Enter Play mode to test.");
        }
    }

    [Button("Test CameraPan")]
    private void TestCameraPan()
    {
        if (Application.isPlaying)
        {
            CutsceneData testData = new CutsceneData
            {
                ID = "Test_Pan",
                HouseLevel = 1,
                CutsceneType = CutsceneType.CameraPan,
                TextAR = "دخلت بيت خالة أم محمد",
                Duration = 3f,
                Animation = AnimationType.Slide
            };

            PlayCutscene(testData, (id) =>
            {
                Debug.Log($"[CutsceneTrigger Test] Cutscene completed: {id}");
            });
        }
        else
        {
            Debug.LogWarning("[CutsceneTrigger] Enter Play mode to test.");
        }
    }

    #endregion
}
