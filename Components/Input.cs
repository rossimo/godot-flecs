using Godot;
using Flecs.NET.Core;

public struct MouseEvent
{
    public InputEventMouseButton Button;
    public Vector2 Position;
}

public struct ControllerEvent
{
    public Vector2 Direction;
}

public class Input
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            MouseInput(world),
            ControllerInput(world),
        };

    public static Routine MouseInput(World world)
    {
        var players = world.Query(filter: world.FilterBuilder().Term<Player>());
        var game = world.Get<Game>();

        return world.Routine(
            filter: world.FilterBuilder().Term<MouseEvent>(),
            callback: (Entity entity, ref MouseEvent @event) =>
            {
                var position = @event.Position;

                if (@event.Button.IsPressed())
                {
                    switch (@event.Button.ButtonIndex)
                    {
                        case MouseButton.Right:
                            {
                                players.Each(player =>
                                {
                                    player.Set(new MoveCommand
                                    {
                                        Position = position
                                    });
                                });
                            }
                            break;

                        case MouseButton.Left:
                            {
                                var objects = game.GetWorld2D()
                                    .DirectSpaceState
                                    .IntersectPoint(new PhysicsPointQueryParameters2D()
                                    {
                                        Position = position
                                    }, 1);

                                players.Each(player =>
                                {
                                    var move = new MoveCommand
                                    {
                                        Position = position,
                                    };

                                    if (objects.Count > 0)
                                    {
                                        var body = objects[0]["collider"].As<CharacterBody2D>();
                                        var target = body.FindEntity(world);
                                        if (target.IsAlive())
                                        {
                                            move.Radius = 100;
                                            move.Target = target;

                                            move.AddChild(new AttackCommand()
                                            {
                                                Target = target
                                            });
                                        }
                                    }

                                    player.Set(move);
                                });
                            }
                            break;
                    }
                }

                entity.Remove<MouseEvent>();
            });
    }

    public static Routine ControllerInput(World world)
    {
        var players = world.Query(filter: world.FilterBuilder().Term<Player>());
        var game = world.Get<Game>();

        return world.Routine(
            filter: world.FilterBuilder().Term<ControllerEvent>(),
            callback: (Entity entity, ref ControllerEvent @event) =>
            {
                var direction = @event.Direction;

                players.Each(player =>
                {
                    var body = player.Get<CharacterBody2D>();
                    player.Set(new MoveCommand
                    {
                        Position = body.Position + (direction * 10)
                    });
                });

                entity.Remove<ControllerEvent>();
            });
    }
}