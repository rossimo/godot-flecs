using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class FlashScript : Node, Script
{
    public async Task Run(Entity entity)
	{
		while (true)
		{
			entity.SetAsync(new FlashCommand { Color = Colors.Red });
			await Task.Delay(1000);

			entity.SetAsync(new FlashCommand { Color = Colors.Green });
			await Task.Delay(1000);

			entity.SetAsync(new FlashCommand { Color = Colors.Blue });
			await Task.Delay(1000);
		}
	}
	
	public void OnRemove(Entity entity)
    {
    }
}
