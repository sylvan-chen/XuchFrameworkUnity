using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Extensions.XR
{
    public delegate void CollisionEvent(GameObject from);

    /// <summary>
    /// This is a component designed to replace Unity’s built-in collision events, with more robust and reliable logic
    /// 1. It guarantees that Exit events will not be missed when an object is disabled or destroyed
    /// 2. It automatically deduplicates events for compound colliders (objects with multiple colliders), triggering only FirstEnter once
    /// 3. NOTE: It has a HIGHER performance cost. Please use it at your discretion
    /// </summary>
    public class ReliableCollisionTracker : MonoBehaviour
    {
        private const int MAX_COLLISIONS_TRACKED = 256;

        public bool DisableCollisionTracking = false;
        public bool DisableTriggerTracking = false;

        public event CollisionEvent OnCollisionFirstEnter;
        public event CollisionEvent OnCollisionFirstExit;
        public event CollisionEvent OnTriggerFirstEnter;
        public event CollisionEvent OnTriggerFirstExit;

        public int CollisionCount => CollisionObjects.Count;
        public int TriggerCount => TriggerObjects.Count;

        public List<GameObject> TriggerObjects { get; protected set; } = new List<GameObject>(MAX_COLLISIONS_TRACKED);
        public List<GameObject> NextTriggerObjects { get; protected set; } = new List<GameObject>(MAX_COLLISIONS_TRACKED);

        public List<GameObject> CollisionObjects { get; protected set; } = new List<GameObject>(MAX_COLLISIONS_TRACKED);
        public List<GameObject> NextCollisionObjects { get; protected set; } = new List<GameObject>(MAX_COLLISIONS_TRACKED);

        private List<Collision> _collisions { get; set; } = new List<Collision>(MAX_COLLISIONS_TRACKED);

        private UniTask _lateFixedUpdate;
        private CancellationTokenSource _cts;

        public void Clear()
        {
            TriggerObjects.Clear();
            NextTriggerObjects.Clear();
            CollisionObjects.Clear();
            NextCollisionObjects.Clear();
            _collisions.Clear();
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            _lateFixedUpdate = LateFixedUpdate(_cts.Token);
        }

        private void OnDisable()
        {
            for (int i = 0; i < CollisionObjects.Count; i++)
            {
                if (CollisionObjects[i])
                {
                    OnCollisionFirstExit?.Invoke(CollisionObjects[i]);
                }
            }

            for (int i = 0; i < TriggerObjects.Count; i++)
            {
                if (TriggerObjects[i])
                {
                    OnTriggerFirstExit?.Invoke(TriggerObjects[i]);
                }
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private async UniTask LateFixedUpdate(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                await UniTask.WaitForFixedUpdate(cancellationToken);

                CheckTrackedObjects();
            }

            void CheckTrackedObjects()
            {
                if (!DisableCollisionTracking)
                {
                    for (int i = 0; i < CollisionObjects.Count; i++)
                    {
                        var collisionObject = CollisionObjects[i];
                        if (!collisionObject.activeInHierarchy || !NextCollisionObjects.Contains(collisionObject))
                        {
                            OnCollisionFirstExit?.Invoke(collisionObject);
                        }
                    }

                    for (int i = 0; i < NextCollisionObjects.Count; i++)
                    {
                        
                    }
                }
            }
        }
    }
}