using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class Move : Command
{
    [Export]
    public new Vector2 Position = Vector2.Zero;

    [Export]
    public float Radius = 0;

    public Entity Target;
}

public class CollisionException : Exception
{
    public Entity Other;

    public CollisionException()
    {

    }

    public CollisionException(Entity other)
    {
        Other = other;
    }
}

public class Moves
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            PositionToTarget(world),
            MoveAndCollide(world),
        };

    public static Routine PositionToTarget(World world) =>
        world.Routine<Move>()
            .Each((ref Move move) =>
            {
                if (move.Target.IsAlive() && move.Target.Has<CharacterBody2D>())
                {
                    move.Position = move.Target.Get<CharacterBody2D>().GlobalPosition;
                }
            });

    public static Routine MoveAndCollide(World world) =>
        world.Routine<Move, CharacterBody2D, Speed>()
            .Each((Entity entity, ref Move move, ref CharacterBody2D body, ref Speed speed) =>
            {
                entity.Set(new LastIntent()
                {
                    Direction = body.Position.DirectionTo(move.Position).Normalized()
                });

                if (move.Radius > 0)
                {
                    var distance = move.Position.DistanceTo(body.Position);

                    if (distance <= move.Radius)
                    {
                        entity.Success(move);
                        return;
                    }
                }

                var scale = world.Get<Time>().Scale;

                Vector2 direction = body.Position
                        .DirectionTo(move.Position)
                        .Normalized();

                var vector = direction * speed.Value * scale * Physics.SPEED_SCALE;

                var remaining = body.Position.DistanceTo(move.Position);
                var full = vector.DistanceTo(Vector2.Zero);

                if (remaining < full)
                {
                    vector = move.Position - body.Position;
                }

                if (body.Position.IsEqualApprox(move.Position))
                {
                    entity.Success(move);
                }

                var collision = body.MoveAndCollide(vector);

                if (collision != null)
                {
                    var other = collision.GetCollider().GetEntity(world);

                    entity.Failure(move, new CollisionException(other));

                    entity.Trigger<CollisionTrigger>(other);

                    if (other.IsValid())
                    {
                        other.Trigger<CollisionTrigger>(entity);
                    }
                }
            })
            .Async(world);
}
