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
        var game = world.Get<Game>();
        var swordScene = ResourceLoader.Load<PackedScene>("res://sword.tscn");

        return world.Routine(
            filter: world.FilterBuilder().Term<MouseEvent>(),
            callback: (Entity entity, ref MouseEvent @event) =>
            {
                var position = @event.position;

                if (@event.mouse.IsPressed())
                {
                    switch (@event.mouse.ButtonIndex)
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
                                players.Each(player =>
                                {
                                    var parent = player.Get<Node2D>();

                                    var angle = parent.GlobalPosition.AngleToPoint(position);

                                    var arc = (float)(Math.PI / 2);

                                    var arm = new Node2D();
                                    arm.CreateEntity(world);
                                    arm.Rotation = angle + (float)(Math.PI / 2) - arc / 2;

                                    var sword = swordScene.Instantiate<Node2D>();
                                    sword.CreateEntity(world);
                                    sword.Position = new Vector2(0, -65);

                                    arm.AddChild(sword);
                                    parent.AddChild(arm);

                                    arm.CreateTween().TweenProperty(arm, "rotation", arm.Rotation + arc, 0.25);
                                });
                            }
                            break;
                    }
                }

                entity.Remove<MouseEvent>();
            });
    }
}