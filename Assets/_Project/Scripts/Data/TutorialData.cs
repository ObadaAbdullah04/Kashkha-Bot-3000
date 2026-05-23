using UnityEngine;
using System.Collections.Generic;

public enum TutorialAnimationType
{
    None,
    Point,
    Swipe,
    Shake,
    Hold,
    Tap,
    Draw
}

[System.Serializable]
public class TutorialStepData
{
    public string TutorialID;
    public int StepIndex;
    public string TargetID;
    public string InstructionAR;
    public bool RequireTargetClick;
    public float TimeScale = 0f;
    public TutorialAnimationType AnimationType = TutorialAnimationType.None;
}
