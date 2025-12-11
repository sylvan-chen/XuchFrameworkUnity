using Alchemy.Inspector;
using Autohand;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(5000)]
    public partial class HandPlayer : MonoSingleton<HandPlayer>
    {
        public enum TurningType
        {
            Snap,
            Smooth,
        }

        [AutoHeader("Hand Player")]
        public bool IgnoreMe;

        [Space(5)]
        [BoxGroup("Tracker"), SerializeField]
        private Transform _trackingContainer;
        [BoxGroup("Tracker"), SerializeField]
        private Camera _headCamera;

        [BoxGroup("Player"), SerializeField]
        private SphereCollider _headCollider;
        [BoxGroup("Player"), SerializeField]
        private CapsuleCollider _bodyCollider;

        [HorizontalGroup("Arms"), BoxGroup("Arms/LeftHand"), SerializeField]
        private Hand _handLeft;
        [HorizontalGroup("Arms"), BoxGroup("Arms/RightHand"), SerializeField]
        private Hand _handRight;

        [Tooltip("Player model following this")]
        public Transform PlayerHeadOffset;

        [AutoSmallHeader("Movement")]
        public bool IgnoreMe1;
        [SerializeField]
        private bool _bodyFollowsHead = true;
        [SerializeField]
        public bool UseGorillamotion = true;

        [AutoToggleHeader("Turning")]
        public bool AllowTurning = true;
        [SerializeField, EnableIf(nameof(AllowTurning)), Tooltip("Whether or not to use snap turning or smooth turning")]
        private TurningType _turningType = TurningType.Snap;
        [SerializeField, ShowIf(nameof(IsSnapTurning)), EnableIf(nameof(AllowTurning))]
        [Tooltip("Turn angle per snap when using snap turning")]
        private float _snapTurnAngle = 30f;
        [SerializeField, ShowIf(nameof(IsSmoothTurning)), EnableIf(nameof(AllowTurning))]
        [Tooltip("Turn speed when not using snap turning"), Min(0)]
        private float _smoothTurnSpeed = 180f;
        [SerializeField, EnableIf(nameof(AllowTurning)), Tooltip("The deadzone for turning input"), Min(0)]
        private float _turnDeadzone = 0.4f;
        [SerializeField, EnableIf(nameof(AllowTurning)), Tooltip("Amount of input required to reset the turn state for snap turning"), Min(0)]
        private float _turnResetzone = 0.3f;

        [AutoToggleHeader("Height")]
        public bool ShowHeight = true;
        [SerializeField, EnableIf(nameof(ShowHeight))]
        private float _heightOffset = 0f;
        [SerializeField, EnableIf(nameof(ShowHeight))]
        private float _targetHeight = 0.35f;
        [SerializeField, EnableIf(nameof(ShowHeight))]
        private float _sneakHeight = 0.15f;
        [SerializeField, EnableIf(nameof(ShowHeight)), Tooltip("Whether or not the capsule height should be adjusted to match the headCamera height")]
        private bool _autoAdjustColliderHeight = true;
        [SerializeField, EnableIf(nameof(ShowHeight)),
         Tooltip("Minimum and maximum auto adjusted height, to adjust height without auto adjustment change capsule collider height instead")]
        private Vector2 _minMaxHeight = new Vector2(0.5f, 2.5f);

#if UNITY_EDITOR
        [AutoToggleHeader("Debug"), OnValueChanged(nameof(OnEnableDebugChanged))]
        public bool EnableDebug = false;
        [SerializeField, EnableIf(nameof(EnableDebug)), OnValueChanged(nameof(OnShowHandFollowSpheresChanged))]
        private bool _showHandFollowSpheres = true;
#endif

        private Gorillamotion2 _gorilla;
        private Hand _lastLeftHand, _lastRightHand;

        private Vector3 _lastUpdatePos;
        private Vector3 _targetTrackedPos;
        private Vector3 _trackingPosOffset;

        private float _turningAxis;
        private bool _isTurningAxisReset = true;

        private bool _hasInitTracking = false;

        public Hand HandLeft
        {
            get => _handLeft;
            set
            {
                _handLeft = value;
                if (_lastLeftHand != _handLeft)
                {
                    DisableHand(_lastLeftHand);
                    EnableHand(_handLeft);
                    _lastLeftHand = _handLeft;
                }
            }
        }

        public Hand HandRight
        {
            get => _handRight;
            set
            {
                _handRight = value;
                if (_lastRightHand != _handRight)
                {
                    DisableHand(_lastRightHand);
                    EnableHand(_handRight);
                    _lastRightHand = _handRight;
                }
            }
        }

        public Rigidbody Body { get; private set; }
        public LayerMask PlayerLayerMask { get; private set; }
        public Camera HeadCamera => _headCamera;

        protected override void Awake()
        {
            base.Awake();

            Body = GetComponent<Rigidbody>();
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation.None;
            if (Body.collisionDetectionMode == CollisionDetectionMode.Discrete)
                Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _gorilla = GetComponent<Gorillamotion2>();
            if (_gorilla == null)
            {
                Log.Error($"[HandPlayer] Gorillamotion2 component not found on HandPlayer GameObject. Disabling Gorillamotion functionality.");
                UseGorillamotion = false;
            }

            if (_hasInitTracking)
                return;
            if (_trackingContainer == null || _headCamera == null)
                return;

            _lastUpdatePos = transform.position;
            _targetTrackedPos = _trackingContainer.position;
            _trackingPosOffset = Vector3.zero;
            _hasInitTracking = true;

            // GameLauncher.Instance.SetTempPlayerActivate(false);
            _singletonState = MonoSingletonState.Initialized;

#if UNITY_EDITOR
            if (EnableDebug)
                OnEnableDebugChanged(EnableDebug);
#endif
        }

        protected virtual void Start()
        {
            PlayerLayerMask = LayerMaskHelper.GetPhysicsLayerMask(gameObject.layer);
        }

        private void EnableHand(Hand hand)
        {
            if (hand == null)
                return;

            hand.OnGrabbed += OnHandGrab;
            hand.OnReleased += OnHandRelease;
        }

        private void DisableHand(Hand hand)
        {
            if (hand == null)
                return;

            hand.OnGrabbed -= OnHandGrab;
            hand.OnReleased -= OnHandRelease;
        }

        protected virtual void OnHandGrab(Hand hand, Grabbable grab)
        {
            grab.IgnoreColliders(_bodyCollider);
            if (_headCollider != null)
                grab.IgnoreColliders(_headCollider);
        }

        protected virtual void OnHandRelease(Hand hand, Grabbable grab)
        {
            if (grab != null && grab.HeldCount() == 0)
            {
                grab.IgnoreColliders(_bodyCollider, false);
                if (_headCollider != null)
                    grab.IgnoreColliders(_headCollider, false);

                if (grab && grab.parentOnGrab && grab.body != null && !grab.body.isKinematic)
                    grab.body.linearVelocity += Body.linearVelocity / 2f;
            }
        }

        protected void FixedUpdate()
        {
            if (!_hasInitTracking)
                return;

            if (UseGorillamotion)
                _gorilla.Jump();

            SyncBodyHead();
        }

        protected virtual void Update()
        {
            if (!_hasInitTracking)
                return;

            if (UseGorillamotion)
                _gorilla.ApplyGorillamotion();

            if (MountingObj != null)
            {
                transform.position = MountingObj.MountPoint.position;
                Body.position = transform.position;
            }

            UpdateTrackingContainer();
            UpdateTurn();
        }

        private void UpdateTrackingContainer()
        {
            _targetTrackedPos += transform.position - _lastUpdatePos;
            _trackingContainer.position = _targetTrackedPos;

            _heightOffset = _targetHeight - _headCamera.transform.localPosition.y;
            _trackingContainer.localPosition += Vector3.up * _heightOffset;

            // var targetPos = transform.position - _headCamera.transform.position;
            // targetPos.y = 0;
            // _trackingPosOffset = Vector3.MoveTowards(_trackingPosOffset, targetPos, Body.linearVelocity.magnitude * Time.deltaTime);
            // _trackingContainer.position += _trackingPosOffset;

            _lastUpdatePos = transform.position;
        }

        private void SyncBodyHead()
        {
            if (!_bodyFollowsHead)
                return;

            Vector3 bodyFlatPos = transform.position;
            Vector3 headFlatPos = _headCamera.transform.position;
            bodyFlatPos.y = headFlatPos.y = 0f;

            if (Vector3.Distance(headFlatPos, bodyFlatPos) > Physics.defaultContactOffset * 1.5f)
            {
                Vector3 direction = Vector3.ClampMagnitude(headFlatPos - bodyFlatPos, _bodyCollider.radius / 2f);
                transform.position += direction;
                Body.position = transform.position;
                _targetTrackedPos -= direction;
            }
        }

        private void UpdateTurn()
        {
            if (_turningType == TurningType.Snap)
            {
                if (Mathf.Abs(_turningAxis) > _turnDeadzone && _isTurningAxisReset)
                {
                    var angle = _turningAxis > _turnDeadzone ? _snapTurnAngle : -_snapTurnAngle;

                    var targetPos = transform.position - _headCamera.transform.position;
                    targetPos.y = 0;

                    _trackingContainer.position += targetPos;

                    _lastUpdatePos = new Vector3(transform.position.x, _lastUpdatePos.y, transform.position.z);
                    var handRightStartPos = _handRight.transform.position;
                    var handLeftStartPos = _handLeft.transform.position;

                    _trackingContainer.RotateAround(transform.position, Vector3.up, angle);
                    _trackingPosOffset = Vector3.zero;
                    _targetTrackedPos = new Vector3(_trackingContainer.position.x, _targetTrackedPos.y, _trackingContainer.position.z);

                    if (_handRight.holdingObj != null && !_handRight.IsGrabbing())
                    {
                        _handRight.body.position = _handRight.handGrabPoint.position;
                        _handRight.body.rotation = _handRight.handGrabPoint.rotation;
                    }
                    else
                    {
                        _handRight.body.position = _handRight.transform.position;
                        _handRight.body.rotation = _handRight.transform.rotation;
                    }

                    _handRight.handFollow.AverageSetMoveTo();
                    _handLeft.handFollow.AverageSetMoveTo();

                    PreventHandClipping(_handRight, handRightStartPos);
                    PreventHandClipping(_handLeft, handLeftStartPos);
                    Physics.SyncTransforms();

                    // OnSnapTurn?.Invoke(this);
                    _isTurningAxisReset = false;
                }
            }
            else if (Mathf.Abs(_turningAxis) > _turnDeadzone)
            {
                _lastUpdatePos = new Vector3(transform.position.x, _lastUpdatePos.y, transform.position.z);
                _trackingContainer.RotateAround(
                    transform.position,
                    Vector3.up,
                    _smoothTurnSpeed * (Mathf.MoveTowards(_turningAxis, 0, _turnDeadzone)) * Time.deltaTime);

                _trackingPosOffset = Vector3.zero;
                _targetTrackedPos = new Vector3(_trackingContainer.position.x, _targetTrackedPos.y, _trackingContainer.position.z);

                _handRight.handFollow.AverageSetMoveTo();
                _handLeft.handFollow.AverageSetMoveTo();
                Physics.SyncTransforms();

                // OnSmoothTurn?.Invoke(this);
                _isTurningAxisReset = false;
            }

            if (Mathf.Abs(_turningAxis) < _turnResetzone)
                _isTurningAxisReset = true;

            void PreventHandClipping(Hand hand, Vector3 startPosition)
            {
                var deltaHandPos = hand.transform.position - startPosition;
                if (deltaHandPos.magnitude < Physics.defaultContactOffset)
                    return;

                var center = hand.handEncapsulationBox.transform.TransformPoint(hand.handEncapsulationBox.center) - deltaHandPos;
                var halfExtents = hand.handEncapsulationBox.transform.TransformVector(hand.handEncapsulationBox.size) / 2f;
                var hits = Physics.BoxCastAll(
                    center,
                    halfExtents,
                    deltaHandPos,
                    hand.handEncapsulationBox.transform.rotation,
                    deltaHandPos.magnitude * 1.5f,
                    PlayerLayerMask);
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit.collider.isTrigger)
                        continue;

                    if (hand.holdingObj == null
                        || hit.collider.attachedRigidbody == null
                        || (hit.collider.attachedRigidbody != hand.holdingObj.body
                            && !hand.holdingObj.jointedBodies.Contains(hit.collider.attachedRigidbody)))
                    {
                        var deltaHitPos = hit.point - hand.transform.position;
                        hand.transform.position = Vector3.MoveTowards(hand.transform.position, startPosition, deltaHitPos.magnitude);

                        break;
                    }
                }
            }
        }

        public void IgnoreCollider(Collider col, bool ignore)
        {
            Physics.IgnoreCollision(_bodyCollider, col, ignore);
            if (_headCollider != null)
                Physics.IgnoreCollision(_headCollider, col, ignore);
        }

        public void Turn(float turnAxis)
        {
            turnAxis = (Mathf.Abs(turnAxis) > _turnDeadzone) ? turnAxis : 0;
            _turningAxis = turnAxis;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            Body.position = position;
        }

        private bool IsSnapTurning => _turningType == TurningType.Snap;
        private bool IsSmoothTurning => _turningType == TurningType.Smooth;

#if UNITY_EDITOR
        private Transform _debugHandFollowSphereLeft;
        private Transform _debugHandFollowSphereRight;

        private void OnEnableDebugChanged(bool value)
        {
            if (!Application.isPlaying)
                return;

            if (value)
            {
                var handFollowSphere = Resources.Load<GameObject>("hand_follow_sphere").transform;
                _debugHandFollowSphereLeft = Instantiate(handFollowSphere, _handLeft.follow);
                _debugHandFollowSphereRight = Instantiate(handFollowSphere, _handRight.follow);
                OnShowHandFollowSpheresChanged(_showHandFollowSpheres);
            }
            else
            {
                DestroyImmediate(_debugHandFollowSphereLeft.gameObject);
                DestroyImmediate(_debugHandFollowSphereRight.gameObject);
            }
        }

        private void OnShowHandFollowSpheresChanged(bool value)
        {
            if (!Application.isPlaying)
                return;

            if (_debugHandFollowSphereLeft != null)
                _debugHandFollowSphereLeft.gameObject.SetActive(value);
            if (_debugHandFollowSphereRight != null)
                _debugHandFollowSphereRight.gameObject.SetActive(value);
        }
#endif
    }
}