using Godot;
using Flecs.NET.Core;

public partial class Game : WorldNode
{
	public override void _EnterTree()
	{
		base._EnterTree();

		World.Set(new Time());

		Physics.Observers(World);
		Interact.Observers(World);
		Timer.Observers(World);
		Animation.Observers(World);
		StateMachines.Observers(World);

		StateMachines.Systems(World);
		Animation.Systems(World);
		Attack.Systems(World);
		Damage.Systems(World);
		Timer.Systems(World);
		Input.Systems(World);
		Interact.Systems(World);
		Flash.Systems(World);
		Colorize.Systems(World);
		Move.Systems(World);
		Physics.Systems(World);
		Delete.Systems(World);
	}

	static StringName UI_LEFT = new StringName("ui_left");
	static StringName UI_RIGHT = new StringName("ui_right");
	static StringName UI_UP = new StringName("ui_up");
	static StringName UI_DOWN = new StringName("ui_down");
	static StringName ATTACK = new StringName("attack");

	public override void _PhysicsProcess(double delta)
	{
		ref var time = ref World.GetMut<Time>();
		time.Delta = delta;
		time.Scale = (float)(delta / Physics.TARGET_FRAMETIME);
		time.Ticks++;

		var direction = Godot.Input.GetVector(UI_LEFT, UI_RIGHT, UI_UP, UI_DOWN).Normalized();
		if (!direction.IsZeroApprox())
		{
			World.Set(new ControllerEvent { Direction = direction });
		}

		if (Godot.Input.IsActionJustPressed(ATTACK))
		{
			World.Add<InputAttackCommand>();
		}

		base._PhysicsProcess(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse && mouse.IsPressed())
		{
			World.Set(new MouseEvent
			{
				Button = mouse,
				Position = ToLocal(GetViewport().GetMousePosition())
			});
		}
	}
}
