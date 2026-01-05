namespace XuchFramework.Extensions.ECS
{
    public interface IComponent { }

    internal static class ComponentCounter
    {
        public static int Counter = 0;
    }

    /// <summary> Get a unique id for component T by ComponentType&lt;T&gt;.Id </summary>
    public static class ComponentType<T>
    {
        public static readonly int Id = ComponentCounter.Counter++;
    }
}