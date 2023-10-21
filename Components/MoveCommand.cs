using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png"), Component]
public partial class MoveCommand : Node
{
    [Export]
    public Vector2 Position;

    [Export]
    public float Range = 0;
}

public class Move
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            MoveWithinRadius(world),
            MoveAndCollide(world),
        };

    public static Routine MoveWithinRadius(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand, CharacterBody2D, Speed>(),
            callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D physics) =>
            {
                if (move.Range > 0)
                {
                    var distance = move.Position.DistanceTo(physics.Position);

                    if (distance <= move.Range)
                    {
                        entity.Remove<MoveCommand>();
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
