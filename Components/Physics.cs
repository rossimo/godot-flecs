using Godot;
using Flecs.NET.Core;

public struct Time
{
    public Time() { }
    public double Delta = 1 / Physics.FPS;
    public float Scale = 1;
    public int Ticks = 0;
}

public class Physics
{
    public static float FPS = $"{ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")}".ToFloat();
    public static float SPEED_SCALE = 30f / FPS;
    public static float TARGET_FRAMETIME = 1 / FPS;

    public static ulong MillisToTicks(double millis)
    {
        return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * FPS);
    }

    public static Observer TopLevel(World world) =>
        world.Observer(
            filter: world.FilterBuilder().Term<CharacterBody2D>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Iter it, int i) =>
            {
                var node = it.Field<CharacterBody2D>(1)[i];
                var globalPosition = node.GlobalPosition;
                node.TopLevel = true;
                node.Position = globalPosition;
            });

    public static Routine Sync(World world) =>
        world.Routine(callback: (Entity entity, ref CharacterBody2D physics, ref Sprite2D sprite) =>
        {
            if (!physics.Position.Equals(sprite.Position))
            {
                var target = physics.Position.DistanceTo(sprite.Position);
                var speed = entity.Has<Speed>() ? entity.Get<Speed>().Value : 1;
                var normal = speed * SPEED_SCALE;

                var ratio = Math.Min(target / normal, 1);

                var tween = sprite.CreateTween();
                tween.TweenProperty(sprite, "position", physics.Position, TARGET_FRAMETIME * ratio);
            }
        });
}