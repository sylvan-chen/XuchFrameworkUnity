using DigiEden.Framework.Internal;

namespace DigiEden.Framework
{
    public class InputManager : ManagerBase
    {
        private GameInputActions _gameInputActions;

        public HandInputAdapter Hand { get; private set; }
        public PlayerInputAdapter Player { get; private set; }
        public UIInputAdapter UI { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _gameInputActions = new GameInputActions();
            Hand = new HandInputAdapter(_gameInputActions);
            Player = new PlayerInputAdapter(_gameInputActions);
            UI = new UIInputAdapter(_gameInputActions);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _gameInputActions.Dispose();
        }
    }
}