using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
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
        private const int TRACKING_CAPACITY = 256;

        public bool DisableCollisionTracking = false;
        public bool DisableTriggerTracking = false;

        public event CollisionEvent OnCollisionFirstEnter;
        public event CollisionEvent OnCollisionLastExit;
        public event CollisionEvent OnTriggerFirstEnter;
        public event CollisionEvent OnTriggerLastExit;

        public int CollisionCount => _collisionObjects.Count;
        public int TriggerCount => _triggerObjects.Count;

        private readonly List<GameObject> _triggerObjects = new List<GameObject>(TRACKING_CAPACITY);
        private readonly List<GameObject> _nextTriggerObjects = new List<GameObject>(TRACKING_CAPACITY);

        private readonly List<GameObject> _collisionObjects = new List<GameObject>(TRACKING_CAPACITY);
        private readonly List<GameObject> _nextCollisionObjects = new List<GameObject>(TRACKING_CAPACITY);

        private CancellationTokenSource _lateFixedUpdateCts;

        public void Clear()
        {
            _triggerObjects.Clear();
            _nextTriggerObjects.Clear();
            _collisionObjects.Clear();
            _nextCollisionObjects.Clear();
        }

        private void OnEnable()
        {
            _lateFixedUpdateCts = new CancellationTokenSource();
            LateFixedUpdate(_lateFixedUpdateCts.Token).Forget();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _collisionObjects.Count; i++)
            {
                if (_collisionObjects[i])
                {
                    OnCollisionLastExit?.Invoke(_collisionObjects[i]);
                }
            }

            for (int i = 0; i < _triggerObjects.Count; i++)
            {
                if (_triggerObjects[i])
                {
                    OnTriggerLastExit?.Invoke(_triggerObjects[i]);
                }
            }

            _lateFixedUpdateCts.Cancel();
            _lateFixedUpdateCts.Dispose();
        }

        private void OnDestroy()
        {
            if (_lateFixedUpdateCts != null)
            {
                _lateFixedUpdateCts.Cancel();
                _lateFixedUpdateCts.Dispose();
            }
        }

        private async UniTaskVoid LateFixedUpdate(CancellationToken cancellationToken = default)
        {
            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate(PlayerLoopTiming.LastFixedUpdate).WithCancellation(cancellationToken))
            {
                CheckCollisionTracking();
                CheckTriggerTracking();
            }

            void CheckCollisionTracking()
            {
                if (DisableCollisionTracking) return;

                for (int i = 0; i < _collisionObjects.Count; i++)
                {
                    var collisionObject = _collisionObjects[i];
                    if (!collisionObject.activeInHierarchy || !_nextCollisionObjects.Contains(collisionObject))
                    {
                        OnCollisionLastExit?.Invoke(collisionObject);
                    }
                }

                for (int i = _nextCollisionObjects.Count - 1; i >= 0; i--)
                {
                    var nextCollisionObject = _nextCollisionObjects[i];
                    if (nextCollisionObject == null || !nextCollisionObject.activeInHierarchy)
                    {
                        _nextCollisionObjects.RemoveAt(i);
                    }
                    else if (!_collisionObjects.Contains(nextCollisionObject))
                    {
                        OnCollisionFirstEnter?.Invoke(nextCollisionObject);
                    }
                }

                _collisionObjects.Clear();
                _collisionObjects.AddRange(_nextCollisionObjects);
                _nextCollisionObjects.Clear();
            }

            void CheckTriggerTracking()
            {
                if (DisableTriggerTracking) return;

                for (int i = 0; i < _triggerObjects.Count; i++)
                {
                    var triggerObject = _triggerObjects[i];
                    if (!triggerObject.activeInHierarchy || !_nextTriggerObjects.Contains(triggerObject))
                    {
                        OnTriggerLastExit?.Invoke(triggerObject);
                    }
                }

                for (int i = _nextTriggerObjects.Count - 1; i >= 0; i--)
                {
                    var nextTriggerObject = _nextTriggerObjects[i];
                    if (nextTriggerObject == null || !nextTriggerObject.activeInHierarchy)
                    {
                        _nextTriggerObjects.RemoveAt(i);
                    }
                    else if (!_triggerObjects.Contains(nextTriggerObject))
                    {
                        OnTriggerFirstEnter?.Invoke(nextTriggerObject);
                    }
                }

                _triggerObjects.Clear();
                _triggerObjects.AddRange(_nextTriggerObjects);
                _nextTriggerObjects.Clear();
            }
        }

        private void OnCollisionStay(Collision other)
        {
            if (DisableCollisionTracking) return;

            if (!_collisionObjects.Contains(other.collider.gameObject))
            {
                _nextCollisionObjects.Add(other.collider.gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (DisableTriggerTracking) return;

            if (!_triggerObjects.Contains(other.gameObject))
            {
                _nextTriggerObjects.Add(other.gameObject);
            }
        }
    }
}