using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class ColorizeCommand : Command
{
	[Export]
	public Color Color { get; set; }

	public bool Modulated = false;
}

public class Colorize
{
	public static IEnumerable<Routine> Systems(World world) =>
		new[] {
			System(world),
		};

	public static Routine System(World world) =>
		world.Routine<Entity2D, ColorizeCommand>()
			.Each((Entity entity, ref Entity2D node, ref ColorizeCommand command) =>
			{
				if (!command.Modulated)
				{
					node.CreateTween().TweenProperty(node, "modulate", command.Color, 0.5f);
					command.Modulated = true;
				}
			});
}
