using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using Autohand;
using DG.Tweening;
using DigiEden.Framework.Utils;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DigiEden.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Gorillamotion2))]
    [DefaultExecutionOrder(4999)]
    public class Mountable : MonoBehaviour
    {
        [Serializable]
        public enum IKTargetFollowType
        {
            Left = 0,
            Right = 1,
        }

        [Serializable, BoxGroup]
        public class IKTarget
        {
            public IKTargetFollowType Follow;
            public LimbIK Limb;
            public Transform Target;
            public Vector3 PositionOffset;
            public Vector3 RotationOffset;
            [Group("Constraints")]
            public AxisMask FreezePosition;
            [Group("Constraints")]
            public AxisMask FreezeRotation;
        }

        [Serializable]
        public class UnityMountEvent : UnityEvent<HandPlayer, Mountable> { }

        [AutoHeader("Mountable", "mountable_logo")]
        public bool IgnoreMe;

        [Space(5)]
        [SerializeField]
        private Transform _mountPoint;
        [SerializeField]
        private Transform _mappedHandLeft;
        [SerializeField]
        private Transform _mappedHandRight;

        [AutoSmallHeader("Space Mapping")]
        public bool UseMapping = true;
        [Space(10)]
        [SerializeField, ReadOnly]
        private Transform _playerSpaceCenter;
        [SerializeField, EnableIf(nameof(UseMapping))]
        private Vector3 _playerSpaceSize = Vector3.one;
        [SerializeField, EnableIf(nameof(UseMapping))]
        private Transform _mountableSpaceCenter;
        [SerializeField, EnableIf(nameof(UseMapping))]
        private Vector3 _mountableSpaceSize = Vector3.one;
        [SerializeField, EnableIf(nameof(UseMapping))]
        private bool _showGizmos = true;

        [AutoSmallHeader("IK Target")]
        public bool IgnoreMe1;
        [SerializeField]
        private float _limbResetDuration = 1f;
        [SerializeField]
        private List<IKTarget> _ikTargets = new();

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

        private Gorillamotion2 _gorilla;

        public Transform MountPoint => _mountPoint;
        public Rigidbody Body { get; private set; }
        public bool BeingMounted { get; private set; }
        public HandPlayer MountedBy { get; private set; }

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _gorilla = GetComponent<Gorillamotion2>();

            SetIKActivate(false);
        }

        private void FixedUpdate()
        {
            if (MountedBy != null)
            {
                _gorilla.Jump();
            }
        }

        private void Update()
        {
            if (MountedBy != null)
            {
                _gorilla.ApplyGorillamotion();
            }
        }

        private void LateUpdate()
        {
            if (MountedBy != null)
            {
                SyncPlayerMountable();
                UpdateIKTargets();
            }
        }

        private void SyncPlayerMountable()
        {
            // Update rotation
            transform.rotation = Quaternion.Euler(0, MountedBy.HeadCamera.transform.eulerAngles.y, 0);

            // Update mapped hands
            _mappedHandLeft.position = PlayerToMountable(MountedBy.HandLeft.follow.position);
            _mappedHandLeft.rotation = MountedBy.HandLeft.follow.rotation;

            _mappedHandRight.position = PlayerToMountable(MountedBy.HandRight.follow.position);
            _mappedHandRight.rotation = MountedBy.HandRight.follow.rotation;

            Vector3 PlayerToMountable(Vector3 playerHandPosition)
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
                    normalizedPos.x * (_mountableSpaceSize.x / _mountableSpaceCenter.lossyScale.x),
                    normalizedPos.y * (_mountableSpaceSize.y / _mountableSpaceCenter.lossyScale.y),
                    normalizedPos.z * (_mountableSpaceSize.z / _mountableSpaceCenter.lossyScale.z));

                return _mountableSpaceCenter.TransformPoint(mountableRelativePos);
            }
        }

        private void UpdateIKTargets()
        {
            for (int i = 0; i < _ikTargets.Count; i++)
            {
                var ikTarget = _ikTargets[i];
                Transform follow = ikTarget.Follow switch
                {
                    IKTargetFollowType.Left => _mappedHandLeft,
                    IKTargetFollowType.Right => _mappedHandRight,
                    _ => _mappedHandLeft
                };

                ikTarget.Target.position = follow.position;
                ikTarget.Target.rotation = follow.rotation;

                ikTarget.Target.localPosition = new Vector3(
                    ikTarget.FreezePosition.X ? 0 : ikTarget.Target.localPosition.x,
                    ikTarget.FreezePosition.Y ? 0 : ikTarget.Target.localPosition.y,
                    ikTarget.FreezePosition.Z ? 0 : ikTarget.Target.localPosition.z);

                ikTarget.Target.localRotation = Quaternion.Euler(
                    new Vector3(
                        ikTarget.FreezeRotation.X ? 0 : ikTarget.Target.localEulerAngles.x,
                        ikTarget.FreezeRotation.Y ? 0 : ikTarget.Target.localEulerAngles.y,
                        ikTarget.FreezeRotation.Z ? 0 : ikTarget.Target.localEulerAngles.z));

                ikTarget.Target.position += ikTarget.PositionOffset;
                ikTarget.Target.rotation *= Quaternion.Euler(ikTarget.RotationOffset);
            }
        }

        internal void OnBeforeMount(HandPlayer player)
        {
            OnBeforeMountEvent?.Invoke(player, this);

            BeingMounted = true;
        }

        internal void OnMount(HandPlayer player)
        {
            _playerSpaceCenter = player.HandSpaceCenter;

            _onMount?.Invoke(player, this);
            OnMountEvent?.Invoke(player, this);

            MountedBy = player;
            BeingMounted = false;

            // TODO: 按侧边键开始控制，送侧边键停止控制
            StartControlMovement();
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

        private void StartControlMovement()
        {
            SetIKActivate(true);
        }

        private void StopControlMovement()
        {
            SetIKActivate(false);
        }

        private void SetIKActivate(bool isActive)
        {
            foreach (var ikTarget in _ikTargets)
            {
                DOTween.Kill(ikTarget.Limb.solver, false);

                if (isActive)
                {
                    ikTarget.Limb.solver.IKPositionWeight = 1;
                    ikTarget.Limb.solver.IKRotationWeight = 1;
                }
                else
                {
                    DOTween.To(() => ikTarget.Limb.solver.IKPositionWeight, x => ikTarget.Limb.solver.IKPositionWeight = x, 0f, _limbResetDuration)
                        .SetTarget(ikTarget.Limb.solver).SetEase(Ease.OutQuad);
                    DOTween.To(() => ikTarget.Limb.solver.IKRotationWeight, x => ikTarget.Limb.solver.IKRotationWeight = x, 0f, _limbResetDuration)
                        .SetTarget(ikTarget.Limb.solver).SetEase(Ease.OutQuad);
                }
            }
        }

#if UNITY_EDITOR
        [AutoToggleHeader("Debug")]
        public bool EnableDebug = false;

        // Debug: 一键快速上下马
        [SerializeField, EnableIf(nameof(EnableDebug))]
        private bool _quickMount = true;
        [SerializeField, EnableIf(nameof(EnableDebug), nameof(_quickMount))]
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

            if (_mountableSpaceCenter != null)
            {
                Gizmos.matrix = _mountableSpaceCenter.localToWorldMatrix;
                var size = Vector3.Scale(
                    _mountableSpaceSize,
                    new Vector3(
                        1 / _mountableSpaceCenter.lossyScale.x,
                        1 / _mountableSpaceCenter.lossyScale.y,
                        1 / _mountableSpaceCenter.lossyScale.z));
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.color = new Color(1, 0, 0, 0.6f);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            // Mapping lines on play mode
            if (Application.isPlaying && MountedBy != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(HandPlayer.Instance.HandLeft.follow.position, _gorilla.LeftHand.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandLeft.follow.position, 0.05f);
                Gizmos.DrawSphere(_gorilla.LeftHand.position, 0.05f);

                Gizmos.DrawLine(HandPlayer.Instance.HandRight.follow.position, _gorilla.RightHand.position);
                Gizmos.DrawSphere(HandPlayer.Instance.HandRight.follow.position, 0.05f);
                Gizmos.DrawSphere(_gorilla.RightHand.position, 0.05f);
            }
        }

        #endregion
    }
}