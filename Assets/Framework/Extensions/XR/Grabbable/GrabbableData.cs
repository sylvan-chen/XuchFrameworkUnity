namespace XuchFramework.Extensions.XR
{
    public enum GrabbableGrabState
    {
        Idle,
        BeingGrabbed,
        Held,
        BeingReleased,
        Destroying
    }

    public struct GrabbableData
    {
        public GrabbableGrabState GrabState;
    }
}