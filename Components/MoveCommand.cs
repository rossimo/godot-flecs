using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png"), Component]
public partial class MoveCommand : Node
{
    [Export]
    public float X { get; set; }
    [Export]
    public float Y { get; set; }
}

public class Move
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            MoveAndCollide(world),
        };

    public static Routine MoveAndCollide(World world) =>
        world.Routine(
            filter: world.FilterBuilder<MoveCommand, CharacterBody2D, Speed>(),
            callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D physics, ref Speed speed) =>
            {
                var scale = world.Get<Time>().Scale;

                var direction = physics.Position
                    .DirectionTo(new Vector2(move.X, move.Y))
                    .Normalized();

                var vector = direction * speed.Value * scale * Physics.SPEED_SCALE;

                var remaining = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
                var full = vector.DistanceTo(Vector2.Zero);

                if (remaining < full)
                {
                    vector = new Vector2(move.X, move.Y) - physics.Position;
                }

                var collision = physics.MoveAndCollide(vector);

                if ((physics.Position.X == move.X && physics.Position.Y == move.Y) ||
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
