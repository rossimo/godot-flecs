using Godot;
using System;
using Flecs.NET.Core;
using System.Reflection;

public partial class Scene : Node2D
{
	private World world = World.Create(System.Environment.GetCommandLineArgs());

	public override void _Ready()
	{
		foreach (var child in GetChildren())
		{
			child.DiscoverEntity(world);
		}

		world.Routine("MyComponentRoutine", (Entity entity, ref MyComponent node) =>
		{
			GD.Print($"MyComponentRoutine: {node.Value}");
		});
	}

	public override void _PhysicsProcess(double delta)
	{
		world.Progress();
	}
}
