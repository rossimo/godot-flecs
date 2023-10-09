using Godot;
using Flecs.NET.Core;

public struct MouseEvent
{
    public InputEventMouseButton mouse;
    public Vector2 position;
}

public class Input
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            Move(world),
        };

    public static Routine Move(World world)
    {
        var players = world.Query(filter: world.FilterBuilder().Term<Player>());

        return world.Routine(
            filter: world.FilterBuilder().Term<MouseEvent>(),
            callback: (Entity entity, ref MouseEvent @event) =>
            {
                if (@event.mouse.IsPressed())
                {
                    switch (@event.mouse.ButtonIndex)
                    {
                        case MouseButton.Right:
                        case MouseButton.Left:
                            {
                                var position = @event.position;

                                players.Each(player =>
                                {
                                    player.Set(new MoveCommand
                                    {
                                        X = position.X,
                                        Y = position.Y
                                    });
                                });
                            }
                            break;
                    }
                }

                entity.Remove<MouseEvent>();
            });
    }
}