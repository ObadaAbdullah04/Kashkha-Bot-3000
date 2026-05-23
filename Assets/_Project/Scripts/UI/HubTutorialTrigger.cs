using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Triggers a sequence of tutorial overlays in the Hub scene.
/// </summary>
public class HubTutorialTrigger : MonoBehaviour
{
    [Header("Hub Navigation Buttons")]
    [SerializeField] private RectTransform wardrobeTabButton;
    [SerializeField] private RectTransform housesTabButton;

    private void Start()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        
        /* Disable Hub Tutorial for now - Focus on Houses
        // Initial check if we're already in Hub or Wardrobe
        if (GameManager.Instance != null && 
           (GameManager.Instance.CurrentState == GameState.HouseHub || GameManager.Instance.CurrentState == GameState.Wardrobe))
        {
            TryStartTutorial();
        }
        */
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        /* Disable Hub Tutorial for now - Focus on Houses
        if (newState == GameState.HouseHub || newState == GameState.Wardrobe)
        {
            TryStartTutorial();
        }
        */
    }

    private void TryStartTutorial()
    {
        if (SaveManager.Instance == null) return;

        bool hasSeen = SaveManager.Instance.HasSeenTutorial("HubWalkthrough");
        // Debug.Log($"[HubTutorial] Checking tutorial. HasSeen: {hasSeen}");

        if (!hasSeen)
        {
            StartCoroutine(HubWalkthroughRoutine());
        }
    }

    private IEnumerator HubWalkthroughRoutine()
    {
        // Debug.Log("[HubTutorial] Starting walkthrough routine...");

        // Step 1: Tell user to go to Wardrobe
        if (wardrobeTabButton != null)
        {
            TutorialOverlayManager.Instance.ShowTutorial(wardrobeTabButton, "Open your Wardrobe to change your look!", false);
            
            // Wait until the Wardrobe panel is actually active
            yield return new WaitUntil(() => UnifiedHubManager.Instance != null && 
                                            UnifiedHubManager.Instance.ActiveTab == UnifiedHubManager.HubTab.Wardrobe);
            
            TutorialOverlayManager.Instance.HideTutorial();
        }
        else
        {
            // Debug.LogWarning("[HubTutorial] Wardrobe Tab Button reference missing!");
        }

        yield return new WaitForSeconds(0.5f);

        // Step 2: Now tell them to go back to Houses
        if (housesTabButton != null)
        {
            TutorialOverlayManager.Instance.ShowTutorial(housesTabButton, "Now, go back to Houses to start your visit!", false);
            
            yield return new WaitUntil(() => UnifiedHubManager.Instance != null && 
                                            UnifiedHubManager.Instance.ActiveTab == UnifiedHubManager.HubTab.Houses);
            
            TutorialOverlayManager.Instance.HideTutorial();
        }
        else
        {
            // Debug.LogWarning("[HubTutorial] Houses Tab Button reference missing!");
        }

        SaveManager.Instance.MarkTutorialAsComplete("HubWalkthrough");
        // Debug.Log("[HubTutorial] Walkthrough complete.");
    }
}
