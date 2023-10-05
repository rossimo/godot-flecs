using Godot;
using Flecs.NET.Core;


[GlobalClass, Component]
public partial class MoveCommand : Node
{
    [Export]
    public float X { get; set; }
    [Export]
    public float Y { get; set; }
}

public class Move
{
    public static Routine System(World world) =>
        world.Routine(callback: (Entity entity, ref MoveCommand move, ref CharacterBody2D physics, ref Speed speed) =>
            {
                var timeScale = world.Get<Time>().Scale;

                var direction = physics.Position
                    .DirectionTo(new Vector2(move.X, move.Y))
                    .Normalized();

                var travel = direction * speed.Value * timeScale * Physics.SPEED_SCALE;

                var remainingDistance = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
                var travelDistance = physics.Position.DistanceTo(physics.Position + travel);

                if (remainingDistance < travelDistance)
                {
                    travel = new Vector2(move.X, move.Y) - physics.Position;
                }

                if (physics.MoveAndCollide(travel) != null)
                {
                    entity.Remove<MoveCommand>();
                }

                if (physics.Position.X == move.X && physics.Position.Y == move.Y)
                {
                    entity.Remove<MoveCommand>();
                }
            });
}