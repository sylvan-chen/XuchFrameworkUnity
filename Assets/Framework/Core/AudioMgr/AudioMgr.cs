using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Core
{
    public class AudioMgr
    {
        /// <summary> 空闲音频实例的存活时间 </summary>
        public static float IdleInstanceTTL = 30f;

        private static readonly Dictionary<string, ResourceHandle<TextAsset>> _bankResourceHandleMap = new();
        private static readonly Dictionary<string, List<AudioInstance>> _cachedInstancesMap = new();
        private static readonly Dictionary<int, AudioInstance> _activeInstanceMap = new();
        private static readonly Dictionary<int, float> _activeLoopIntervalMap = new();

        private static readonly Stack<int> _handleIdPool = new();

        private static FMOD.Studio.EventInstance _bgmInstance;

        #region Lifecycle

        public static void Dispose()
        {
            foreach (var audioInstances in _cachedInstancesMap.Values)
            {
                foreach (var instance in audioInstances)
                {
                    instance.Release();
                }
            }

            UnloadAllBanks();
            _bankResourceHandleMap.Clear();

            _cachedInstancesMap.Clear();
            _activeInstanceMap.Clear();
            _activeLoopIntervalMap.Clear();

            _handleIdPool.Clear();
            _currentHandleIdCapacity = 1;
        }

        public static void Update()
        {
            // Collect keys to remove to avoid modifying collection during enumeration
            var keysToRemove = new List<int>();
            var activateInstances = _activeInstanceMap.Values;
            foreach (var instance in activateInstances)
            {
                // Update spatial position
                if (instance.IsPlaying && instance.Is3D)
                {
                    instance.UpdatePosition();
                }

                // Stop or loop
                if (instance.IsStopped)
                {
                    if (instance.PlayMode == AudioPlayMode.Loop && instance.LoopTimes != 0)
                    {
                        if (instance.IsWaitingLoop) continue;

                        if (_activeLoopIntervalMap.TryGetValue(instance.HandleId, out float interval))
                            ReplayLater(instance, interval).Forget();
                        else
                            Replay(instance);

                        if (instance.LoopTimes > 0) instance.LoopTimes--;
                    }
                    else
                    {
                        keysToRemove.Add(instance.HandleId);
                    }
                }
            }

            // Remove collected keys after enumeration
            foreach (var key in keysToRemove)
            {
                _activeInstanceMap.Remove(key);
            }

            // Release idle instances
            var instancesToRemove = new List<(string eventName, AudioInstance instance)>();
            var cachedInstances = _cachedInstancesMap.Values;
            foreach (var instances in cachedInstances)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    var instance = instances[i];

                    if (!instance.IsStopped || instance.IsWaitingLoop) continue;

                    instance.IdleDuration += Time.unscaledDeltaTime;
                    if (instance.IdleDuration >= IdleInstanceTTL)
                    {
                        _handleIdPool.Push(instance.HandleId);
                        instance.Release();
                        instancesToRemove.Add((instance.EventName, instance));
                    }
                }
            }

            // Remove collected instances after enumeration
            foreach (var (eventName, instance) in instancesToRemove)
            {
                _cachedInstancesMap[eventName].Remove(instance);
            }
        }

        #endregion

        #region Resource Management

        public static async UniTask LoadBankAsync(string bankName)
        {
            var handle = await ResourceMgr.LoadAssetAsync<TextAsset>(bankName);
            if (handle.IsValid)
            {
                FMODUnity.RuntimeManager.LoadBank(handle.Asset);
                _bankResourceHandleMap.Add(bankName, handle);
            }
        }

        public static void LoadBankAsync(string bankName, Action callback)
        {
            ResourceMgr.LoadAssetAsync<TextAsset>(
                bankName,
                handle =>
                {
                    if (handle.IsValid)
                    {
                        FMODUnity.RuntimeManager.LoadBank(handle.Asset);
                        _bankResourceHandleMap.Add(bankName, handle);
                        callback?.Invoke();
                    }
                }
            );
        }

        public static void UnloadBank(string bankName)
        {
            if (_bankResourceHandleMap.TryGetValue(bankName, out var handle))
            {
                FMODUnity.RuntimeManager.UnloadBank(handle.Asset);
                handle.Release();
            }
            _bankResourceHandleMap.Remove(bankName);
        }

        public static void UnloadAllBanks()
        {
            foreach (var handle in _bankResourceHandleMap.Values)
            {
                FMODUnity.RuntimeManager.UnloadBank(handle.Asset);
                handle.Release();
            }
            _bankResourceHandleMap.Clear();
        }

        public static bool IsBankLoaded(string bankName)
        {
            return _bankResourceHandleMap.Keys.Any(x => x == bankName);
        }

        public static void ReleaseIdleInstancesImmediately()
        {
            var instancesToRemove = new List<(string eventName, AudioInstance instance)>();
            var cachedInstances = _cachedInstancesMap.Values;
            foreach (var instances in cachedInstances)
            {
                for (int i = 0; i < instances.Count; i++)
                {
                    var instance = instances[i];

                    if (!instance.IsStopped || instance.IsWaitingLoop) continue;

                    _handleIdPool.Push(instance.HandleId);
                    instance.Release();
                    instancesToRemove.Add((instance.EventName, instance));
                }
            }

            // Remove collected instances after enumeration
            foreach ((string eventName, AudioInstance instance) in instancesToRemove)
            {
                _cachedInstancesMap[eventName].Remove(instance);
            }
        }

        #endregion

        #region Play/Stop

        public static int Play(string eventName, Transform source)
        {
            var sourceType = source == null ? AudioSourceType.Flattened : AudioSourceType.Transform;
            return PlayInternal(eventName, sourceType, source, null, null, null, AudioPlayMode.Single, 0, 0, 0);
        }

        public static int Play(string eventName, Transform source, Vector3 velocity)
        {
            var sourceType = source == null ? AudioSourceType.Flattened : AudioSourceType.TransformWithVelocity;
            return PlayInternal(eventName, sourceType, source, null, velocity, null, AudioPlayMode.Single, 0, 0, 0);
        }

        public static int Play(string eventName, Transform source, Rigidbody sourceBody)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else if (sourceBody == null)
                sourceType = AudioSourceType.Transform;
            else
                sourceType = AudioSourceType.TransformWithRigidbody;

            return PlayInternal(eventName, sourceType, source, sourceBody, null, null, AudioPlayMode.Single, 0, 0, 0);
        }

        public static int Play(string eventName, Vector3 position)
        {
            return PlayInternal(
                eventName,
                AudioSourceType.Position,
                null,
                null,
                null,
                position,
                AudioPlayMode.Single,
                0,
                0,
                0
            );
        }

        public static int PlayLoop(string eventName, Transform source, float interval = 0, int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.Transform;

            return PlayInternal(
                eventName,
                sourceType,
                source,
                null,
                null,
                null,
                AudioPlayMode.Loop,
                interval,
                loopTimes,
                0
            );
        }

        public static int PlayLoop(
            string eventName,
            Transform source,
            Vector3 velocity,
            float interval = 0,
            int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.TransformWithVelocity;

            return PlayInternal(
                eventName,
                sourceType,
                source,
                null,
                velocity,
                null,
                AudioPlayMode.Loop,
                interval,
                loopTimes,
                0
            );
        }

        public static int PlayLoop(
            string eventName,
            Transform source,
            Rigidbody sourceBody,
            float interval = 0,
            int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else if (sourceBody == null)
                sourceType = AudioSourceType.Transform;
            else
                sourceType = AudioSourceType.TransformWithRigidbody;

            return PlayInternal(
                eventName,
                sourceType,
                source,
                sourceBody,
                null,
                null,
                AudioPlayMode.Loop,
                interval,
                loopTimes,
                0
            );
        }

        public static int PlayLoop(string eventName, Vector3 position, float interval = 0, int loopTimes = -1)
        {
            return PlayInternal(
                eventName,
                AudioSourceType.Position,
                null,
                null,
                null,
                position,
                AudioPlayMode.Loop,
                interval,
                loopTimes,
                0
            );
        }

        public static int Play2D(string eventName)
        {
            return PlayInternal(
                eventName,
                AudioSourceType.Flattened,
                null,
                null,
                null,
                null,
                AudioPlayMode.Single,
                0,
                0,
                0
            );
        }

        public static int Play2DLoop(string eventName, float interval = 0, int loopTimes = -1)
        {
            return PlayInternal(
                eventName,
                AudioSourceType.Flattened,
                null,
                null,
                null,
                null,
                AudioPlayMode.Loop,
                interval,
                loopTimes,
                0
            );
        }

        public static void Stop(int handleId, AudioStopMode stopMode = AudioStopMode.FadeOut)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.stop(GetFmodStopMode(stopMode));
                _activeInstanceMap.Remove(handleId);
            }
        }

        public static void StopEvent(string eventName, AudioStopMode stopMode = AudioStopMode.FadeOut)
        {
            if (_cachedInstancesMap.TryGetValue(eventName, out var cachedInstances))
            {
                var mode = GetFmodStopMode(stopMode);

                var keysToRemove = new List<int>();
                foreach (var instance in cachedInstances)
                {
                    instance.EventInstance.stop(mode);
                    keysToRemove.Add(instance.HandleId);
                }

                foreach (var key in keysToRemove)
                {
                    _activeInstanceMap.Remove(key);
                }
            }
        }

        public static void StopAll(AudioStopMode stopMode = AudioStopMode.FadeOut)
        {
            var mode = GetFmodStopMode(stopMode);

            foreach (var instance in _activeInstanceMap.Values)
            {
                instance.EventInstance.stop(mode);
            }
            _activeInstanceMap.Clear();
        }

        private static FMOD.Studio.STOP_MODE GetFmodStopMode(AudioStopMode stopMode)
        {
            return stopMode switch
            {
                AudioStopMode.FadeOut => FMOD.Studio.STOP_MODE.ALLOWFADEOUT,
                AudioStopMode.Immediate => FMOD.Studio.STOP_MODE.IMMEDIATE,
                _ => FMOD.Studio.STOP_MODE.IMMEDIATE
            };
        }

        #endregion

        #region Pause/Resume

        public static void Pause(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var instance))
            {
                instance.EventInstance.setPaused(true);
            }
        }

        public static void PauseEvent(string eventName)
        {
            if (_cachedInstancesMap.TryGetValue(eventName, out var cachedInstances))
            {
                foreach (var instance in cachedInstances)
                {
                    instance.EventInstance.setPaused(true);
                }
            }
        }

        public static void PauseAll()
        {
            foreach (var instance in _activeInstanceMap.Values)
            {
                instance.EventInstance.setPaused(true);
            }
        }

        public static void Resume(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var instance))
            {
                instance.EventInstance.setPaused(false);
            }
        }

        public static void ResumeEvent(string eventName)
        {
            if (_cachedInstancesMap.TryGetValue(eventName, out var cachedInstances))
            {
                foreach (var instance in cachedInstances)
                {
                    instance.EventInstance.setPaused(false);
                }
            }
        }

        public static void ResumeAll()
        {
            foreach (var instance in _activeInstanceMap.Values)
            {
                instance.EventInstance.setPaused(false);
            }
        }

        public static void PauseBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setPaused(true);
        }

        public static void ResumeBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setPaused(false);
        }

        public static bool IsPlaying(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                return audioInstance.IsPlaying;
            }
            return false;
        }

        public static bool IsPaused(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                return audioInstance.IsPaused;
            }
            return false;
        }

        #endregion

        #region Volume/Pitch Control

        public static void SetVolume(int handleId, float volume)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setVolume(volume);
            }
        }

        public static float GetVolume(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getVolume(out var volume);
                return volume;
            }
            return 0f;
        }

        public static void SetPitch(int handleId, float pitch)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setPitch(pitch);
            }
        }

        public static float GetPitch(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getPitch(out var pitch);
                return pitch;
            }
            return 1f;
        }

        public static void SetVolumeEvent(string eventName, float volume)
        {
            if (_cachedInstancesMap.TryGetValue(eventName, out var audioInstance))
            {
                foreach (var instance in audioInstance)
                {
                    instance.EventInstance.setVolume(volume);
                }
            }
        }

        public static void SetPitchEvent(string eventName, float pitch)
        {
            if (_cachedInstancesMap.TryGetValue(eventName, out var audioInstance))
            {
                foreach (var instance in audioInstance)
                {
                    instance.EventInstance.setPitch(pitch);
                }
            }
        }

        public static void SetVolumeVCA(string vcaPath, float volume)
        {
            var vca = FMODUnity.RuntimeManager.GetVCA(vcaPath);
            vca.setVolume(volume);
        }

        public static float GetVolumeVCA(string vcaPath)
        {
            var vca = FMODUnity.RuntimeManager.GetVCA(vcaPath);
            vca.getVolume(out var volume);
            return volume;
        }

        public static void SetVolumeBus(string busPath, float volume)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setVolume(volume);
        }

        public static float GetVolumeBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.getVolume(out var volume);
            return volume;
        }

        public static void SeMuteBus(string busPath, bool mute)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setMute(mute);
        }

        public static bool GetMuteBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.getMute(out var mute);
            return mute;
        }

        #endregion

        #region Parameters

        public static int GetAudioMilliseconds(string eventName)
        {
            var desc = FMODUnity.RuntimeManager.GetEventDescription(FMODUnity.RuntimeManager.PathToGUID(eventName));
            desc.getLength(out int milliseconds);
            return milliseconds;
        }

        public static float GetAudioSeconds(string eventName)
        {
            return GetAudioMilliseconds(eventName) / 1000f;
        }

        public static void SetParameter(int handleId, string parameterName, float value)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setParameterByName(parameterName, value);
            }
        }

        public static float GetParameter(int handleId, string parameterName)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getParameterByName(parameterName, out var value);
                return value;
            }
            return 0f;
        }

        public static void SetTimelinePosition(int handleId, int position)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setTimelinePosition(position);
            }
        }

        public static int GetTimelinePosition(int handleId)
        {
            if (_activeInstanceMap.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getTimelinePosition(out var position);
                return position;
            }
            return 0;
        }

        #endregion

        #region Bgm

        public static void SwitchBgm(string bgmName)
        {
            _bgmInstance.getPlaybackState(out var state);
            if (state is FMOD.Studio.PLAYBACK_STATE.PLAYING or FMOD.Studio.PLAYBACK_STATE.STARTING)
            {
                _bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _bgmInstance.release();
            }

            var bgmDesc = FMODUnity.RuntimeManager.GetEventDescription(bgmName);
            if (!bgmDesc.isValid())
            {
                Log.Error($"[AudioManager] Switch BGM failed. Invalid BGM event: {bgmName}");
                return;
            }

            bgmDesc.createInstance(out _bgmInstance);

            _bgmInstance.start();
        }

        public static void StopBgm(AudioStopMode stopMode = AudioStopMode.FadeOut)
        {
            _bgmInstance.stop(GetFmodStopMode(stopMode));
        }

        public static void PauseBgm()
        {
            _bgmInstance.setPaused(true);
        }

        public static void ResumeBgm()
        {
            _bgmInstance.setPaused(false);
        }

        public static void SetBgmVolume(float volume)
        {
            _bgmInstance.setVolume(volume);
        }

        public static float GetBgmVolume()
        {
            _bgmInstance.getVolume(out var volume);
            return volume;
        }

        public static void SetBgmParameter(string parameterName, float value)
        {
            _bgmInstance.setParameterByName(parameterName, value);
        }

        public static float GetBgmParameter(string parameterName)
        {
            _bgmInstance.getParameterByName(parameterName, out var value);
            return value;
        }

        public static bool IsBgmPlaying()
        {
            _bgmInstance.getPlaybackState(out var state);
            return state is FMOD.Studio.PLAYBACK_STATE.PLAYING or FMOD.Studio.PLAYBACK_STATE.STARTING;
        }

        public static bool IsBgmPaused()
        {
            _bgmInstance.getPaused(out bool paused);
            return paused;
        }

        #endregion

        #region Internal Methods

        private static int PlayInternal(
            string eventName,
            AudioSourceType sourceType,
            Transform source,
            Rigidbody body,
            Vector3? velocity,
            Vector3? position,
            AudioPlayMode playMode,
            float interval,
            int loopTimes,
            int timelinePos)
        {
            AudioInstance audioInstance = GetOrCreateIdleInstance();

            if (audioInstance.Is3D)
            {
                audioInstance.UpdatePosition();
            }

            if (playMode == AudioPlayMode.Loop && interval > 0)
            {
                _activeLoopIntervalMap[audioInstance.HandleId] = interval;
            }

            audioInstance.EventInstance.setTimelinePosition(timelinePos);
            // audioInstance.EventInstance.setCallback(EventInstanceCallback);

            audioInstance.EventInstance.start();

#if UNITY_EDITOR
            // Verify cache list in editor to avoid duplicate handle id issue
            if (_activeInstanceMap.ContainsKey(audioInstance.HandleId))
            {
                Log.Warning(
                    $"[AudioManager] On Creating new audio instance: audioHandles already contains the handle id {audioInstance.HandleId} for event {eventName}"
                );
                return audioInstance.HandleId;
            }
#endif

            _activeInstanceMap.Add(audioInstance.HandleId, audioInstance);

            return audioInstance.HandleId;

            AudioInstance GetOrCreateIdleInstance()
            {
                AudioInstance instance = null;
                var sourceInfo = new AudioSourceInfo()
                {
                    Body = body,
                    Source = source,
                    Position = position ?? Vector3.zero,
                    Velocity = velocity ?? Vector3.zero
                };

                if (_cachedInstancesMap.TryGetValue(eventName, out var cachedInstances))
                {
                    instance = cachedInstances.FirstOrDefault(x => x.IsStopped && !x.IsWaitingLoop);
                }

                if (instance != null)
                {
                    // Reuse idle instance
                    instance.Clear();
                    instance.PlayMode = playMode;
                    instance.SourceType = sourceType;
                    instance.SourceInfo = sourceInfo;
                    instance.LoopTimes = loopTimes;
                }
                else
                {
                    // Create new instance
                    var eventDesc = FMODUnity.RuntimeManager.GetEventDescription(eventName);
                    if (!eventDesc.isValid())
                    {
                        Log.Error($"[AudioManager] Acquired a invalid FMOD event: {eventName}");
                        return null;
                    }

                    eventDesc.createInstance(out var eventInstance);
                    instance = AudioInstance.Create(
                        GetNextHandleId(),
                        playMode,
                        eventName,
                        eventInstance,
                        sourceType,
                        sourceInfo,
                        loopTimes
                    );

                    if (!_cachedInstancesMap.ContainsKey(eventName))
                    {
                        _cachedInstancesMap[eventName] = new List<AudioInstance>();
                    }
                    _cachedInstancesMap[eventName].Add(instance);
                }

                return instance;
            }
        }

        private static int _currentHandleIdCapacity = 1;

        private static int GetNextHandleId()
        {
            if (_handleIdPool.Count > 0)
            {
                return _handleIdPool.Pop();
            }

            if (_currentHandleIdCapacity + 1 == int.MaxValue)
            {
                // Try to release idle instances to avoid handle id overflow
                ReleaseIdleInstancesImmediately();
                if (_handleIdPool.Count > 0)
                {
                    return _handleIdPool.Pop();
                }

                Log.Error(
                    $"[AudioManager] Too many audio instances created at the same time (more than {int.MaxValue}), handle id overflow!"
                );
                return -1;
            }

            return _currentHandleIdCapacity++;
        }

        private static void Replay(AudioInstance audioInstance)
        {
            if (audioInstance.Is3D)
            {
                audioInstance.UpdatePosition();
            }
            audioInstance.EventInstance.setTimelinePosition(0);
            audioInstance.EventInstance.start();
        }

        private static async UniTaskVoid ReplayLater(AudioInstance audioInstance, float delay)
        {
            audioInstance.IsWaitingLoop = true;
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            Replay(audioInstance);
            audioInstance.IsWaitingLoop = false;
        }

        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        private static FMOD.RESULT EventInstanceCallback(
            FMOD.Studio.EVENT_CALLBACK_TYPE type,
            IntPtr instancePtr,
            IntPtr parameters)
        {
            return FMOD.RESULT.OK;
        }

        #endregion
    }
}