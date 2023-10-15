using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class FlashScript : Script
{
	public async override Task Run(Entity entity)
	{
		while (true)
		{
			SetAsync(entity, new FlashCommand { Color = Colors.Red });
			await Task.Delay(1000);

			SetAsync(entity, new FlashCommand { Color = Colors.Green });
			await Task.Delay(1000);

			SetAsync(entity, new FlashCommand { Color = Colors.Blue });
			await Task.Delay(1000);
		}
	}
}
