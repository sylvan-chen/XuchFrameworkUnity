using Autohand;
using RootMotion.FinalIK;
using UnityEngine;

namespace XuchFramework.Extensions.Movement
{
    public class HandPlayerConfig : MonoBehaviour
    {
        [Header("References")]
        public Transform trackingContainer;
        public Transform controllerLeft;
        public Transform controllerRight;
        public Camera headCamera;
        public Hand leftHand;
        public Hand rightHand;
        public Transform followerContainer;
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;
        public LimbIK leftArmIK;
        public LimbIK rightArmIK;
    }
}