using UnityEditor.UIElements;

namespace Alchemy.Editor.Drawers
{
    public abstract class TrackSerializedObjectAttributeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            TargetElement.TrackSerializedObjectValue(SerializedObject, x => { OnInspectorChanged(); });

            OnInspectorChanged();
            TargetElement.schedule.Execute(() => OnInspectorChanged());
        }

        protected abstract void OnInspectorChanged();

        protected bool AreConditionsMet(string[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
                return false;

            foreach (var condition in conditions)
            {
                if (!ReflectionHelper.GetValueBool(Target, condition))
                {
                    return false;
                }
            }

            return true;
        }
    }
}