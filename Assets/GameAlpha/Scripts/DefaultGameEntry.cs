using Cysharp.Threading.Tasks;
using XuchFramework.Core;
using XuchFramework.Extensions.ECS;

namespace GamePlay
{
    public class DefaultGameEntry : GameEntryBase
    {
        public override async UniTask EnterGame()
        {
            await GameRunner.Instance.LaunchModules("[game_modules]");

            LaunchECS();
        }

        private void LaunchECS()
        {
            WorldContext world = new WorldContext();

            int player = world.CreateEntity();
            int enemy = world.CreateEntity();

            world.AddComponent(player, new PositionComponent() { X = 0f, Y = 0f, Z = 0f });
            world.AddComponent(player, new VelocityComponent() { X = 1f, Y = 1f, Z = 1f });

            world.AddComponent(enemy, new PositionComponent() { X = 10f, Y = 0f, Z = 10f });

            var playerPos = world.GetComponent<PositionComponent>(player);
            var playerVelocity = world.GetComponent<VelocityComponent>(player);

            var enemyPos = world.GetComponent<PositionComponent>(enemy);

            Log.Debug(
                $"Player Position: ({playerPos.X}, {playerPos.Y}, {playerPos.Z}), Velocity: ({playerVelocity.X}, {playerVelocity.Y}, {playerVelocity.Z})");
            Log.Debug($"Enemy Position: ({enemyPos.X}, {enemyPos.Y}, {enemyPos.Z})");

            world.RemoveComponent<VelocityComponent>(player);
            Log.Debug($"Has Velocity Component: {world.HasComponent<VelocityComponent>(player)}");
        }
    }
}