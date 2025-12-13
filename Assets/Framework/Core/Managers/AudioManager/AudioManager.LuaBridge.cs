using UnityEngine;

namespace XuchFramework.Core
{
    public partial class AudioManager
    {
        /// <summary>
        /// Lua 专用桥接类（因为 Lua 识别不了重载）
        /// </summary>
        public static class LuaBridge
        {
            public static int Play3D(string eventName, Transform source, Rigidbody body, Vector3? velocity)
            {
                if (source == null)
                {
                    return GameModule<AudioManager>.Instance.Play2D(eventName);
                }
                else if (body != null)
                {
                    return GameModule<AudioManager>.Instance.Play(eventName, source, body);
                }
                else if (velocity != null)
                {
                    return GameModule<AudioManager>.Instance.Play(eventName, source, velocity.Value);
                }
                else
                {
                    return GameModule<AudioManager>.Instance.Play(eventName, source);
                }
            }

            public static int Play3DLoop(string eventName, Transform source, Rigidbody body, Vector3? velocity, float internval, int loopTimes)
            {
                if (source == null)
                {
                    return GameModule<AudioManager>.Instance.Play2DLoop(eventName, internval, loopTimes);
                }
                else if (body != null)
                {
                    return GameModule<AudioManager>.Instance.PlayLoop(eventName, source, body, internval, loopTimes);
                }
                else if (velocity != null)
                {
                    return GameModule<AudioManager>.Instance.PlayLoop(eventName, source, velocity.Value, internval, loopTimes);
                }
                else
                {
                    return GameModule<AudioManager>.Instance.PlayLoop(eventName, source, internval, loopTimes);
                }
            }

            public static int Play2D(string eventName)
            {
                return GameModule<AudioManager>.Instance.Play2D(eventName);
            }

            public static int Play2DLoop(string eventName, float internval, int loopTimes)
            {
                return GameModule<AudioManager>.Instance.Play2DLoop(eventName, internval, loopTimes);
            }
        }
    }
}