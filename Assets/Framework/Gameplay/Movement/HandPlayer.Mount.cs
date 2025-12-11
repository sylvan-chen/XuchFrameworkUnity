namespace Xuch.Gameplay
{
    public delegate void MountEvent(HandPlayer player, Mountable mountable);

    public partial class HandPlayer
    {
        public Mountable MountingObj { get; private set; }

        public MountEvent OnTriggerMount;
        public MountEvent OnBeforeMount;
        public MountEvent OnMount;
        public MountEvent OnTriggerDismount;
        public MountEvent OnBeforeDismount;
        public MountEvent OnDismount;

        public void Mount(Mountable mountable)
        {
            MountingObj = mountable;

            OnTriggerMount?.Invoke(this, MountingObj);

            Body.useGravity = false;
            Body.isKinematic = true;

            OnBeforeMount?.Invoke(this, MountingObj);
            MountingObj.OnBeforeMount(this);

            OnMount?.Invoke(this, MountingObj);
            MountingObj.OnMount(this);
        }

        public void Dismount()
        {
            OnTriggerDismount?.Invoke(this, MountingObj);

            Body.useGravity = true;
            Body.isKinematic = false;

            OnBeforeDismount?.Invoke(this, MountingObj);
            MountingObj.OnBeforeDismountEvent?.Invoke(this, MountingObj);

            OnDismount?.Invoke(this, MountingObj);
            MountingObj.OnDismount(this);

            MountingObj = null;
        }
    }
}