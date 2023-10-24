using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class FlashScript : Script
{
	public async override Task Run(Entity entity)
	{
		while (true)
		{
			await SetAsync(entity, new FlashCommand { Color = Colors.Red });
			await SetAsync(entity, new TimerCommand { Millis = 1000 });
			await OnRemoveAsync<TimerCommand>(entity);

			await SetAsync(entity, new FlashCommand { Color = Colors.Green });
			await SetAsync(entity, new TimerCommand { Millis = 1000 });
			await OnRemoveAsync<TimerCommand>(entity);

			await SetAsync(entity, new FlashCommand { Color = Colors.Blue });
			await SetAsync(entity, new TimerCommand { Millis = 1000 });
			await OnRemoveAsync<TimerCommand>(entity);
		}
	}

	async Task<T> OnRemoveAsync<T>(Entity entity) =>
		await OnRemoveAsync<FlashScript, T>(entity);
}
