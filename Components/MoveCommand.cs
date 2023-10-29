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
            MoveWithinRange(world),
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

    public static Routine MoveWithinRange(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand, CharacterBody2D, Speed>(),
            callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D physics) =>
            {
                if (move.Radius > 0)
                {
                    var distance = move.Position.DistanceTo(physics.Position);

                    if (distance <= move.Radius)
                    {
                        entity.Conclude(move);
                    }
                }
            });

    public static Routine MoveAndCollide(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand, CharacterBody2D, Speed>(),
            callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D physics, ref Speed speed) =>
            {
                var scale = world.Get<Time>().Scale;

                Vector2 direction;
                if (entity.Has<NavigationAgent2D>())
                {
                    var navigationAgent = entity.Get<NavigationAgent2D>();
                    navigationAgent.TargetPosition = move.Position;

                    direction = physics.Position
                        .DirectionTo(navigationAgent.GetNextPathPosition())
                        .Normalized();
                }
                else
                {
                    direction = physics.Position
                        .DirectionTo(move.Position)
                        .Normalized();
                }

                var vector = direction * speed.Value * scale * Physics.SPEED_SCALE;

                var remaining = physics.Position.DistanceTo(move.Position);
                var full = vector.DistanceTo(Vector2.Zero);

                if (remaining < full)
                {
                    vector = move.Position - physics.Position;
                }

                var collision = physics.MoveAndCollide(vector);

                if (physics.Position.IsEqualApprox(move.Position) ||
                    collision != null)
                {
                    entity.Conclude(move);
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
