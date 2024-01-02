using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png")]
public partial class FlashScript : Script
{
	public async override Task Run(Entity entity)
	{
		while (true)
		{
			entity.Set(new FlashCommand { Color = Colors.Red });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");

			entity.Set(new FlashCommand { Color = Colors.Green });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");

			entity.Set(new FlashCommand { Color = Colors.Blue });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");
		}
	}
}
