using System.Collections.Generic;
using UnityEngine;

namespace Framework.Extensions.XR
{
    public class Grabbable : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _body;

        public Rigidbody Body => _body;

        public bool IsGrabbable { get; private set; } = true;

        public List<Collider> GrabColliders { get; internal set; } = new List<Collider>();
        public List<Grabbable> JointedGrabbables { get; internal set; } = new List<Grabbable>();
        public List<Grabbable> GrabChildren { get; internal set; } = new List<Grabbable>();

        public List<Hand> HeldBy { get; private set; } = new List<Hand>();
        public List<Hand> BeingGrabbedBy { get; private set; } = new List<Hand>();
        public List<Hand> WaitingToGrabBy { get; private set; } = new List<Hand>();

        public bool BeingGrabbed { get; private set; } = false;
        public bool BeingDestroyed { get; private set; } = false;

        public Grabbable RootGrabbable { get; internal set; }

        public Transform RootBodyTransform
        {
            get
            {
                if (_body != null)
                    return _body.transform;
                else if (TryGetComponent(out Rigidbody rb))
                    return rb.transform;
                else if (GetComponentInParent<Rigidbody>() != null)
                    return GetComponentInParent<Rigidbody>().transform;
                else
                    return null;
            }
        }
    }
}