using UnityEngine;

namespace Xuch.Framework
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
                    return App.AudioManager.Play2D(eventName);
                }
                else if (body != null)
                {
                    return App.AudioManager.Play(eventName, source, body);
                }
                else if (velocity != null)
                {
                    return App.AudioManager.Play(eventName, source, velocity.Value);
                }
                else
                {
                    return App.AudioManager.Play(eventName, source);
                }
            }

            public static int Play3DLoop(string eventName, Transform source, Rigidbody body, Vector3? velocity, float internval, int loopTimes)
            {
                if (source == null)
                {
                    return App.AudioManager.Play2DLoop(eventName, internval, loopTimes);
                }
                else if (body != null)
                {
                    return App.AudioManager.PlayLoop(eventName, source, body, internval, loopTimes);
                }
                else if (velocity != null)
                {
                    return App.AudioManager.PlayLoop(eventName, source, velocity.Value, internval, loopTimes);
                }
                else
                {
                    return App.AudioManager.PlayLoop(eventName, source, internval, loopTimes);
                }
            }

            public static int Play2D(string eventName)
            {
                return App.AudioManager.Play2D(eventName);
            }

            public static int Play2DLoop(string eventName, float internval, int loopTimes)
            {
                return App.AudioManager.Play2DLoop(eventName, internval, loopTimes);
            }
        }
    }
}