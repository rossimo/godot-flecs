using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class MoveScript : Script
{
	public async override Task Run(Entity entity)
	{
		try
		{
			while (true)
			{
				await entity.SetAsync(this, new MoveCommand { Position = new Vector2 { X = 200, Y = 200 } });
				await OnRemoveAsync<MoveCommand>(entity);

				await entity.SetAsync(this, new MoveCommand { Position = new Vector2 { X = 400, Y = 200 } });
				await OnRemoveAsync<MoveCommand>(entity);

				await entity.SetAsync(this, new MoveCommand { Position = new Vector2 { X = 400, Y = 400 } });
				await OnRemoveAsync<MoveCommand>(entity);

				await entity.SetAsync(this, new MoveCommand { Position = new Vector2 { X = 200, Y = 400 } });
				await OnRemoveAsync<MoveCommand>(entity);
			}
		}
		catch (ScriptRemovedException)
		{
			await entity.RemoveAsync<MoveCommand>(this);
		}
	}

	async Task<T> OnRemoveAsync<T>(Entity entity) =>
		await OnRemoveAsync<MoveScript, T>(entity);
}
