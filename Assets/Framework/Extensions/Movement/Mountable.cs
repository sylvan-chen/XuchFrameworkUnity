using System;
using Autohand;
using Sirenix.OdinInspector;
using XuchFramework.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace XuchFramework.Extensions.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Gorillamotion2))]
    [DefaultExecutionOrder(4999)]
    public class Mountable : MonoSingleton<Mountable>
    {
        [Serializable]
        public class UnityMountEvent : UnityEvent<HandPlayer, Mountable> { }

        [AutoHeader("Mountable", "mountable_header_icon")]
        public bool IgnoreMe;

        [Space(5)]
        [SerializeField]
        private Transform _mountPoint;
        [SerializeField]
        private Transform _limbTargetFrontLeft;
        [SerializeField]
        private Transform _limbTargetFrontRight;
        [SerializeField]
        private Transform _limbTargetBackLeft;
        [SerializeField]
        private Transform _limbTargetBackRight;

        [AutoToggleHeader("Turning")]
        public bool AllowTurning = true;
        [SerializeField, EnableIf(nameof(AllowTurning))]
        private Transform _headTarget;

        [AutoSmallHeader("Space Mapping")]
        public bool UseMapping = true;

        [BoxGroup("Player"), SerializeField, EnableIf(nameof(UseMapping))]
        private Transform _playerSpaceCenter;
        [BoxGroup("Player"), SerializeField, EnableIf(nameof(UseMapping))]
        private Vector3 _playerSpaceSize = Vector3.one;

        [BoxGroup("MountableFront"), SerializeField, EnableIf(nameof(UseMapping))]
        private Transform _mountableFrontSpaceCenter;
        [BoxGroup("MountableFront"), SerializeField, EnableIf(nameof(UseMapping))]
        private Vector3 _mountableFrontSpaceSize = Vector3.one;
        [BoxGroup("MountableFront"), SerializeField, EnableIf(nameof(UseMapping))]
        private bool _reverseFrontSide = false;

        [BoxGroup("MountableBack"), SerializeField, EnableIf(nameof(UseMapping))]
        private Transform _mountableBackSpaceCenter;
        [BoxGroup("MountableBack"), SerializeField, EnableIf(nameof(UseMapping))]
        private Vector3 _mountableBackSpaceSize = Vector3.one;
        [BoxGroup("MountableBack"), SerializeField, EnableIf(nameof(UseMapping))]
        private bool _reverseBackSide = false;

        [Header("Gizmos")]
        [SerializeField, EnableIf(nameof(UseMapping))]
        private bool _showGizmos = true;

        [AutoSmallHeader("Limbs")]
        public bool IgnoreMe1;
        [SerializeField]
        private float _limbRestoreDuration = 1f;

        [AutoToggleHeader("Events")]
        public bool EnableEvents = true;
        [SerializeField, EnableIf(nameof(EnableEvents))]
        private UnityMountEvent _onMount = new();
        [SerializeField, EnableIf(nameof(EnableEvents))]
        private UnityMountEvent _onDismount = new();

        // For programmers
        public MountEvent OnBeforeMountEvent;
        public MountEvent OnMountEvent;
        public MountEvent OnBeforeDismountEvent;
        public MountEvent OnDismountEvent;

        private Vector3 _initialHeadLocalEulerAngles;

        public Transform MountPoint => _mountPoint;
        public Transform HeadTarget => _headTarget;
        public Transform LimbTargetFrontLeft => _limbTargetFrontLeft;
        public Transform LimbTargetFrontRight => _limbTargetFrontRight;
        public Transform LimbTargetBackLeft => _limbTargetBackLeft;
        public Transform LimbTargetBackRight => _limbTargetBackRight;

        public Rigidbody Body { get; private set; }
        public Gorillamotion2 Gorilla { get; private set; }
        public HandPlayer MountedBy { get; private set; }
        public bool IsMounted { get; private set; }

        protected override void OnInitialize()
        {
            Body = GetComponent<Rigidbody>();
            Gorilla = GetComponent<Gorillamotion2>();

            _initialHeadLocalEulerAngles = _headTarget.localEulerAngles;
        }

        private void FixedUpdate()
        {
            if (MountedBy != null)
            {
                Gorilla.Jump();
            }
        }

        private void Update()
        {
            if (MountedBy != null)
            {
                Gorilla.ApplyGorillamotion();
            }
        }

        private void LateUpdate()
        {
            if (MountedBy != null)
            {
                UpdateTurning();
                UpdateSpaceMapping();
            }
        }

        private void UpdateTurning()
        {
            _headTarget.rotation = MountedBy.HeadCamera.transform.rotation;

            transform.rotation = Quaternion.Euler(0, MountedBy.HeadCamera.transform.eulerAngles.y, 0);
        }

        private void UpdateSpaceMapping()
        {
            Hand frontFollowHandLeft, frontFollowHandRight;
            Hand backFollowHandLeft, backFollowHandRight;

            if (_reverseFrontSide)
            {
                frontFollowHandLeft = MountedBy.HandRight;
                frontFollowHandRight = MountedBy.HandLeft;
            }
            else
            {
                frontFollowHandLeft = MountedBy.HandLeft;
                frontFollowHandRight = MountedBy.HandRight;
            }

            if (_reverseBackSide)
            {
                backFollowHandLeft = MountedBy.HandRight;
                backFollowHandRight = MountedBy.HandLeft;
            }
            else
            {
                backFollowHandLeft = MountedBy.HandLeft;
                backFollowHandRight = MountedBy.HandRight;
            }

            // Update mapping
            _limbTargetFrontLeft.position = PlayerToMountable(
                frontFollowHandLeft.follow.position,
                _mountableFrontSpaceCenter,
                _mountableFrontSpaceSize,
                _reverseFrontSide);
            _limbTargetFrontLeft.rotation = frontFollowHandLeft.follow.rotation;

            _limbTargetFrontRight.position = PlayerToMountable(
                frontFollowHandRight.follow.position,
                _mountableFrontSpaceCenter,
                _mountableFrontSpaceSize,
                _reverseFrontSide);
            _limbTargetFrontRight.rotation = frontFollowHandRight.follow.rotation;

            _limbTargetBackLeft.position = PlayerToMountable(
                backFollowHandLeft.follow.position,
                _mountableBackSpaceCenter,
                _mountableBackSpaceSize,
                _reverseBackSide);
            _limbTargetBackLeft.rotation = backFollowHandLeft.follow.rotation;

            _limbTargetBackRight.position = PlayerToMountable(
                backFollowHandRight.follow.position,
                _mountableBackSpaceCenter,
                _mountableBackSpaceSize,
                _reverseBackSide);
            _limbTargetBackRight.rotation = backFollowHandRight.follow.rotation;

            Vector3 PlayerToMountable(Vector3 playerHandPosition, Transform mountableSpaceCenter, Vector3 mountableSpaceSize, bool reverse)
            {
                var playerRelativePos = _playerSpaceCenter.InverseTransformPoint(playerHandPosition);

                var normalizedPos = new Vector3(
                    playerRelativePos.x / (_playerSpaceSize.x / _playerSpaceCenter.lossyScale.x),
                    playerRelativePos.y / (_playerSpaceSize.y / _playerSpaceCenter.lossyScale.y),
                    playerRelativePos.z / (_playerSpaceSize.z / _playerSpaceCenter.lossyScale.z));

                normalizedPos = new Vector3(
                    Mathf.Clamp(normalizedPos.x, -1f, 1f),
                    Mathf.Clamp(normalizedPos.y, -1f, 1f),
                    Mathf.Clamp(normalizedPos.z, -1f, 1f));

                var mountableRelativePos = new Vector3(
                    normalizedPos.x * (mountableSpaceSize.x / mountableSpaceCenter.lossyScale.x),
                    normalizedPos.y * (mountableSpaceSize.y / mountableSpaceCenter.lossyScale.y),
                    normalizedPos.z * (mountableSpaceSize.z / mountableSpaceCenter.lossyScale.z));

                if (reverse)
                    mountableRelativePos = new Vector3(-mountableRelativePos.x, mountableRelativePos.y, mountableRelativePos.z);

                return mountableSpaceCenter.TransformPoint(mountableRelativePos);
            }
        }

        internal void OnBeforeMount(HandPlayer player)
        {
            OnBeforeMountEvent?.Invoke(player, this);

            IsMounted = true;
        }

        internal void OnMount(HandPlayer player)
        {
            _onMount?.Invoke(player, this);
            OnMountEvent?.Invoke(player, this);

            MountedBy = player;

            _headTarget.localEulerAngles = _initialHeadLocalEulerAngles;

            // TODO: 按侧边键开始控制，送侧边键停止控制
            StartControlMovement();
        }

        internal void OnBeforeDisMount(HandPlayer player)
        {
            OnBeforeDismountEvent?.Invoke(player, this);

            IsMounted = false;
        }

        internal void OnDismount(HandPlayer player)
        {
            if (MountedBy == player)
            {
                MountedBy = null;
                Body.useGravity = true;
                StopControlMovement();

                _onDismount?.Invoke(player, this);
                OnDismountEvent?.Invoke(player, this);
            }
        }

        private void StartControlMovement() { }

        private void StopControlMovement() { }

#if UNITY_EDITOR
        [AutoToggleHeader("Debug")]
        public bool EnableDebug = false;

        // Debug: 一键快速上下马
        [SerializeField, EnableIf(nameof(EnableDebug))]
        public bool QuickMount = true;
        [SerializeField, EnableIf(nameof(EnableDebug), nameof(QuickMount))]
        private InputActionProperty _quickMountAction;

        private void OnEnable()
        {
            _quickMountAction.action.performed += OnMountActionPerformed;
        }

        private void OnDisable()
        {
            _quickMountAction.action.performed -= OnMountActionPerformed;
        }

        private void OnMountActionPerformed(InputAction.CallbackContext context)
        {
            if (MountedBy != null)
                HandPlayer.Instance.Dismount();
            else
                HandPlayer.Instance.Mount(this);
        }

#endif

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;

            var playerCenter = _playerSpaceCenter != null ? _playerSpaceCenter : MountPoint;

            if (playerCenter != null)
            {
                Gizmos.matrix = playerCenter.localToWorldMatrix;
                var size = Vector3.Scale(
                    _playerSpaceSize,
                    new Vector3(1 / playerCenter.lossyScale.x, 1 / playerCenter.lossyScale.y, 1 / playerCenter.lossyScale.z));
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = new Color(0, 1, 0, 0.6f);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (_mountableFrontSpaceCenter != null)
            {
                Gizmos.matrix = _mountableFrontSpaceCenter.localToWorldMatrix;
                var size = Vector3.Scale(
                    _mountableFrontSpaceSize,
                    new Vector3(
                        1 / _mountableFrontSpaceCenter.lossyScale.x,
                        1 / _mountableFrontSpaceCenter.lossyScale.y,
                        1 / _mountableFrontSpaceCenter.lossyScale.z));
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = new Color(1, 0, 0, 0.6f);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (_mountableBackSpaceCenter != null)
            {
                Gizmos.matrix = _mountableBackSpaceCenter.localToWorldMatrix;
                var size = Vector3.Scale(
                    _mountableBackSpaceSize,
                    new Vector3(
                        1 / _mountableBackSpaceCenter.lossyScale.x,
                        1 / _mountableBackSpaceCenter.lossyScale.y,
                        1 / _mountableBackSpaceCenter.lossyScale.z));
                Gizmos.color = new Color(1, 0, 0.5f, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = new Color(1, 0, 0.5f, 0.6f);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            // Mapping lines on play mode
            if (Application.isPlaying && MountedBy != null)
            {
                Gizmos.color = Color.yellow;

                Transform frontLeftTarget, frontRightTarget;
                Transform backLeftTarget, backRightTarget;

                if (_reverseFrontSide)
                {
                    frontLeftTarget = _limbTargetFrontRight;
                    frontRightTarget = _limbTargetFrontLeft;
                }
                else
                {
                    frontLeftTarget = _limbTargetFrontLeft;
                    frontRightTarget = _limbTargetFrontRight;
                }

                if (_reverseBackSide)
                {
                    backLeftTarget = _limbTargetBackRight;
                    backRightTarget = _limbTargetBackLeft;
                }
                else
                {
                    backLeftTarget = _limbTargetBackLeft;
                    backRightTarget = _limbTargetBackRight;
                }

                Gizmos.DrawLine(HandPlayer.Instance.HandLeft.follow.position, frontLeftTarget.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandLeft.follow.position, 0.05f);
                Gizmos.DrawSphere(frontLeftTarget.position, 0.05f);

                Gizmos.DrawLine(HandPlayer.Instance.HandRight.follow.position, frontRightTarget.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandRight.follow.position, 0.05f);
                Gizmos.DrawSphere(frontRightTarget.position, 0.05f);

                Gizmos.DrawLine(HandPlayer.Instance.HandLeft.follow.position, backLeftTarget.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandLeft.follow.position, 0.05f);
                Gizmos.DrawSphere(backLeftTarget.position, 0.05f);

                Gizmos.DrawLine(HandPlayer.Instance.HandRight.follow.position, backRightTarget.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandRight.follow.position, 0.05f);
                Gizmos.DrawSphere(backRightTarget.position, 0.05f);
            }
        }

        #endregion
    }
}