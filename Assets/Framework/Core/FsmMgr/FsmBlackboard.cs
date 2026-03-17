using System.Collections.Generic;

namespace Framework.Core
{
    public class FsmBlackboard
    {
        private readonly Dictionary<string, int> _intValues = new();
        private readonly Dictionary<string, float> _floatValues = new();
        private readonly Dictionary<string, string> _stringValues = new();
        private readonly Dictionary<string, bool> _boolValues = new();
        private readonly Dictionary<string, object> _objectValues = new();

        public void SetIntValue(string name, int value)
        {
            _intValues[name] = value;
        }

        public int GetIntValue(string name, int defaultValue = 0)
        {
            return _intValues.GetValueOrDefault(name, defaultValue);
        }

        public bool TryGetIntValue(string name, out int value)
        {
            return _intValues.TryGetValue(name, out value);
        }

        public void SetFloatValue(string name, float value)
        {
            _floatValues[name] = value;
        }

        public float GetFloatValue(string name, float defaultValue = 0f)
        {
            return _floatValues.GetValueOrDefault(name, defaultValue);
        }

        public bool TryGetFloatValue(string name, out float value)
        {
            return _floatValues.TryGetValue(name, out value);
        }

        public void SetStringValue(string name, string value)
        {
            _stringValues[name] = value;
        }

        public string GetStringValue(string name, string defaultValue = null)
        {
            return _stringValues.GetValueOrDefault(name, defaultValue);
        }

        public bool TryGetStringValue(string name, out string value)
        {
            return _stringValues.TryGetValue(name, out value);
        }

        public void SetBoolValue(string name, bool value)
        {
            _boolValues[name] = value;
        }

        public bool GetBoolValue(string name, bool defaultValue = false)
        {
            return _boolValues.GetValueOrDefault(name, defaultValue);
        }

        public bool TryGetBoolValue(string name, out bool value)
        {
            return _boolValues.TryGetValue(name, out value);
        }

        public void SetObjectValue(string name, object value)
        {
            _objectValues[name] = value;
        }

        public object GetObjectValue(string name, object defaultValue = null)
        {
            return _objectValues.GetValueOrDefault(name, defaultValue);
        }

        public T GetObjectValue<T>(string name, T defaultValue = default)
        {
            if (_objectValues.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool TryGetObjectValue(string name, out object value)
        {
            return _objectValues.TryGetValue(name, out value);
        }

        public bool TryGetObjectValue<T>(string name, out T value)
        {
            if (_objectValues.TryGetValue(name, out var obj) && obj is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        public bool HasValue(string name)
        {
            return _intValues.ContainsKey(name)
                   || _floatValues.ContainsKey(name)
                   || _stringValues.ContainsKey(name)
                   || _boolValues.ContainsKey(name)
                   || _objectValues.ContainsKey(name);
        }

        public void RemoveValue(string name)
        {
            _intValues.Remove(name);
            _floatValues.Remove(name);
            _stringValues.Remove(name);
            _boolValues.Remove(name);
            _objectValues.Remove(name);
        }

        public void Clear()
        {
            _intValues.Clear();
            _floatValues.Clear();
            _stringValues.Clear();
            _boolValues.Clear();
            _objectValues.Clear();
        }
    }
}