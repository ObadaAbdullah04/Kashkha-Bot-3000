using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// Component attached to tutorial prefabs (e.g., Hand Pointer) to manage its animations.
/// </summary>
public class TutorialPointer : MonoBehaviour
{
    [SerializeField] private RectTransform graphicRoot;

    [Header("Positioning Offset")]
    [Tooltip("Manual offset to adjust the pointer's position relative to the target.")]
    [SerializeField] private Vector2 pointerOffset = Vector2.zero;

    public Vector2 PointerOffset => pointerOffset;
    
    private Tween currentAnim;
    private Sequence currentSequence;

    public void PlayAnimation(TutorialAnimationType type)
    {
        StopAnimation();
        
        if (graphicRoot == null) graphicRoot = GetComponent<RectTransform>();
        if (graphicRoot == null) return;

        // PHASE 18: Reset to the manual offset instead of absolute zero
        graphicRoot.localPosition = (Vector3)pointerOffset;
        graphicRoot.localRotation = Quaternion.identity;
        graphicRoot.localScale = Vector3.one;

        Vector3 basePos = (Vector3)pointerOffset;

        switch (type)
        {
            case TutorialAnimationType.Point:
                // Use relative punch or add to basePos
                currentAnim = graphicRoot.DOPunchPosition(new Vector3(0, -30f, 0), 0.6f, 5, 0.5f).SetLoops(-1).SetUpdate(true);
                break;

            case TutorialAnimationType.Swipe:
                currentSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
                
                // SWIPE RIGHT (Offset aware)
                currentSequence.Append(graphicRoot.DOLocalMove(basePos + new Vector3(250f, 60f, 0), 0.8f).SetEase(Ease.OutQuad));
                currentSequence.Join(graphicRoot.DOLocalRotate(new Vector3(0, 0, -25f), 0.8f).SetEase(Ease.OutQuad));
                currentSequence.AppendInterval(0.2f);
                currentSequence.Append(graphicRoot.DOLocalMove(basePos, 0.2f).SetEase(Ease.InSine));
                currentSequence.Join(graphicRoot.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InSine));
                
                // SWIPE LEFT (Offset aware)
                currentSequence.Append(graphicRoot.DOLocalMove(basePos + new Vector3(-250f, 60f, 0), 0.8f).SetEase(Ease.OutQuad));
                currentSequence.Join(graphicRoot.DOLocalRotate(new Vector3(0, 0, 25f), 0.8f).SetEase(Ease.OutQuad));
                currentSequence.AppendInterval(0.2f);
                currentSequence.Append(graphicRoot.DOLocalMove(basePos, 0.2f).SetEase(Ease.InSine));
                currentSequence.Join(graphicRoot.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InSine));
                break;

            case TutorialAnimationType.Shake:
                currentAnim = graphicRoot.DOShakePosition(1f, 30f, 10, 90f).SetLoops(-1).SetUpdate(true);
                break;

            case TutorialAnimationType.Tap:
                currentAnim = graphicRoot.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                break;

            case TutorialAnimationType.Hold:
                currentSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
                currentSequence.Append(graphicRoot.DOScale(1.4f, 0.8f).SetEase(Ease.InOutSine));
                currentSequence.Append(graphicRoot.DOScale(1f, 0.8f).SetEase(Ease.InOutSine));
                break;

            case TutorialAnimationType.Draw:
                currentSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
                currentSequence.Append(graphicRoot.DOLocalMove(basePos + new Vector3(100f, 0, 0), 0.5f).SetEase(Ease.Linear));
                currentSequence.Append(graphicRoot.DOLocalMove(basePos + new Vector3(100f, -100f, 0), 0.5f).SetEase(Ease.Linear));
                currentSequence.Append(graphicRoot.DOLocalMove(basePos + new Vector3(0, -100f, 0), 0.5f).SetEase(Ease.Linear));
                currentSequence.Append(graphicRoot.DOLocalMove(basePos, 0.5f).SetEase(Ease.Linear));
                break;
        }
    }

    public void StopAnimation()
    {
        currentAnim?.Kill();
        currentSequence?.Kill();
        if (graphicRoot != null)
        {
            graphicRoot.DOKill();
            // PHASE 18: Reset to the manual offset instead of absolute zero
            graphicRoot.localPosition = (Vector3)pointerOffset;
            graphicRoot.localRotation = Quaternion.identity;
            graphicRoot.localScale = Vector3.one;
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }
}
