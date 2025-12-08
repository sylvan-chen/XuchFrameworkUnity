using Autohand;
using Alchemy.Inspector;
using UnityEngine;

namespace DigiEden.Gameplay
{
    [System.Obsolete]
    [DefaultExecutionOrder(4999)]
    public class Gorillamotion : MonoBehaviour
    {
        public const float GORILLA_TOUCHING_PRECISION = 0.995f;

        [Title("General")]
        [SerializeField, Tooltip("The left hand")]
        public Transform _leftHand;
        [SerializeField, Tooltip("The right hand")]
        public Transform _rightHand;
        [SerializeField]
        private CapsuleCollider _bodyCollider;
        [SerializeField]
        private bool _useHeadCollider = false;
        [SerializeField, EnableIf(nameof(_useHeadCollider))]
        private SphereCollider _headCollider;

        [Title("Movement")]
        [SerializeField]
        private float _gravityStrength = 10f;
        [SerializeField]
        private bool _limitArmLength = true;
        [SerializeField, Indent, EnableIf(nameof(_limitArmLength)), Tooltip("Left shoulder transform for arm length calculation")]
        private Transform _leftShoulder;
        [SerializeField, Indent, EnableIf(nameof(_limitArmLength)), Tooltip("Right shoulder transform for arm length calculation")]
        private Transform _rightShoulder;
        [SerializeField, Indent, EnableIf(nameof(_limitArmLength)), Tooltip("Max arm length on moving")]
        private float _maxArmLength = 1.5f;
        [SerializeField, Tooltip("Distance between frames threshold to unstick from a surface")]
        private float _unstickDistance = 1f;
        [SerializeField, Tooltip("The default slide factor"), Range(0f, 1f)]
        private float _defaultSlideFactor = 0.03f;

        [Title("Jumping")]
        [SerializeField, Tooltip("Velocity threshold to trigger a jump")]
        private float _toJumpVelocity = 0.4f;
        [SerializeField, Tooltip("Strength for the jump velocity")]
        private float _jumpingStrength = 1.1f;
        [SerializeField, Tooltip("Maximum jump speed")]
        private float _maxJumpSpeed = 6.5f;
        [SerializeField, Tooltip("Size of the velocity history used to determine if the player trigger a jump"), Range(3, 10)]
        private int _velocityHistorySize = 8;

        [Title("Surface")]
        [SerializeField, Tooltip("Radius of the hand sphere used for surface detection")]
        private float _handSphereRadius = 0.05f;
        [SerializeField, Tooltip("The layers that count as surface for gorilla locomotion")]
        private LayerMask _surfaceLayerMask = ~0;

        [Title("Haptics")]
        [SerializeField]
        private bool _allowTouchingHaptics = true;
        [SerializeField, Indent, EnableIf(nameof(_allowTouchingHaptics))]
        private float _touchingHapticsCoolDown = 0.15f;
        [SerializeField, Indent, EnableIf(nameof(_allowTouchingHaptics))]
        private float _touchingHapticDuration = 0.05f;
        [SerializeField, Indent, EnableIf(nameof(_allowTouchingHaptics))]
        private float _touchingHapticAmplitude = 0.5f;
        [SerializeField, Indent, EnableIf(nameof(_allowTouchingHaptics))]
        private float _slidingHapticAmplitude = 0.5f;

        private Vector3 _lastHeadPos;
        private Vector3 _lastBodyPos;
        private Vector3 _lastUpdatePos;
        private Vector3 _lastLeftHandPos, _lastRightHandPos;

        private bool _leftHandColliding = false, _rightHandColliding = false;
        private bool _wasLeftHandTouching = false, _wasRightHandTouching = false;
        private bool _allowVibrationLeft = false, _allowVibrationRight = false;
        private float _lastLeftCollideTime = 0f, _lastRightCollideTime = 0f;

        private int _velocityIndex = 0;
        private Vector3[] _velocityHistory;
        private Vector3 _denormalizedVelocityAverage = Vector3.zero;

        private RaycastHit[] _hitsNonAlloc5 = new RaycastHit[5];
        private int _lastHitsCount = 0;

        public Transform LeftHand => _leftHand;
        public Transform RightHand => _rightHand;
        public Vector3 BodyMovement { get; private set; }
        public Vector3 JumpVelocity { get; private set; }

        public bool IsJumping { get; private set; }
        public bool IsHandTouching => _wasLeftHandTouching || _wasRightHandTouching;
        public bool IsLeftHandTouching => _wasLeftHandTouching;
        public bool IsRightHandTouching => _wasRightHandTouching;

        private void Awake()
        {
            _velocityHistory = new Vector3[_velocityHistorySize];
        }

        private void Start()
        {
            if (_useHeadCollider)
            {
                _lastHeadPos = _headCollider.transform.position;
            }
            _lastBodyPos = _bodyCollider.transform.position;
            _lastUpdatePos = transform.position;
            _lastLeftHandPos = _leftHand.transform.position;
            _lastRightHandPos = _rightHand.transform.position;
        }

        private void FixedUpdate()
        {
            Jump();
        }

        private void Update()
        {
            UpdateBodyMovement();
            FinalizeHands();
            PlayHaptics();
        }

        private Vector3 GetHandPosition(Transform hand)
        {
            if (_limitArmLength)
            {
                var shoulder = hand == _leftHand ? _leftShoulder : _rightShoulder;
                if ((hand.position - shoulder.position).magnitude >= _maxArmLength)
                    return shoulder.position + (hand.position - shoulder.position).normalized * _maxArmLength;
            }

            return hand.position;
        }

        private void UpdateBodyMovement()
        {
            BodyMovement = Vector3.zero;

            Vector3 leftHandMovement = Vector3.zero;
            Vector3 rightHandMovement = Vector3.zero;

            var currentLeftHandPos = GetHandPosition(_leftHand);
            var currentRightHandPos = GetHandPosition(_rightHand);
            // use gravity offset to simulate natural falling when climbing
            Vector3 gravityOffset = Physics.gravity * (_gravityStrength * Time.deltaTime * Time.deltaTime);

            var traveledVector = currentLeftHandPos - _lastLeftHandPos + gravityOffset;
            if (ApplySphereMovement(_lastLeftHandPos, _handSphereRadius, traveledVector, out var finalPosition, out _, true, false, out _))
            {
                // this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
                if (_wasLeftHandTouching)
                    leftHandMovement = _lastLeftHandPos - currentLeftHandPos;
                else
                    leftHandMovement = finalPosition - currentLeftHandPos;

                _leftHandColliding = true;
            }
            else
            {
                _leftHandColliding = false;
            }

            traveledVector = currentRightHandPos - _lastRightHandPos + gravityOffset;
            if (ApplySphereMovement(_lastRightHandPos, _handSphereRadius, traveledVector, out finalPosition, out _, true, false, out _))
            {
                if (_wasRightHandTouching)
                    rightHandMovement = _lastRightHandPos - currentRightHandPos;
                else
                    rightHandMovement = finalPosition - currentRightHandPos;

                _rightHandColliding = true;
            }
            else
            {
                _rightHandColliding = false;
            }

            if ((_leftHandColliding || _wasLeftHandTouching) && (_rightHandColliding || _wasRightHandTouching))
            {
                // this lets you touch surface with both hands at the same time
                BodyMovement = (leftHandMovement + rightHandMovement) / 2;
            }
            else
            {
                BodyMovement = leftHandMovement + rightHandMovement;
            }

            if (BodyMovement != Vector3.zero)
            {
                PreventClipping();
            }

            void PreventClipping()
            {
                traveledVector = _bodyCollider.transform.position + BodyMovement - _lastBodyPos;
                if (ApplySphereMovement(_lastBodyPos, _handSphereRadius, traveledVector, out _, out _, false, true, out _))
                {
                    BodyMovement = finalPosition - _lastBodyPos;

                    if (Physics.Raycast(
                            _lastBodyPos,
                            traveledVector,
                            out _,
                            traveledVector.magnitude + _bodyCollider.radius * GORILLA_TOUCHING_PRECISION * 0.999f,
                            _surfaceLayerMask.value))
                    {
                        BodyMovement = _lastBodyPos - _bodyCollider.transform.position;
                    }
                }
                _lastBodyPos = _bodyCollider.transform.position + BodyMovement;

                if (_useHeadCollider)
                {
                    traveledVector = _headCollider.transform.position + BodyMovement - _lastHeadPos;
                    if (ApplySphereMovement(_lastHeadPos, _headCollider.radius, traveledVector, out finalPosition, out _, false, true, out _))
                    {
                        BodyMovement = finalPosition - _lastHeadPos;

                        // last check to make sure the head won't phase through geometry
                        if (Physics.Raycast(
                                _lastHeadPos,
                                traveledVector,
                                out _,
                                traveledVector.magnitude + _headCollider.radius * GORILLA_TOUCHING_PRECISION * 0.999f,
                                _surfaceLayerMask.value))
                        {
                            BodyMovement = _lastHeadPos - _headCollider.transform.position;
                        }
                    }
                    _lastHeadPos = _headCollider.transform.position + BodyMovement;
                }
            }
        }

        private void FinalizeHands()
        {
            Vector3 finalPosition;

            // After body movement, recaculate the hand follow positions
            var currentLeftHandPos = GetHandPosition(_leftHand) + BodyMovement;
            var currentRightHandPos = GetHandPosition(_rightHand) + BodyMovement;

            // Update final position for hands after body movement

            var singleHand = !((_leftHandColliding || _wasLeftHandTouching) && (_rightHandColliding || _wasRightHandTouching));

            var handTraveled = currentLeftHandPos - _lastLeftHandPos;
            if (ApplySphereMovement(_lastLeftHandPos, _handSphereRadius, handTraveled, out finalPosition, out _, singleHand, false, out _))
            {
                _leftHandColliding = true;
                _lastLeftHandPos = finalPosition;
            }
            else
            {
                _lastLeftHandPos = currentLeftHandPos;
            }

            handTraveled = currentRightHandPos - _lastRightHandPos;
            if (ApplySphereMovement(_lastRightHandPos, _handSphereRadius, handTraveled, out finalPosition, out _, singleHand, false, out _))
            {
                _rightHandColliding = true;
                _lastRightHandPos = finalPosition;
            }
            else
            {
                _lastRightHandPos = currentRightHandPos;
            }

            // Check to see if we need to unstick from a surface

            if (_leftHandColliding && (currentLeftHandPos - _lastLeftHandPos).magnitude > _unstickDistance)
            {
                _leftHandColliding = false;
                _lastLeftHandPos = currentLeftHandPos;
            }

            if (_rightHandColliding && (currentRightHandPos - _lastRightHandPos).magnitude > _unstickDistance)
            {
                _rightHandColliding = false;
                _lastRightHandPos = currentRightHandPos;
            }

            _allowVibrationLeft = _leftHandColliding && _wasLeftHandTouching != _leftHandColliding;
            _allowVibrationRight = _rightHandColliding && _wasRightHandTouching != _rightHandColliding;

            _wasLeftHandTouching = _leftHandColliding;
            _wasRightHandTouching = _rightHandColliding;
        }

        private void PlayHaptics()
        {
            var currentTime = Time.realtimeSinceStartup;

            if (_allowVibrationLeft && currentTime - _lastLeftCollideTime > _touchingHapticsCoolDown)
            {
                HandPlayer.Instance.HandLeft.PlayHapticVibration(_touchingHapticDuration, _touchingHapticAmplitude);
                _lastLeftCollideTime = currentTime;
            }

            if (_allowVibrationRight && currentTime - _lastRightCollideTime > _touchingHapticsCoolDown)
            {
                HandPlayer.Instance.HandRight.PlayHapticVibration(_touchingHapticDuration, _touchingHapticAmplitude);
                _lastRightCollideTime = currentTime;
            }
        }

        private void Jump()
        {
            IsJumping = false;
            JumpVelocity = Vector3.zero;
            StoreVelocities();

            if (_rightHandColliding || _leftHandColliding)
            {
                if (_denormalizedVelocityAverage.magnitude > _toJumpVelocity)
                {
                    IsJumping = true;
                    if (_denormalizedVelocityAverage.magnitude * _jumpingStrength > _maxJumpSpeed)
                    {
                        JumpVelocity = _denormalizedVelocityAverage.normalized * _maxJumpSpeed;
                    }
                    else
                    {
                        JumpVelocity = _jumpingStrength * _denormalizedVelocityAverage;
                    }
                }
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
    }
}