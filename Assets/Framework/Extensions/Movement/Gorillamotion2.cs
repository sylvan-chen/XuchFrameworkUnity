using Autohand;
using Alchemy.Inspector;
using UnityEngine;

namespace XuchFramework.Gameplay
{
    public class Gorillamotion2 : MonoBehaviour
    {
        public const float GORILLA_TOUCHING_PRECISION = 0.995f;

        [AutoHeader("Gorillamotion", "gorilla_logo")]
        public bool IgnoreMe;

        [Space(5)]
        [SerializeField, Tooltip("The left hand")]
        public Transform _leftHand;
        [SerializeField, Tooltip("The right hand")]
        public Transform _rightHand;
        [SerializeField, Tooltip("Radius of the hand sphere used for surface detection")]
        private float _handSphereRadius = 0.05f;
        [SerializeField, Tooltip("The layers that count as surface for gorilla locomotion")]
        private LayerMask _surfaceLayerMask = ~0;

        [AutoToggleHeader("Movement")]
        public bool EnableMovement = true;
        [SerializeField, EnableIf(nameof(EnableMovement))]
        private bool _limitArmLength = false;
        [SerializeField, Indent, EnableIf(nameof(EnableMovement), nameof(_limitArmLength)),
         Tooltip("Left shoulder transform for arm length calculation")]
        private Transform _leftShoulder;
        [SerializeField, Indent, EnableIf(nameof(EnableMovement), nameof(_limitArmLength)),
         Tooltip("Right shoulder transform for arm length calculation")]
        private Transform _rightShoulder;
        [SerializeField, Indent, EnableIf(nameof(EnableMovement), nameof(_limitArmLength)), Tooltip("Max arm length on moving")]
        private float _maxArmLength = 1.5f;
        [SerializeField, EnableIf(nameof(EnableMovement))]
        private bool _checkHeadClipping = false;
        [SerializeField, Indent, EnableIf(nameof(EnableMovement), nameof(_checkHeadClipping))]
        private SphereCollider _headCollider;
        [SerializeField, EnableIf(nameof(EnableMovement))]
        private float _gravityStength = 10f;
        [SerializeField, Tooltip("Distance between frames threshold to unstick from a surface")]
        private float _unstickDistance = 1f;
        [SerializeField, Tooltip("The default slide factor"), Range(0f, 1f)]
        private float _defaultSlideFactor = 0.03f;

        [AutoToggleHeader("Jumping")]
        public bool EnableJumping = true;
        [SerializeField, EnableIf(nameof(EnableJumping)), Tooltip("Velocity threshold to trigger a jump")]
        private float _toJumpVelocity = 0.4f;
        [SerializeField, EnableIf(nameof(EnableJumping)), Tooltip("Strength for the jump velocity")]
        private float _jumpingStrength = 1.1f;
        [SerializeField, EnableIf(nameof(EnableJumping)), Tooltip("Maximum jump speed")]
        private float _maxJumpSpeed = 6.5f;
        [SerializeField, EnableIf(nameof(EnableJumping)), Tooltip("Size of the velocity history used to determine if the player trigger a jump")]
        [Range(3, 10)]
        private int _velocityHistorySize = 8;

        [AutoToggleHeader("Haptics")]
        public bool AllowTouchingHaptics = true;
        [SerializeField, Indent, EnableIf(nameof(AllowTouchingHaptics))]
        private float _touchingHapticsCoolDown = 0.15f;
        [SerializeField, Indent, EnableIf(nameof(AllowTouchingHaptics))]
        private float _touchingHapticDuration = 0.05f;
        [SerializeField, Indent, EnableIf(nameof(AllowTouchingHaptics))]
        private float _touchingHapticAmplitude = 0.5f;
        [SerializeField, Indent, EnableIf(nameof(AllowTouchingHaptics))]
        private float _slidingHapticAmplitude = 0.5f;

        private Vector3 _lastHeadPos;
        private Vector3 _lastBodyPos;
        private Vector3 _lastUpdatePos;
        private Vector3 _lastLeftHandPos, _lastRightHandPos;
        private float _lastLeftCollideTime = 0f, _lastRightCollideTime = 0f;

        private int _velocityIndex = 0;
        private Vector3[] _velocityHistory;
        private Vector3 _denormalizedVelocityAverage = Vector3.zero;

        private RaycastHit[] _hitsNonAlloc5 = new RaycastHit[5];
        private int _lastHitsCount = 0;
        private bool _hasInitTracking = false;

        public Transform LeftHand => _leftHand;
        public Transform RightHand => _rightHand;
        public Rigidbody Body { get; private set; }

        public bool IsJumping { get; private set; }
        public bool IsHandTouching => IsLeftHandTouching || IsRightHandTouching;
        public bool IsLeftHandTouching { get; private set; } = false;
        public bool IsRightHandTouching { get; private set; } = false;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();

            _velocityHistory = new Vector3[_velocityHistorySize];

#if UNITY_EDITOR
            if (EnableDebug)
            {
                InitializeDebugTransforms();
            }
#endif
        }

        private void Start()
        {
            if (_hasInitTracking)
                return;

            _lastUpdatePos = transform.position;

            if (_headCollider != null && _checkHeadClipping)
                _lastHeadPos = _headCollider.transform.position;

            if (_leftHand && _rightHand)
            {
                _lastLeftHandPos = _leftHand.transform.position;
                _lastRightHandPos = _rightHand.transform.position;
                _hasInitTracking = true;
            }
        }

        public void ApplyGorillamotion()
        {
            bool isLeftHandColliding = false, isRightHandColliding = false;

            Vector3 bodyMovement;
            var leftHandMovement = Vector3.zero;
            var rightHandMovement = Vector3.zero;

            var currentLeftHandPos = GetHandPosition(_leftHand);
            var currentRightHandPos = GetHandPosition(_rightHand);

            var gravityOffset = Physics.gravity * (_gravityStength * Time.deltaTime * Time.deltaTime);

            // Apply hand movement

            var leftGravityOffset = IsLeftHandTouching ? gravityOffset : Vector3.zero;
            var traveledVector = currentLeftHandPos - _lastLeftHandPos + leftGravityOffset;
            if (ApplySphereMovement(_lastLeftHandPos, _handSphereRadius, traveledVector, out var finalLeftHandPosition, out _, true, false, out _))
            {
                // this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
                if (IsLeftHandTouching)
                {
                    leftHandMovement = _lastLeftHandPos - currentLeftHandPos;
                }
                else
                {
                    leftHandMovement = finalLeftHandPosition - currentLeftHandPos;
                    _lastLeftHandPos = finalLeftHandPosition;
                }

                isLeftHandColliding = true;
                Body.linearVelocity = Vector3.zero;
            }

            var rightGravityOffset = IsRightHandTouching ? gravityOffset : Vector3.zero;
            traveledVector = currentRightHandPos - _lastRightHandPos + rightGravityOffset;
            if (ApplySphereMovement(_lastRightHandPos, _handSphereRadius, traveledVector, out var finalRightHandPos, out _, true, false, out _))
            {
                if (IsRightHandTouching)
                {
                    rightHandMovement = _lastRightHandPos - currentRightHandPos;
                }
                else
                {
                    rightHandMovement = finalRightHandPos - currentRightHandPos;
                    _lastRightHandPos = finalRightHandPos;
                }

                isRightHandColliding = true;
                Body.linearVelocity = Vector3.zero;
            }

#if UNITY_EDITOR
            if (EnableDebug)
            {
                if (_visualizeLastHandPosition)
                {
                    _lastLeftHandCube.position = _lastLeftHandPos;
                    _lastRightHandCube.position = _lastRightHandPos;
                }

                if (_visualizeFinalPosition)
                {
                    if (isLeftHandColliding)
                    {
                        _leftFinalPosition.gameObject.SetActive(true);
                        _leftFinalPosition.position = finalLeftHandPosition;
                    }
                    else
                    {
                        _leftFinalPosition.gameObject.SetActive(false);
                    }

                    if (isRightHandColliding)
                    {
                        _rightFinalPosition.gameObject.SetActive(true);
                        _rightFinalPosition.position = finalRightHandPos;
                    }
                    else
                    {
                        _rightFinalPosition.gameObject.SetActive(false);
                    }
                }
            }
#endif

            // Apply body movement

            if ((isLeftHandColliding || IsLeftHandTouching) && (isRightHandColliding || IsRightHandTouching))
            {
                // this lets you touch surface with both hands at the same time
                bodyMovement = (leftHandMovement + rightHandMovement) / 2;
            }
            else
            {
                bodyMovement = leftHandMovement + rightHandMovement;
            }

            if (bodyMovement != Vector3.zero)
            {
                PreventHeadClipping();

                transform.position += bodyMovement;
                Body.position = transform.position;

                if (_checkHeadClipping)
                    _lastHeadPos = _headCollider.transform.position;
            }

#if UNITY_EDITOR
            if (EnableDebug && _visualizeTravelVector)
            {
                _leftTravelVector.position = currentLeftHandPos;
                _rightTravelVector.position = currentRightHandPos;
            }
#endif

            // Final hand position

            currentLeftHandPos = GetHandPosition(_leftHand);
            currentRightHandPos = GetHandPosition(_rightHand);

            if (!isLeftHandColliding)
            {
                _lastLeftHandPos = currentLeftHandPos;
            }

            if (!isRightHandColliding)
            {
                _lastRightHandPos = currentRightHandPos;
            }

            if (isLeftHandColliding || isRightHandColliding)
                Body.useGravity = false;
            else
                Body.useGravity = true;

            // Check unstick

            if (isLeftHandColliding && (currentLeftHandPos - _lastLeftHandPos).magnitude > _unstickDistance)
            {
                isLeftHandColliding = false;
                _lastLeftHandPos = currentLeftHandPos;
            }

            if (isRightHandColliding && (currentRightHandPos - _lastRightHandPos).magnitude > _unstickDistance)
            {
                isRightHandColliding = false;
                _lastRightHandPos = currentRightHandPos;
            }

            // Post process

            PlayHaptics();

            IsLeftHandTouching = isLeftHandColliding;
            IsRightHandTouching = isRightHandColliding;

            Vector3 GetHandPosition(Transform hand)
            {
                if (_limitArmLength)
                {
                    var shoulder = hand == _leftHand ? _leftShoulder : _rightShoulder;
                    if ((hand.position - shoulder.position).magnitude >= _maxArmLength)
                        return shoulder.position + (hand.position - shoulder.position).normalized * _maxArmLength;
                }

                return hand.position;
            }

            void PreventHeadClipping()
            {
                if (!_checkHeadClipping)
                    return;

                traveledVector = _headCollider.transform.position + bodyMovement - _lastHeadPos;
                if (ApplySphereMovement(_lastHeadPos, _headCollider.radius, traveledVector, out var finalPosition, out _, false, true, out _))
                {
                    bodyMovement = finalPosition - _lastHeadPos;

                    // last check to make sure the head won't phase through geometry
                    if (Physics.Raycast(
                            _lastHeadPos,
                            traveledVector,
                            out _,
                            traveledVector.magnitude + _headCollider.radius * GORILLA_TOUCHING_PRECISION * 0.999f,
                            _surfaceLayerMask.value))
                    {
                        // If head is colliding, revert to previous position
                        bodyMovement = _lastHeadPos - _headCollider.transform.position;
                    }
                }
            }

            void PlayHaptics()
            {
                var currentRealtime = Time.realtimeSinceStartup;

                if (isLeftHandColliding
                    && IsLeftHandTouching != isLeftHandColliding
                    && currentRealtime - _lastLeftCollideTime > _touchingHapticsCoolDown)
                {
                    HandPlayer.Instance.HandLeft.PlayHapticVibration(_touchingHapticDuration, _touchingHapticAmplitude);
                    _lastLeftCollideTime = currentRealtime;
                }

                if (isRightHandColliding
                    && IsRightHandTouching != isRightHandColliding
                    && currentRealtime - _lastRightCollideTime > _touchingHapticsCoolDown)
                {
                    HandPlayer.Instance.HandRight.PlayHapticVibration(_touchingHapticDuration, _touchingHapticAmplitude);
                    _lastRightCollideTime = currentRealtime;
                }
            }
        }

        public void Jump()
        {
            IsJumping = false;
            StoreVelocities();

            if (!IsHandTouching)
                return;

            if (_denormalizedVelocityAverage.magnitude > _toJumpVelocity)
            {
                IsJumping = true;

                if (_denormalizedVelocityAverage.magnitude * _jumpingStrength > _maxJumpSpeed)
                    Body.linearVelocity = _denormalizedVelocityAverage.normalized * _maxJumpSpeed;
                else
                    Body.linearVelocity = _jumpingStrength * _denormalizedVelocityAverage;
            }

            void StoreVelocities()
            {
                _velocityIndex = (_velocityIndex + 1) % _velocityHistorySize;
                Vector3 oldestVelocity = _velocityHistory[_velocityIndex];
                var currentVelocity = (transform.position - _lastUpdatePos) / Time.fixedDeltaTime;
                _denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)_velocityHistorySize;
                _velocityHistory[_velocityIndex] = currentVelocity;
                _lastUpdatePos = transform.position;
            }
        }

        /// <summary>Move sphere with collision check and apply sliding</summary>
        /// <returns>Does sphere hit on surface</returns>
        private bool ApplySphereMovement(
            Vector3 startPosition, float sphereRadius, Vector3 movementVector, out Vector3 finalPosition, out RaycastHit finalHitInfo,
            bool singleHand, bool fullSlide, out float finalSlideFactor)
        {
            finalSlideFactor = _defaultSlideFactor;

            // Cast 1: Direct movement cast
            if (!AccurateSphereCast(startPosition, sphereRadius, movementVector, out finalPosition, out finalHitInfo, _surfaceLayerMask.value))
            {
                // If the movement path is completely clear, do nothing
                return false;
            }

            // Get slide factor
            if (finalHitInfo.collider.gameObject.TryGetComponent(out SurfaceOverride surfaceOverride))
            {
                finalSlideFactor = surfaceOverride.slideFactor <= _defaultSlideFactor ? _defaultSlideFactor : surfaceOverride.slideFactor;
            }
            else
            {
                // Use tiny slide factor for single hand to keep hand stuck on surface
                finalSlideFactor = singleHand ? 0.001f : _defaultSlideFactor;
            }

            if (fullSlide)
                finalSlideFactor = 1f;

            // Get the project vector on collision plane (first hit -> target) and apply slide factor
            var firstHitPosition = finalPosition;
            var targetPosition = startPosition + movementVector;
            // The bigger slide factor, the more motion on surface is allowed
            var surfaceMovement = Vector3.ProjectOnPlane(targetPosition - firstHitPosition, finalHitInfo.normal) * finalSlideFactor;

            // Cast 2: Surface movement cast
            if (AccurateSphereCast(firstHitPosition, sphereRadius, surfaceMovement, out finalPosition, out finalHitInfo, _surfaceLayerMask.value))
            {
                return true;
            }

            // Cast 3: Finish the remaining movement
            if (AccurateSphereCast(
                    firstHitPosition + surfaceMovement,
                    sphereRadius,
                    targetPosition - (firstHitPosition + surfaceMovement),
                    out finalPosition,
                    out finalHitInfo,
                    _surfaceLayerMask.value))
            {
                return true;
            }

            // No collision
            finalPosition = startPosition;
            return false;
        }

        /// <summary>
        /// Perform sphere casts to find the final position when moving sphere hit on surface
        /// </summary>
        /// <returns>Does sphere hit on surface</returns>
        private bool AccurateSphereCast(
            Vector3 startPosition, float sphereRadius, Vector3 movementVector, out Vector3 finalPosition, out RaycastHit finalHitInfo,
            int layerMask = ~0)
        {
            RaycastHit tempHitInfo;

            // Ensure sphere is non-overlap at the start position
            AutoHandExtensions.TryGetMaxSphereRadiusForNonOverlap(startPosition, sphereRadius, sphereRadius * 0.75f, out var finalRadius1, layerMask);

            // Cast 1.1: Movement vector cast
            ClearHitBuffer(ref _hitsNonAlloc5);
            _lastHitsCount = Physics.SphereCastNonAlloc(
                startPosition,
                finalRadius1,
                movementVector.normalized,
                _hitsNonAlloc5,
                movementVector.magnitude,
                layerMask);

            if (_lastHitsCount > 0)
            {
                // Find the closest hit point
                tempHitInfo = _hitsNonAlloc5[0];
                for (int i = 0; i < _lastHitsCount; i++)
                {
                    if (_hitsNonAlloc5[i].distance < tempHitInfo.distance)
                    {
                        tempHitInfo = _hitsNonAlloc5[i];
                    }
                }

                finalHitInfo = tempHitInfo;
                finalPosition = finalHitInfo.point + finalHitInfo.normal * sphereRadius;

                // If hit movement is short and nearly vertical, direct return without complex calculation
                if (IsSimpleCollision(finalHitInfo, movementVector))
                    return true;

                // Cast 1.2: Raycast to avoid miss through thin objects
                ClearHitBuffer(ref _hitsNonAlloc5);
                _lastHitsCount = Physics.RaycastNonAlloc(
                    startPosition,
                    (finalPosition - startPosition).normalized,
                    _hitsNonAlloc5,
                    (finalPosition - startPosition).magnitude,
                    layerMask,
                    QueryTriggerInteraction.Ignore);

                if (_lastHitsCount > 0)
                {
                    tempHitInfo = _hitsNonAlloc5[0];
                    for (int i = 0; i < _lastHitsCount; i++)
                    {
                        if (_hitsNonAlloc5[i].distance < tempHitInfo.distance)
                        {
                            tempHitInfo = _hitsNonAlloc5[i];
                        }
                    }

                    finalPosition = startPosition + movementVector.normalized * tempHitInfo.distance;
                }

                // Cast 1.3: Verify the final position is non-overlap
                AutoHandExtensions.TryGetMaxSphereRadiusForNonOverlap(
                    finalPosition,
                    sphereRadius,
                    sphereRadius * 0.75f,
                    out var finalRadius2,
                    layerMask);

                // Find the exact final position with smaller sphere radius
                ClearHitBuffer(ref _hitsNonAlloc5);
                _lastHitsCount = Physics.SphereCastNonAlloc(
                    startPosition,
                    Mathf.Min(finalRadius1, finalRadius2),
                    (finalPosition - startPosition).normalized,
                    _hitsNonAlloc5,
                    (finalPosition - startPosition).magnitude,
                    layerMask);

                if (_lastHitsCount > 0)
                {
                    tempHitInfo = _hitsNonAlloc5[0];
                    for (int i = 0; i < _lastHitsCount; i++)
                    {
                        if (_hitsNonAlloc5[i].collider != null && _hitsNonAlloc5[i].distance < tempHitInfo.distance)
                        {
                            tempHitInfo = _hitsNonAlloc5[i];
                        }
                    }

                    finalHitInfo = tempHitInfo;
                    finalPosition = startPosition + tempHitInfo.distance * (finalPosition - startPosition).normalized;
                }

                // Cast 1.4: Final raycast to avoid miss through thin objects
                // Return the final position to start position if there are missing colliders
                // This is a simple but reliable strategy to avoid incorrect hit for complex surfaces like edges and corners
                ClearHitBuffer(ref _hitsNonAlloc5);
                _lastHitsCount = Physics.RaycastNonAlloc(
                    startPosition,
                    (finalPosition - startPosition).normalized,
                    _hitsNonAlloc5,
                    (finalPosition - startPosition).magnitude,
                    layerMask,
                    QueryTriggerInteraction.Ignore);

                if (_lastHitsCount > 0)
                {
                    tempHitInfo = _hitsNonAlloc5[0];
                    for (int i = 0; i < _lastHitsCount; i++)
                    {
                        if (_hitsNonAlloc5[i].distance < tempHitInfo.distance)
                        {
                            tempHitInfo = _hitsNonAlloc5[i];
                        }
                    }

                    finalHitInfo = tempHitInfo;
                    finalPosition = startPosition;
                }

                return true;
            }

            // Cast 2: Backup raycast if sphere cast misses
            ClearHitBuffer(ref _hitsNonAlloc5);
            _lastHitsCount = Physics.RaycastNonAlloc(
                startPosition,
                movementVector.normalized,
                _hitsNonAlloc5,
                movementVector.magnitude,
                layerMask,
                QueryTriggerInteraction.Ignore);

            if (_lastHitsCount > 0)
            {
                tempHitInfo = _hitsNonAlloc5[0];
                for (int i = 0; i < _lastHitsCount; i++)
                {
                    if (_hitsNonAlloc5[i].collider != null && _hitsNonAlloc5[i].distance < tempHitInfo.distance)
                    {
                        tempHitInfo = _hitsNonAlloc5[i];
                    }
                }

                finalHitInfo = tempHitInfo;
                finalPosition = startPosition;

                return true;
            }

            // No hitting
            finalHitInfo = default;
            finalPosition = startPosition + movementVector;

            return false;

            void ClearHitBuffer(ref RaycastHit[] hits)
            {
                for (int i = 0; i < _lastHitsCount; i++)
                {
                    hits[i] = default;
                }
            }

            bool IsSimpleCollision(RaycastHit hit, Vector3 movement)
            {
                return Vector3.Dot(hit.normal, -movement.normalized) > 0.9f && movement.sqrMagnitude < (sphereRadius * 2f) * (sphereRadius * 2f);
            }
        }

#if UNITY_EDITOR
        [AutoToggleHeader("Debug"), OnValueChanged("OnEnableDebugChanged")]
        public bool EnableDebug = false;
        [SerializeField, EnableIf(nameof(EnableDebug))]
        private bool _visualizeTravelVector = true;
        [SerializeField, EnableIf(nameof(EnableDebug))]
        private bool _visualizeFinalPosition = true;
        [SerializeField, EnableIf(nameof(EnableDebug))]
        private bool _visualizeLastHandPosition = true;

        // For debug
        private Transform _leftTravelVector;
        private Transform _leftFinalPosition;
        private Transform _rightTravelVector;
        private Transform _rightFinalPosition;
        private Transform _lastRightHandCube;
        private Transform _lastLeftHandCube;

        private void OnEnableDebugChanged(bool value)
        {
            if (value)
            {
                ClearDebugTransforms();
                InitializeDebugTransforms();
            }
            else
            {
                ClearDebugTransforms();
            }
        }

        private void InitializeDebugTransforms()
        {
            if (!Application.isPlaying)
                return;

            if (_visualizeTravelVector)
            {
                var traverVector = Resources.Load<GameObject>("gorillamotion_travel_vector").transform;
                _leftTravelVector = Instantiate(traverVector);
                _rightTravelVector = Instantiate(traverVector);
            }

            if (_visualizeFinalPosition)
            {
                var finalPosition = Resources.Load<GameObject>("gorillamotion_final_position").transform;
                _leftFinalPosition = Instantiate(finalPosition);
                _rightFinalPosition = Instantiate(finalPosition);
            }

            if (_visualizeLastHandPosition)
            {
                var lastHandCube = Resources.Load<GameObject>("gorillamotion_last_hand_pos").transform;
                _lastLeftHandCube = Instantiate(lastHandCube);
                _lastRightHandCube = Instantiate(lastHandCube);
            }
        }

        private void ClearDebugTransforms()
        {
            if (!Application.isPlaying)
                return;

            DestroyImmediate(_leftTravelVector.gameObject);
            DestroyImmediate(_rightTravelVector.gameObject);
            DestroyImmediate(_leftFinalPosition.gameObject);
            DestroyImmediate(_rightFinalPosition.gameObject);
            DestroyImmediate(_lastLeftHandCube.gameObject);
            DestroyImmediate(_lastRightHandCube.gameObject);
        }
#endif
    }
}