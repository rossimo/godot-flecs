using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class SpiderMoveScript : Script
{
	public async override Task Run(Entity entity)
	{
		try
		{
			while (true)
			{
				await entity.OnChangeAsync(this, new MoveCommand
				{
					Position = new Vector2 { X = 200, Y = 200 }
				});

				await entity.OnChangeAsync(this, new MoveCommand
				{
					Position = new Vector2 { X = 400, Y = 200 }
				});

				await entity.OnChangeAsync(this, new MoveCommand
				{
					Position = new Vector2 { X = 400, Y = 400 }
				});

				await entity.OnChangeAsync(this, new MoveCommand
				{
					Position = new Vector2 { X = 200, Y = 400 }
				});
			}
		}
		catch (ScriptRemovedException)
		{
			await entity.RemoveAsync<MoveCommand>(this);
		}
	}
}

public static class SpiderMoveScriptUtils
{
	public static async Task OnChangeAsync<T>(this Entity entity, SpiderMoveScript script, T component) =>
		await entity.OnChangeAsync<SpiderMoveScript, T>(script, component);
}
