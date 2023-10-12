using Godot;
using Flecs.NET.Core;

public partial class Game : Node2D
{
	private World world = World.Create();

	public override void _Ready()
	{
		Interop.Systems(world);

		world.Set(new Time());

		Physics.Observers(world);

		Input.Systems(world);
		Flash.Systems(world);
		Move.Systems(world);
		Physics.Systems(world);
		Delete.Systems(world);

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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse)
		{
			world.Set(new MouseEvent
			{
				mouse = mouse,
				position = ToLocal(GetViewport().GetMousePosition())
			});
		}
	}
}
