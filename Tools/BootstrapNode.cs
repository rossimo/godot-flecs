using Godot;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class BootstrapNode : Node
{
	[Export]
	public PackedScene? Scene { get; set; } = null;

	public override void _Ready()
	{
		QueueFree();

		if (Scene != null)
		{
			GetTree().Root.CallDeferred("add_child", Bootstrap.PrepareNode(Scene.Instantiate()));
		}
	}
}

public static class Bootstrap
{
	public static T PrepareNode<T>(T node) where T : Node
	{
		foreach (var child in node.GetChildren())
		{
			PrepareNode(child);
		}

		if (node is IBootstrap bootstrap)
		{
			bootstrap.Bootstrap();
		}

		return node;
	}
}
