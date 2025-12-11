using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Xuch/Audio Manager")]
    public partial class AudioManager : ManagerBase
    {
        // 30 秒自动释放空闲的音频实例
        private const float MAX_IDLE_DURATION = 30f;

        [SerializeField]
        private bool _autoLoadAllBanks = false;
        [SerializeField]
        private string _bankAddressLabel = "bank";
        [SerializeField]
        private string _bankAddressPrefix = "Assets/Res/Banks";

        private readonly List<string> _loadedBankNames = new();
        private readonly Dictionary<string, List<AudioInstance>> _cachedInstances = new();
        private readonly Dictionary<int, AudioInstance> _audioHandles = new();
        private readonly List<AudioInstance> _activeInstances = new();

        private readonly Dictionary<int, float> _loopIntervals = new();
        private readonly List<AudioInstance> _releasingInstances = new();

        private readonly Stack<int> _handleIdPool = new();

        private FMOD.Studio.EventInstance _bgmInstance;

        #region 生命周期

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_bankAddressPrefix.EndsWith("/"))
            {
                _bankAddressPrefix = _bankAddressPrefix.Substring(0, _bankAddressPrefix.Length - 1);
            }
        }

        protected override async UniTask OnStartupAsync()
        {
            await base.OnStartupAsync();
            if (_autoLoadAllBanks)
                await LoadAllBanksAsync();
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            foreach (var audioInstances in _cachedInstances.Values)
            {
                foreach (var instance in audioInstances)
                {
                    instance.Release();
                }
            }

            _cachedInstances.Clear();
            _activeInstances.Clear();
            _audioHandles.Clear();
            _loopIntervals.Clear();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            float deltaTime = Time.unscaledDeltaTime;

            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                var activeInst = _activeInstances[i];

                // 空间信息更新
                if (activeInst.IsPlaying && activeInst.Is3D)
                {
                    activeInst.UpdatePosition();
                }

                // 停止/循环状态检查
                if (activeInst.IsStopped)
                {
                    if (activeInst.PlayMode == PlayMode.Loop && activeInst.LoopTimes != 0)
                    {
                        if (activeInst.IsWaitingLoop)
                            continue;

                        if (_loopIntervals.TryGetValue(activeInst.HandleId, out float interval))
                            ReplayWithInterval(activeInst, interval).Forget();
                        else
                            Replay(activeInst);

                        if (activeInst.LoopTimes > 0)
                            activeInst.LoopTimes--;
                    }
                    else
                    {
                        _activeInstances.RemoveAt(i);
                        _audioHandles.Remove(activeInst.HandleId);
                    }
                }
            }

            // 空闲实例清理检查
            _releasingInstances.Clear();
            foreach (var cachedInstances in _cachedInstances.Values)
            {
                foreach (var cachedInst in cachedInstances)
                {
                    if (!cachedInst.IsStopped || cachedInst.IsWaitingLoop)
                        continue;

                    cachedInst.IdleDuration += deltaTime;
                    if (cachedInst.IdleDuration >= MAX_IDLE_DURATION)
                    {
                        _releasingInstances.Add(cachedInst);
                        cachedInst.Release();
                    }
                }
            }

            foreach (var releasingInstance in _releasingInstances)
            {
                _cachedInstances.Remove(releasingInstance.EventName);
            }
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 加载所有 Banks
        /// </summary>
        private async UniTask LoadAllBanksAsync()
        {
            var handle = await App.ResourceManager.LoadAssetsAsync<TextAsset>(_bankAddressLabel);
            if (handle.IsValid)
            {
                var results = handle.Asset;
                foreach (var asset in results)
                {
                    FMODUnity.RuntimeManager.LoadBank(asset);
                    _loadedBankNames.Add(asset.name);
                }
                handle.Release();
            }
        }

        /// <summary>
        /// 加载 Bank
        /// </summary>
        public async UniTask LoadBankAsync(string bankName)
        {
            var handle = await App.ResourceManager.LoadAssetAsync<TextAsset>(GetBankAddress(bankName));
            if (handle.IsValid)
            {
                var asset = handle.Asset;
                FMODUnity.RuntimeManager.LoadBank(asset);
                _loadedBankNames.Add(bankName);
                handle.Release();
            }
        }

        /// <summary>
        /// 加载 Bank（回调方式）
        /// </summary>
        public void LoadBank(string bankName, Action callback = null)
        {
            App.ResourceManager.LoadAssetAsync<TextAsset>(
                GetBankAddress(bankName),
                handle =>
                {
                    if (handle.IsValid)
                    {
                        var asset = handle.Asset;
                        FMODUnity.RuntimeManager.LoadBank(asset);
                        _loadedBankNames.Add(bankName);
                        handle.Release();
                        callback?.Invoke();
                    }
                });
        }

        private string GetBankAddress(string bankName)
        {
            if (!bankName.StartsWith(_bankAddressPrefix))
                return _bankAddressPrefix + bankName;
            else
                return bankName;
        }

        /// <summary>
        /// Bank 是否已经加载
        /// </summary>
        public bool IsBankLoaded(string bankName)
        {
            return _loadedBankNames.Any(x => x == bankName);
        }

        #endregion

        #region 播放控制

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <returns>音频句柄 Id</returns>
        public int Play(string eventName, Transform source)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.Transform;

            return PlayInternal(eventName, sourceType, source, null, null, null, PlayMode.Single, 0, 0, 0);
        }

        /// <summary>
        /// 播放音频（带速度）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <param name="velocity">音源速度</param>
        /// <returns>音频句柄 Id</returns>
        public int Play(string eventName, Transform source, Vector3 velocity)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.TransformWithVelocity;

            return PlayInternal(eventName, sourceType, source, null, velocity, null, PlayMode.Single, 0, 0, 0);
        }

        /// <summary>
        /// 播放音频（带刚体速度）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <param name="sourceBody">音源刚体</param>
        /// <returns>音频句柄 Id</returns>
        public int Play(string eventName, Transform source, Rigidbody sourceBody)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else if (sourceBody == null)
                sourceType = AudioSourceType.Transform;
            else
                sourceType = AudioSourceType.TransformWithRigidbody;

            return PlayInternal(eventName, sourceType, source, sourceBody, null, null, PlayMode.Single, 0, 0, 0);
        }

        /// <summary>
        /// 播放音频（指定位置）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="position">音源位置</param>
        /// <returns>音频句柄 Id</returns>
        public int Play(string eventName, Vector3 position)
        {
            return PlayInternal(eventName, AudioSourceType.Position, null, null, null, position, PlayMode.Single, 0, 0, 0);
        }

        /// <summary>
        /// 播放音频（循环）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <param name="interval">循环间隔时间</param>
        /// <param name="loopTimes">循环次数（-1 表示无限循环）</param>
        /// <returns>音频句柄 Id</returns>
        public int PlayLoop(string eventName, Transform source, float interval = 0, int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.Transform;

            return PlayInternal(eventName, sourceType, source, null, null, null, PlayMode.Loop, interval, loopTimes, 0);
        }

        /// <summary>
        /// 播放音频（循环，带速度）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <param name="velocity">音源速度</param>
        /// <param name="interval">循环间隔时间</param>
        /// <param name="loopTimes">循环次数（-1 表示无限循环）</param>
        /// <returns>音频句柄 Id</returns>
        public int PlayLoop(string eventName, Transform source, Vector3 velocity, float interval = 0, int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else
                sourceType = AudioSourceType.TransformWithVelocity;

            return PlayInternal(eventName, sourceType, source, null, velocity, null, PlayMode.Loop, interval, loopTimes, 0);
        }

        /// <summary>
        /// 播放音频（循环，带刚体速度）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="source">音源</param>
        /// <param name="sourceBody">音源刚体</param>
        /// <param name="interval">循环间隔时间</param>
        /// <param name="loopTimes">循环次数（-1 表示无限循环）</param>
        /// <returns>音频句柄 Id</returns>
        public int PlayLoop(string eventName, Transform source, Rigidbody sourceBody, float interval = 0, int loopTimes = -1)
        {
            AudioSourceType sourceType;
            if (source == null)
                sourceType = AudioSourceType.Flattened;
            else if (sourceBody == null)
                sourceType = AudioSourceType.Transform;
            else
                sourceType = AudioSourceType.TransformWithRigidbody;

            return PlayInternal(eventName, sourceType, source, sourceBody, null, null, PlayMode.Loop, interval, loopTimes, 0);
        }

        /// <summary>
        /// 播放音频（循环）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="position">音源位置</param>
        /// <param name="interval">循环间隔时间</param>
        /// <param name="loopTimes">循环次数（-1 表示无限循环）</param>
        /// <returns>音频句柄 Id</returns>
        public int PlayLoop(string eventName, Vector3 position, float interval = 0, int loopTimes = -1)
        {
            return PlayInternal(eventName, AudioSourceType.Position, null, null, null, position, PlayMode.Loop, interval, loopTimes, 0);
        }

        /// <summary>
        /// 播放音频（2D）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <returns>音频句柄 Id</returns>
        public int Play2D(string eventName)
        {
            return PlayInternal(eventName, AudioSourceType.Flattened, null, null, null, null, PlayMode.Single, 0, 0, 0);
        }

        /// <summary>
        /// 播放音频（2D 循环）
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="interval">循环间隔时间</param>
        /// <param name="loopTimes">循环次数（-1 表示无限循环）</param>
        /// <returns>音频句柄 Id</returns>
        public int Play2DLoop(string eventName, float interval = 0, int loopTimes = -1)
        {
            return PlayInternal(eventName, AudioSourceType.Flattened, null, null, null, null, PlayMode.Loop, interval, loopTimes, 0);
        }

        /// <summary>
        /// 停止音频
        /// </summary>
        /// <param name="handleId">句柄 Id</param>
        /// <param name="stopMode">停止模式</param>
        public void Stop(int handleId, StopMode stopMode = StopMode.FadeOut)
        {
            var mode = GetFmodStopMode(stopMode);

            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.stop(mode);
                _activeInstances.Remove(audioInstance);
                _audioHandles.Remove(handleId);
            }
        }

        /// <summary>
        /// 停止事件对应的所有音频
        /// </summary>
        /// <param name="eventName">Event 名字</param>
        /// <param name="stopMode">停止模式</param>
        public void StopEvent(string eventName, StopMode stopMode = StopMode.FadeOut)
        {
            if (_cachedInstances.TryGetValue(eventName, out var audioInstances))
            {
                var mode = GetFmodStopMode(stopMode);

                foreach (var instance in audioInstances)
                {
                    instance.EventInstance.stop(mode);
                    _activeInstances.Remove(instance);
                    _audioHandles.Remove(instance.HandleId);
                }
            }
        }

        /// <summary>
        /// 停止所有音频
        /// </summary>
        /// <param name="stopMode">停止模式</param>
        public void StopAll(StopMode stopMode = StopMode.FadeOut)
        {
            foreach (var instance in _activeInstances)
            {
                instance.EventInstance.stop(GetFmodStopMode(stopMode));
            }
            _activeInstances.Clear();
            _audioHandles.Clear();
        }

        private FMOD.Studio.STOP_MODE GetFmodStopMode(StopMode stopMode)
        {
            return stopMode switch
            {
                StopMode.FadeOut => FMOD.Studio.STOP_MODE.ALLOWFADEOUT,
                StopMode.Immediate => FMOD.Studio.STOP_MODE.IMMEDIATE,
                _ => FMOD.Studio.STOP_MODE.IMMEDIATE
            };
        }

        /// <summary>
        /// 暂停音频
        /// </summary>
        public void Pause(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setPaused(true);
            }
        }

        /// <summary>
        /// 暂停事件对应的所有音频
        /// </summary>
        public void PauseEvent(string eventName)
        {
            if (_cachedInstances.TryGetValue(eventName, out var audioInstances))
            {
                foreach (var instance in audioInstances)
                {
                    instance.EventInstance.setPaused(true);
                }
            }
        }

        /// <summary>
        /// 暂停所有音频
        /// </summary>
        public void PauseAll()
        {
            foreach (var instance in _activeInstances)
            {
                instance.EventInstance.setPaused(true);
            }
        }

        /// <summary>
        /// 恢复音频
        /// </summary>
        /// <param name="handleId"></param>
        public void Resume(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setPaused(false);
            }
        }

        /// <summary>
        /// 恢复事件对应的所有音频
        /// </summary>
        public void ResumeEvent(string eventName)
        {
            if (_cachedInstances.TryGetValue(eventName, out var audioInstances))
            {
                foreach (var instance in audioInstances)
                {
                    instance.EventInstance.setPaused(false);
                }
            }
        }

        /// <summary>
        /// 恢复所有音频
        /// </summary>
        public void ResumeAll()
        {
            foreach (var instance in _activeInstances)
            {
                instance.EventInstance.setPaused(false);
            }
        }

        /// <summary>
        /// 暂停 Bus
        /// </summary>
        public void PauseBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setPaused(true);
        }

        /// <summary>
        /// 恢复 Bus
        /// </summary>
        public void ResumeBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setPaused(false);
        }

        /// <summary>
        /// 音频是否正在播放
        /// </summary>
        public bool IsPlaying(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                return audioInstance.IsPlaying;
            }
            return false;
        }

        /// <summary>
        /// 音频是否暂停
        /// </summary>
        public bool IsPaused(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                return audioInstance.IsPaused;
            }
            return false;
        }

        #endregion

        #region 音量控制

        /// <summary>
        /// 设置音量
        /// </summary>
        public void SetVolume(int handleId, float volume)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setVolume(volume);
            }
        }

        /// <summary>
        /// 获取音量
        /// </summary>
        public float GetVolume(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getVolume(out var volume);
                return volume;
            }
            return 0f;
        }

        /// <summary>
        /// 设置音高
        /// </summary>
        public void SetPitch(int handleId, float pitch)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setPitch(pitch);
            }
        }

        /// <summary>
        /// 获取音高
        /// </summary>
        public float GetPitch(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getPitch(out var pitch);
                return pitch;
            }
            return 1f;
        }

        /// <summary>
        /// 设置事件对应的所有实例音量
        /// </summary>
        public void SetVolumeEvent(string eventName, float volume)
        {
            if (_cachedInstances.TryGetValue(eventName, out var audioInstance))
            {
                foreach (var instance in audioInstance)
                {
                    instance.EventInstance.setVolume(volume);
                }
            }
        }

        /// <summary>
        /// 设置事件对应的所有实例音高
        /// </summary>
        public void SetPitchEvent(string eventName, float pitch)
        {
            if (_cachedInstances.TryGetValue(eventName, out var audioInstance))
            {
                foreach (var instance in audioInstance)
                {
                    instance.EventInstance.setPitch(pitch);
                }
            }
        }

        /// <summary>
        /// 设置 VCA 音量
        /// </summary>
        public void SetVolumeVCA(string vcaPath, float volume)
        {
            var vca = FMODUnity.RuntimeManager.GetVCA(vcaPath);
            vca.setVolume(volume);
        }

        /// <summary>
        /// 获取 VCA 音量
        /// </summary>
        public float GetVolumeVCA(string vcaPath)
        {
            var vca = FMODUnity.RuntimeManager.GetVCA(vcaPath);
            vca.getVolume(out var volume);
            return volume;
        }

        /// <summary>
        /// 设置 Bus 音量
        /// </summary>
        public void SetVolumeBus(string busPath, float volume)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setVolume(volume);
        }

        /// <summary>
        /// 获取 Bus 音量
        /// </summary>
        public float GetVolumeBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.getVolume(out var volume);
            return volume;
        }

        /// <summary>
        /// 设置 Bus 静音状态
        /// </summary>
        public void SetMuteBus(string busPath, bool mute)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.setMute(mute);
        }

        /// <summary>
        /// 获取 Bus 静音状态
        /// </summary>
        public bool GetMuteBus(string busPath)
        {
            var bus = FMODUnity.RuntimeManager.GetBus(busPath);
            bus.getMute(out var mute);
            return mute;
        }

        #endregion

        #region 参数控制

        /// <summary>
        /// 获取音频长度（秒）
        /// </summary>
        public float GetAudioLength(string eventName)
        {
            var desc = FMODUnity.RuntimeManager.GetEventDescription(FMODUnity.RuntimeManager.PathToGUID(eventName));
            desc.getLength(out int length);
            return length / 1000f;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void SetParameter(int handleId, string parameterName, float value)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setParameterByName(parameterName, value);
            }
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public float GetParameter(int handleId, string parameterName)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getParameterByName(parameterName, out var value);
                return value;
            }
            return 0f;
        }

        /// <summary>
        /// 设置时间轴位置（毫秒）
        /// </summary>
        public void SetTimelinePosition(int handleId, int position)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.setTimelinePosition(position);
            }
        }

        /// <summary>
        /// 获取时间轴位置（毫秒）
        /// </summary>
        public int GetTimelinePosition(int handleId)
        {
            if (_audioHandles.TryGetValue(handleId, out var audioInstance))
            {
                audioInstance.EventInstance.getTimelinePosition(out var position);
                return position;
            }
            return 0;
        }

        #endregion

        #region Bgm 控制

        /// <summary>
        /// 切换 Bgm
        /// </summary>
        public void SwitchBgm(string bgmName)
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
                Log.Error($"Invalid BGM event: {bgmName}");
                return;
            }

            bgmDesc.createInstance(out _bgmInstance);

            _bgmInstance.start();
        }

        /// <summary>
        /// 停止 Bgm
        /// </summary>
        public void StopBgm(StopMode stopMode = StopMode.FadeOut)
        {
            _bgmInstance.stop(GetFmodStopMode(stopMode));
        }

        /// <summary>
        /// 暂停 Bgm
        /// </summary>
        public void PauseBgm()
        {
            _bgmInstance.setPaused(true);
        }

        /// <summary>
        /// 恢复 Bgm
        /// </summary>
        public void ResumeBgm()
        {
            _bgmInstance.setPaused(false);
        }

        /// <summary>
        /// 设置 Bgm 音量
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            _bgmInstance.setVolume(volume);
        }

        /// <summary>
        /// 获取 Bgm 音量
        /// </summary>
        public float GetBgmVolume()
        {
            _bgmInstance.getVolume(out var volume);
            return volume;
        }

        /// <summary>
        /// 设置 Bgm 参数
        /// </summary>
        public void SetBgmParameter(string parameterName, float value)
        {
            _bgmInstance.setParameterByName(parameterName, value);
        }

        /// <summary>
        /// 获取 Bgm 参数
        /// </summary>
        public float GetBgmParameter(string parameterName)
        {
            _bgmInstance.getParameterByName(parameterName, out var value);
            return value;
        }

        /// <summary>
        /// Bgm 是否正在播放
        /// </summary>
        public bool IsBgmPlaying()
        {
            _bgmInstance.getPlaybackState(out var state);
            return state is FMOD.Studio.PLAYBACK_STATE.PLAYING or FMOD.Studio.PLAYBACK_STATE.STARTING;
        }

        /// <summary>
        /// Bgm 是否暂停
        /// </summary>
        public bool IsBgmPaused()
        {
            _bgmInstance.getPaused(out bool paused);
            return paused;
        }

        #endregion

        #region 内部方法

        private int PlayInternal(
            string eventName, AudioSourceType sourceType, Transform source, Rigidbody body, Vector3? velocity, Vector3? position, PlayMode playMode,
            float interval, int loopTimes, int timelinePos)
        {
            AudioInstance audioInstance = GetOrCraeteIdleInstance();

            if (audioInstance.Is3D)
            {
                audioInstance.UpdatePosition();
            }

            if (playMode == PlayMode.Loop && interval > 0)
            {
                _loopIntervals[audioInstance.HandleId] = interval;
            }

            audioInstance.EventInstance.setTimelinePosition(timelinePos);
            // audioInstance.EventInstance.setCallback(EventInstanceCallback);

            audioInstance.EventInstance.start();

#if UNITY_EDITOR
            // 编辑器验证一下三个列表的同步情况，检验逻辑错误
            if (_activeInstances.Contains(audioInstance))
            {
                throw new InvalidOperationException(
                    $"[AudioManager] On Creating new audio instance: activeInstances already contains the handle id {audioInstance.HandleId} for event {eventName}");
            }

            if (_audioHandles.ContainsKey(audioInstance.HandleId))
            {
                throw new InvalidOperationException(
                    $"[AudioManager] On Creating new audio instance: audioHandles already contains the handle id {audioInstance.HandleId} for event {eventName}");
            }
#endif

            _activeInstances.Add(audioInstance);
            _audioHandles.Add(audioInstance.HandleId, audioInstance);

            return audioInstance.HandleId;

            AudioInstance GetOrCraeteIdleInstance()
            {
                AudioInstance instance = null;
                var sourceInfo = new AudioSourceInfo()
                {
                    Body = body,
                    Source = source,
                    Position = position ?? Vector3.zero,
                    Velocity = velocity ?? Vector3.zero
                };

                if (_cachedInstances.TryGetValue(eventName, out var existingInstances))
                {
                    instance = existingInstances.FirstOrDefault(x => x.IsStopped && !x.IsWaitingLoop);
                }

                if (instance != null)
                {
                    instance.Clear();
                    instance.PlayMode = playMode;
                    instance.SourceType = sourceType;
                    instance.SourceInfo = sourceInfo;
                    instance.LoopTimes = loopTimes;
                }
                else
                {
                    var eventDesc = FMODUnity.RuntimeManager.GetEventDescription(eventName);
                    if (!eventDesc.isValid())
                    {
                        Log.Error($"Invalid FMOD event: {eventName}");
                        return null;
                    }

                    eventDesc.createInstance(out var eventInstance);
                    instance = AudioInstance.Create(GetNextHandleId(), playMode, eventName, eventInstance, sourceType, sourceInfo, loopTimes);

                    if (!_cachedInstances.ContainsKey(eventName))
                    {
                        _cachedInstances[eventName] = new List<AudioInstance>();
                    }

                    _cachedInstances[eventName].Add(instance);
                }

                return instance;
            }
        }

        private int GetNextHandleId()
        {
            if (_handleIdPool.Count > 0)
            {
                return _handleIdPool.Pop();
            }
            else
            {
                return _audioHandles.Count + 1;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        private static FMOD.RESULT EventInstanceCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameters)
        {
            return FMOD.RESULT.OK;
        }

        private async UniTaskVoid ReplayWithInterval(AudioInstance audioInstance, float interval)
        {
            audioInstance.IsWaitingLoop = true;
            await UniTask.Delay(TimeSpan.FromSeconds(interval));
            Replay(audioInstance);
            audioInstance.IsWaitingLoop = false;
        }

        private void Replay(AudioInstance audioInstance)
        {
            if (audioInstance.Is3D)
            {
                audioInstance.UpdatePosition();
            }
            audioInstance.EventInstance.setTimelinePosition(0);
            audioInstance.EventInstance.start();
        }

        #endregion
    }
}