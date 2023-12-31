using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png")]
public partial class FlashScript : Script
{
	public async override Task Run(Entity entity)
	{
		while (true)
		{
			await SetAsync<FlashScript, FlashCommand>(entity, new FlashCommand { Color = Colors.Red });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");

			await SetAsync<FlashScript, FlashCommand>(entity, new FlashCommand { Color = Colors.Green });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");

			await SetAsync<FlashScript, FlashCommand>(entity, new FlashCommand { Color = Colors.Blue });
			await GetTree().ToSignal(GetTree().CreateTimer(1), "timeout");
		}
	}
}
