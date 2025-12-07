using Autohand;
using UnityEngine;

namespace DigiEden.Gameplay
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
    }
}