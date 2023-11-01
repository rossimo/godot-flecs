using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class MoveCommand : Node
{
    [Export]
    public Vector2 Position;

    [Export]
    public float Radius = 0;

    public Entity Target;
}

public class Move
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            PositionToTarget(world),
            MoveAndCollide(world),
        };

    public static Routine PositionToTarget(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand>(),
            callback: (ref MoveCommand move) =>
            {
                if (move.Target.IsAlive() && move.Target.Has<CharacterBody2D>())
                {
                    move.Position = move.Target.Get<CharacterBody2D>().GlobalPosition;
                }
            });

    public static Routine MoveAndCollide(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand, CharacterBody2D, Speed>(),
            callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D body, ref Speed speed) =>
            {
                if (move.Radius > 0)
                {
                    var distance = move.Position.DistanceTo(body.Position);

                    if (distance <= move.Radius)
                    {
                        entity.Complete(move);
                        return;
                    }
                }

                var scale = world.Get<Time>().Scale;

                Vector2 direction;
                if (entity.Has<NavigationAgent2D>())
                {
                    var navigationAgent = entity.Get<NavigationAgent2D>();
                    navigationAgent.TargetPosition = move.Position;

                    direction = body.Position
                        .DirectionTo(navigationAgent.GetNextPathPosition())
                        .Normalized();
                }
                else
                {
                    direction = body.Position
                        .DirectionTo(move.Position)
                        .Normalized();
                }

                var vector = direction * speed.Value * scale * Physics.SPEED_SCALE;

                var remaining = body.Position.DistanceTo(move.Position);
                var full = vector.DistanceTo(Vector2.Zero);

                if (remaining < full)
                {
                    vector = move.Position - body.Position;
                }

                var collision = body.MoveAndCollide(vector);

                if (body.Position.IsEqualApprox(move.Position))
                {
                    entity.Complete(move);
                }
                else if (collision != null)
                {
                    entity.Remove<MoveCommand>();
                }

                if (collision != null)
                {
                    var other = collision.GetCollider().FindEntity(world);
                    entity.Trigger<CollisionTrigger>(other);

                    if (other.IsValid())
                    {
                        other.Trigger<CollisionTrigger>(entity);
                    }
                }
            });
}
