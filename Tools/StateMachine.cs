using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/state_machine.png")]
public partial class StateMachine : Node2D, IBootstrap
{
	[Export]
	public string State { get; set; } = "";

	[Signal]
	public delegate void StateExitedEventHandler(string name);

	[Signal]
	public delegate void StateEnteredEventHandler(string name);

	public Dictionary<string, Node> States = new Dictionary<string, Node>();

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		foreach (var item in States.Values)
		{
			item.Free();
		}
	}

	public bool Bootstrapped()
	{
		return GetMeta("bootstrap", false).AsBool();
	}

	public void Bootstrap()
	{
		foreach (var state in GetChildren().OfType<State>())
		{
			States.Add(state.Name, state.Duplicate());

			state.QueueFree();
			RemoveChild(state);
		}

		SetMeta("bootstrap", true);
	}

	public override void _Ready()
	{
		base._Ready();

		if (!Bootstrapped())
		{
			GD.PrintErr($"{GetPath()} is being bootstrapped late!");
			Bootstrap();
		}

		if (State.Length > 0)
		{
			ChangeState(State);
		}
	}

	public void ChangeState(string stateName)
	{
		ExitState();

		if (States.ContainsKey(stateName))
		{
			EnterState(stateName);
		}
	}

	private void ExitState()
	{
		foreach (var state in GetChildren().OfType<State>())
		{
			foreach (var component in state.GetChildren())
			{
				component.QueueFree();
			}

			state.QueueFree();
			RemoveChild(state);
		}

		var previous = State;
		State = "";

		EmitSignal(SignalName.StateExited, previous);
	}

	private void EnterState(string stateName)
	{
		State = stateName;

		var state = States[stateName].Duplicate();
		AddChild(state);

		EmitSignal(SignalName.StateEntered, State);
	}
}

public class StateMachines
{
	public static IEnumerable<Observer> Observers(World world) =>
		new Observer[] {
			OnStateMachine(world)
		};

	public static IEnumerable<Routine> Systems(World world) =>
		new[] {
			ChangeStateCommand(world),
			ChangeStateCommandCleanup(world)
		};

	public static Observer OnStateMachine(World world) =>
		world.Observer()
			.Term<StateMachine>()
			.Event(Ecs.OnSet)
			.Each((Entity entity, ref StateMachine machine) =>
			{
				var machineRef = machine;

				var components = (string state) =>
				{
					foreach (var component in machineRef.GetNode(state).GetChildren())
					{
						entity.SetNode(component);
					}
				};

				components(machine.State);

				machine.StateEntered += (string state) => components(state);
			});

	public static Routine ChangeStateCommand(World world) =>
		world.Routine<StateMachine, ChangeStateCommand>()
			.NoReadonly()
			.Each((Entity entity, ref StateMachine machine, ref ChangeStateCommand command) =>
			{
				world.DeferSuspend();
				try
				{
					entity.Remove<ChangeStateCommand>();
					machine.ChangeState(command.State);
				}
				finally
				{
					world.DeferResume();
				}
			});

	public static Routine ChangeStateCommandCleanup(World world) =>
		world.Routine<ChangeStateCommand>()
		.Each((Entity entity) =>
			{
				entity.Remove<ChangeStateCommand>();
			});
}

