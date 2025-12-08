using DigiEden.Framework.Utils;

namespace DigiEden.Framework
{
    public interface IEvent
    {
        public object[] Args { get; }
    }

    public sealed class GameEvent : IEvent
    {
        public object[] Args { get; private set; }

        public static GameEvent Create(object[] args)
        {
            return new GameEvent { Args = args };
        }
    }
}