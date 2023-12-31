using Flecs.NET.Core;
using Godot;

[GlobalClass, Icon("res://resources/tools/entity.png")]
public partial class Entity2D : Node2D
{
	public override void _Ready()
	{
		base._Ready();

		var candidate = this.FindWorld();
		if (candidate is World world)
		{
			this.CreateEntity(world);
		}
		else
		{
			GD.PrintErr($"{GetPath()} is unable to find world");
		}
	}
}

public static class Entity2DUtils
{
	public static WorldNode FindWorldNode(this Node node)
	{
		if (node is WorldNode world)
		{
			return world;
		}

		var parent = node.GetParent();
		if (parent != null)
		{
			return parent.FindWorldNode();
		}

		throw new Exception($"Unable to find world node for {node.GetPath()}");
	}

	public static World FindWorld(this Node node)
	{
		var candidate = node.FindWorldNode()?.World;

		if (candidate is World world)
		{
			return world;
		}

		throw new Exception($"Unable to find world for {node.GetPath()}");
	}
}

