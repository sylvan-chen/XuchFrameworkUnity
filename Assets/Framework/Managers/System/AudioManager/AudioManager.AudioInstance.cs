using DigiEden.Framework.Utils;
using UnityEngine;

namespace DigiEden.Framework
{
    public partial class AudioManager
    {
        public enum PlayMode
        {
            Single = 0,
            Loop
        }

        public enum StopMode
        {
            FadeOut = 0,
            Immediate
        }

        public enum AudioSourceType
        {
            Flattened = 0,
            Transform,
            TransformWithVelocity,
            TransformWithRigidbody,
            Position
        }

        public struct AudioSourceInfo
        {
            public Transform Source;

            public Rigidbody Body;

            public Vector3 Velocity;

            public Vector3 Position;
        }

        /// <summary>
        /// 音频实例
        /// </summary>
        public class AudioInstance : ICache
        {
            public int HandleId;

            public PlayMode PlayMode;

            public string EventName;

            public FMOD.Studio.EventInstance EventInstance;

            public AudioSourceType SourceType;

            public AudioSourceInfo SourceInfo;

            public int LoopTimes;

            public float IdleDuration;

            public bool IsWaitingLoop;

            public static AudioInstance Create(
                int handleId, PlayMode playMode, string eventName, FMOD.Studio.EventInstance eventInstance, AudioSourceType sourceType,
                AudioSourceInfo sourceInfo, int loopTimes = 0)
            {
                var inst = CachePool.Spawn<AudioInstance>();
                inst.HandleId = handleId;
                inst.PlayMode = playMode;
                inst.EventName = eventName;
                inst.EventInstance = eventInstance;
                inst.SourceType = sourceType;
                inst.SourceInfo = sourceInfo;
                inst.LoopTimes = loopTimes;
                inst.IdleDuration = 0f;
                inst.IsWaitingLoop = false;
                return inst;
            }

            public bool IsPlaying
            {
                get
                {
                    EventInstance.getPlaybackState(out var state);
                    return state is FMOD.Studio.PLAYBACK_STATE.PLAYING or FMOD.Studio.PLAYBACK_STATE.STARTING;
                }
            }

            public bool IsStopped
            {
                get
                {
                    EventInstance.getPlaybackState(out var state);
                    return state is FMOD.Studio.PLAYBACK_STATE.STOPPED;
                }
            }

            public bool IsPaused
            {
                get
                {
                    EventInstance.getPaused(out var paused);
                    return paused;
                }
            }

            public bool Is3D => SourceType != AudioSourceType.Flattened;

            internal void UpdatePosition()
            {
                switch (SourceType)
                {
                    case AudioSourceType.Flattened:
                        return;
                    case AudioSourceType.Transform:
                        EventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(SourceInfo.Source));
                        break;
                    case AudioSourceType.TransformWithRigidbody:
                        EventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(SourceInfo.Source, SourceInfo.Body));
                        break;
                    case AudioSourceType.TransformWithVelocity:
                        EventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(SourceInfo.Source, SourceInfo.Velocity));
                        break;
                    case AudioSourceType.Position:
                        EventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(SourceInfo.Position));
                        break;
                    default:
                        Log.Error($"[AudioInstance] Unknown AudioSourceType: {SourceType}");
                        break;
                }
            }

            public void Release()
            {
                Clear();
                EventInstance.release();
                CachePool.Unspawn(this);
            }

            public void Clear()
            {
                EventInstance.setCallback(null);
                if (!IsStopped)
                {
                    EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                }
                LoopTimes = 0;
                IdleDuration = 0f;
                IsWaitingLoop = false;
            }
        }
    }
}