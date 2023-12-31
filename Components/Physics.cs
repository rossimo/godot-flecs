using Godot;
using Flecs.NET.Core;

public struct Time
{
	public Time() { }
	public double Delta = 1 / Physics.FPS;
	public float Scale = 1;
	public ulong Ticks = 0;
}

public class Physics
{
	public static float FPS = $"{ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")}".ToFloat();
	public static float SPEED_SCALE = 60f / FPS;
	public static float TARGET_FRAMETIME = 1 / FPS;

	public static ulong MillisToTicks(ulong millis)
	{
		return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * FPS);
	}

	public static IEnumerable<Observer> Observers(World world) =>
		new[] {
			TopLevel(world),
			Area(world)
		};

	public static IEnumerable<Routine> Systems(World world) =>
		new[] {
			Sync(world),
		};

	public static Observer TopLevel(World world) =>
		world.Observer<CharacterBody2D>()
			.Event(Ecs.OnSet)
			.Each((ref CharacterBody2D node) =>
			{
				var globalPosition = node.GlobalPosition;
				node.TopLevel = true;
				node.GlobalPosition = globalPosition;
			});

	public static Observer Area(World world) =>
		world.Observer<Area2D>()
			.Event(Ecs.OnSet)
			.Each((Entity entity, ref Area2D node) =>
			{
				node.AreaEntered += (Area2D otherNode) =>
				{
					var other = otherNode.GetEntity(world);

					entity.Trigger<AreaTrigger>(other);

					if (other.IsValid())
					{
						other.Trigger<AreaTrigger>(entity);
					}
				};
			});

	static NodePath POSITION = new NodePath("position");

	public static Routine Sync(World world) =>
		world.Routine()
			.Term<CharacterBody2D>().Term<Entity2D>()
			.Each((Entity entity, ref CharacterBody2D physics, ref Entity2D node) =>
		{
			if (!physics.Position.IsEqualApprox(node.Position))
			{
				var tween = node.CreateTween();
				tween.TweenProperty(node, POSITION, physics.Position, TARGET_FRAMETIME);
			}
		});
}
