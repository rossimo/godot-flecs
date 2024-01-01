using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class AttackCommand : Command
{
    [Export]
    public float Angle;

    [Export]
    public ulong TargetID;
}

public class Attack
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world)
        };

    public static Routine System(World world)
    {
        var swordScene = ResourceLoader.Load<PackedScene>("res://sword.tscn");

        return world.Routine()
            .Term<AttackCommand>().Term<Entity2D>()
            .Each((Entity entity, ref AttackCommand attack, ref Entity2D body) =>
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

                var target = world.Entity(attack.TargetID);
                if (target.IsAlive() && target.Has<CharacterBody2D>())
                {
                    var targetBody = target.Get<CharacterBody2D>();
                    angle = body.GlobalPosition.AngleToPoint(targetBody.GlobalPosition);
                }

                var arc = (float)(Math.PI / 2);

                var arm = new Entity2D();
                var timer = new Godot.Timer() { WaitTime = duration };
                timer.AddChild(new DeleteCommand());
                arm.AddChild(timer);
                arm.Rotation = angle + (float)(Math.PI / 2) - arc / 2;

                var sword = Bootstrap.PrepareNode(swordScene.Instantiate<Entity2D>());
                sword.Position = new Vector2(0, -65);

                arm.AddChild(sword);
                body.AddChild(arm);

                arm.CreateTween().TweenProperty(arm, "rotation", arm.Rotation + arc, duration);

                entity.Remove<AttackCommand>();
            });
    }
}