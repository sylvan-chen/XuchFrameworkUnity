using UnityEngine;

namespace Framework.Extensions.XR
{
    public enum GrabType
    {
        /// <summary>On grab, hand will move to the grabbable, create grab connection, then return to follow position</summary>
        HandToGrabbable,
        /// <summary>On grab, grabbable will move to the hand, then create grab connection</summary>
        GrabbableToHand,
        /// <summary>On grab, grabbable instantly travel to the hand</summary>
        InstantGrab,
    }

    // public delegate void HandGrabEvent(Hand hand, Grabbable grabbable);

    public class Hand : MonoBehaviour { }
}