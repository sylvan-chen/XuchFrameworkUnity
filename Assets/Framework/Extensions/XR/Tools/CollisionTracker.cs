using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Extensions.XR
{
    public delegate void CollisionEvent(GameObject from);

    public class CollisionTracker : MonoBehaviour
    {
        public event CollisionEvent OnCollisionFirstEnter;
        public event CollisionEvent OnCollisionFirstExit;
        public event CollisionEvent OnTriggerFirstEnter;
        public event CollisionEvent OnTriggerFirstExit;

        private void OnEnable() { }

        private void OnDisable() { }

        private async void LateFixedUpdate()
        {
            while (true)
            {
                await UniTask.WaitForFixedUpdate();

                CheckTrackedObjects();
            }

            void CheckTrackedObjects() { }
        }
    }
}