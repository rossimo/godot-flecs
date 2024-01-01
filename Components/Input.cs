using Godot;
using Flecs.NET.Core;

public struct MouseEvent
{
    public InputEventMouseButton Button;
    public Vector2 Position;
}

public struct InputAttackCommand
{

}

public struct ControllerEvent
{
    public Vector2 Direction;
}

public struct LastIntent
{
    public Vector2 Direction;
}

public struct LastAttack
{
    public ulong Ticks;
}

public class Input
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            MouseInput(world),
            ControllerInput(world),
            ControllerAttack(world)
        };

    public static Routine MouseInput(World world)
    {
        var players = world.Query<Player>();
        var worldNode = world.Get<WorldNode>();

        return world.Routine().Term<MouseEvent>()
            .Each((Entity entity, ref MouseEvent @event) =>
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
                                    player.Set(new Move
                                    {
                                        Position = position
                                    });
                                });
                            }
                            break;

                        case MouseButton.Left:
                            {
                                var objects = worldNode.GetWorld2D()
                                    .DirectSpaceState
                                    .IntersectPoint(new PhysicsPointQueryParameters2D()
                                    {
                                        Position = position
                                    }, 1);

                                players.Each(player =>
                                {
                                    var move = new Move
                                    {
                                        Position = position,
                                    };

                                    if (objects.Count > 0)
                                    {
                                        var body = objects[0]["collider"].As<CharacterBody2D>();
                                        var target = body.GetEntity(world);
                                        if (target.IsAlive() && target != player)
                                        {
                                            move.Radius = 100;
                                            move.Target = target;

                                            move.AddChild(new AttackCommand()
                                            {
                                                TargetID = target.Id.Value
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
        var players = world.Query<Player>();

        return world.Routine()
            .Term<ControllerEvent>()
            .Each((Entity entity, ref ControllerEvent @event) =>
            {
                var direction = @event.Direction;

                players.Each(player =>
                {
                    var body = player.Get<CharacterBody2D>();

                    player.Set(new Move
                    {
                        Position = body.Position + (direction * 10)
                    });
                });

                entity.Remove<ControllerEvent>();
            });
    }

    public static Routine ControllerAttack(World world)
    {
        var bodies = world.Query<CharacterBody2D>();
        var players = world.Query<Player>();
        var game = world.Get<WorldNode>();

        return world.Routine()
            .Term<InputAttackCommand>()
            .Each((Entity entity, ref InputAttackCommand @event) =>
            {
                players.Each(player =>
                {
                    var playerBody = player.Get<CharacterBody2D>();

                    var targets = bodies.All<CharacterBody2D>()
                        .Where(body => body.GlobalPosition.DistanceTo(playerBody.GlobalPosition) <= 100)
                        .Select(body => body.GetEntity(world))
                        .Where(target => target.IsAlive() && target != player)
                        .OrderBy(body => body.Has<Health>() ? 0 : 1);

                    foreach (var target in targets)
                    {
                        var targetBody = target.Get<CharacterBody2D>();

                        player.Set(new LastIntent()
                        {
                            Direction = playerBody.Position.DirectionTo(targetBody.Position).Normalized()
                        });

                        player.Set(new AttackCommand()
                        {
                            TargetID = target.Id.Value
                        });
                        return;
                    }

                    var intent = player.Has<LastIntent>()
                        ? player.Get<LastIntent>().Direction
                        : Vector2.Down;

                    player.Set(new AttackCommand()
                    {
                        Angle = Vector2.Zero.AngleToPoint(intent)
                    });
                });

                entity.Remove<InputAttackCommand>();
            });
    }
}
