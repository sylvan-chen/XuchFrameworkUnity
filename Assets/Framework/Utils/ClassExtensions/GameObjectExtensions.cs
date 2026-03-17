using UnityEngine;

namespace Framework.Extensions
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : UnityEngine.Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        public static Component GetOrAddComponent(this GameObject gameObject, System.Type type)
        {
            var component = gameObject.GetComponent(type);
            if (component == null)
            {
                component = gameObject.AddComponent(type);
            }
            return component;
        }

        public static Transform FindOrCreate(this Transform transform, string name)
        {
            var result = transform.Find(name);
            if (result == null)
            {
                result = new GameObject(name).transform;
                result.SetParent(transform);
                result.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            return result;
        }
    }
}