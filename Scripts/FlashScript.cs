using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class FlashScript : Script
{
	public async override Task Run(Entity entity)
	{
		while (true)
		{
			await entity.ReflectionSetAsync(this, new FlashCommand { Color = Colors.Red });
			await entity.OnChangeAsync(this, new TimerCommand { Millis = 1000 });

			await entity.ReflectionSetAsync(this, new FlashCommand { Color = Colors.Green });
			await entity.OnChangeAsync(this, new TimerCommand { Millis = 1000 });

			await entity.ReflectionSetAsync(this, new FlashCommand { Color = Colors.Blue });
			await entity.OnChangeAsync(this, new TimerCommand { Millis = 1000 });
		}
	}
}

public static class FlashScriptUtils
{
	public static async Task OnChangeAsync<T>(this Entity entity, FlashScript script, T component) =>
		await entity.OnChangeAsync<FlashScript, T>(script, component);
}
