using Godot;
using Flecs.NET.Core;

public partial class Scene : Node2D
{
	private World world = World.Create();

	public override void _Ready()
	{
		world.Set(new Time());
		world.PrepareGodotComponents();
		Physics.TopLevel(world);
		Physics.Sync(world);
		Flash.System(world);
		Move.System(world);

		foreach (var node in GetChildren())
		{
			node.DiscoverFlatEntity(world);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		ref var time = ref world.GetMut<Time>();
		time.Delta = delta;
		time.Scale = (float)(delta / Physics.TARGET_FRAMETIME);
		time.Ticks++;

		world.Progress();
	}
}
