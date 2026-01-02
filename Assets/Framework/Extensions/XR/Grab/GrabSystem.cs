using XuchFramework.Core.ECS;

namespace Framework.Extensions.XR
{
    public class GrabSystem : GameSystemBase
    {
        private EntityDataPool<GrabbableData> _grabbablePool;

        protected override void OnInitialize()
        {
            _grabbablePool = GameContext.Instance.GetPool<GrabbableData>();
        }

        public void Grab(Hand hand, Grabbable grabbable) { }
    }
}