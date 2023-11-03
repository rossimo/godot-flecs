using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class AttackCommand : Node
{
    [Export]
    public float Angle;

    public Entity Target;
}

public class Attack
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world),
            Cleanup(world)
        };

    public static Routine System(World world)
    {
        var swordScene = ResourceLoader.Load<PackedScene>("res://sword.tscn");

        return world.Routine(
            filter: world.FilterBuilder().Term<AttackCommand>().Term<CharacterBody2D>().NotTrigger(),
            callback: (Entity entity, ref AttackCommand attack, ref CharacterBody2D body) =>
            {
                var lastAttackTick = entity.Has<LastAttack>() 
                    ? entity.Get<LastAttack>().Ticks
                    : 0;

                var duration = .2;

                var time = world.Get<Time>();

                if ((time.Ticks - lastAttackTick) < Physics.MillisToTicks((ulong)(duration * 1000)))
                {
                    return;
                }

                entity.Set(new LastAttack()
                {
                    Ticks = time.Ticks
                });

                var angle = attack.Angle;

                if (attack.Target.IsAlive() && attack.Target.Has<CharacterBody2D>())
                {
                    var targetBody = attack.Target.Get<CharacterBody2D>();
                    angle = body.GlobalPosition.AngleToPoint(targetBody.GlobalPosition);
                }

                var arc = (float)(Math.PI / 2);

                var arm = new Node2D();
                var timer = new TimerCommand() { Millis = (ulong)(duration * 1000) };
                timer.AddChild(new DeleteCommand());
                arm.AddChild(timer);
                arm.CreateEntity(world);

                arm.Rotation = angle + (float)(Math.PI / 2) - arc / 2;

                var sword = swordScene.Instantiate<Node2D>();
                sword.CreateEntity(world);
                sword.Position = new Vector2(0, -65);

                arm.AddChild(sword);
                body.AddChild(arm);

                arm.CreateTween().TweenProperty(arm, "rotation", arm.Rotation + arc, duration);
            });
    }

    public static Routine Cleanup(World world) =>
    world.Routine(
        filter: world.FilterBuilder().Term<AttackCommand>().NotTrigger(),
        callback: (Entity entity) =>
        {
            entity.Remove<AttackCommand>();
        });
}