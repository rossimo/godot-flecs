using Godot;
using Flecs.NET.Core;

public class Animation
{
    public static IEnumerable<Observer> Observers(World world) =>
        new Observer[] {
            OnAnimationPlayer(world),
            OnAnimatedSprite2D(world)
        };

    public static IEnumerable<Routine> Systems(World world) =>
        new Routine[] {
            AnimationCommand(world),
            AnimationCommandCleanup(world)
        };

    static StringName IDLE = new StringName("idle");
    static StringName NAME = new StringName("name");

    public static Observer OnAnimationPlayer(World world) =>
        world.Observer()
            .Term<AnimationPlayer>()
            .Event(Ecs.OnSet).Each((ref AnimationPlayer player) =>
            {
                var animations = player.GetAnimationList().Where(anim => anim != "RESET");
                if (animations.Count() == 1)
                {
                    player.Play(animations.First());
                }
                else if (animations.Contains(IDLE.ToString()))
                {
                    player.Play(IDLE);
                }
            });

    public static Observer OnAnimatedSprite2D(World world) =>
        world.Observer()
            .Term<AnimatedSprite2D>()
            .Event(Ecs.OnSet)
            .Each((ref AnimatedSprite2D player) =>
        {
            var animations = player.SpriteFrames.Animations.Select(anim =>
                anim.AsGodotDictionary()[NAME].AsString());

            if (animations.Count() == 1)
            {
                player.Play(animations.First());
            }
            else if (animations.Contains(IDLE.ToString()))
            {
                player.Play(IDLE);
            }
        });

    /*
        public static Routine Movement(World world) =>
            world.Routine(
                filter: world.FilterBuilder().Term<CharacterBody2D>().Term<StateMachine>().Term<Frozen>().Not(),
                callback: (Entity entity, ref CharacterBody2D body, ref StateMachine machine) =>
                {
                    if (body.Velocity.IsZeroApprox())
                    {
                        if (machine.State != "Idle" && machine.State != "Attack" && machine.State != "Hurt" && machine.States.ContainsKey("Idle"))
                        {
                            entity.Set(new ChangeStateCommand() { State = "Idle" });
                        }
                    }
                });
    */

    public static Routine AnimationCommand(World world) =>
        world.Routine()
            .Term<AnimatedSprite2D>().Term<AnimationCommand>()
            .Each((Entity entity, ref AnimatedSprite2D sprite, ref AnimationCommand command) =>
        {
            sprite.Play(command.Animation);
        });

    public static Routine AnimationCommandCleanup(World world) =>
        world.Routine()
            .Term<AnimationCommand>()
            .Each((Entity entity) =>
        {
            entity.Remove<AnimationCommand>();
        });
}
