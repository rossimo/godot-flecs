using Godot;
using Flecs.NET.Core;

public partial class Scene : Node2D
{
	private World world = World.Create();

	public override void _Ready()
	{
		world.Set(new Time());
		Interop.NodeSystems(world);

		Physics.TopLevel(world);
		Flash.System(world);
		Move.System(world);
		Physics.Sync(world);

		foreach (var node in GetChildren())
		{
			node.CreateEntity(world);
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
