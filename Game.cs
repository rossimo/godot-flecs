using Godot;
using Flecs.NET.Core;

public partial class Game : Node2D
{
	private World world = World.Create();

	public override void _Ready()
	{
		Interop.Observers(world);

		world.Set(this);
		world.Set(new Time());

		Physics.Observers(world);
		Interact.Observers(world);
		Timer.Observers(world);

		Attack.Systems(world);
		Damage.Systems(world);
		Timer.Systems(world);
		Input.Systems(world);
		Interact.Systems(world);
		Flash.Systems(world);
		Move.Systems(world);
		Physics.Systems(world);
		Delete.Systems(world);

		world.Set(this);

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

		var direction = Godot.Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		if (!direction.IsZeroApprox())
		{
			world.Set(new ControllerEvent
			{
				Direction = direction
			});
		}

		world.Progress();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse && mouse.IsPressed())
		{
			world.Set(new MouseEvent
			{
				Button = mouse,
				Position = ToLocal(GetViewport().GetMousePosition())
			});
		}
	}
}
