using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class MoveScript : Node, Script
{
	public async Task Run(Entity entity)
	{
		while (true)
		{
			entity.SetAsync(new MoveCommand { X = 200, Y = 200 });
			await entity.OnRemoveAsync<MoveScript, MoveCommand>();

			entity.SetAsync(new MoveCommand { X = 400, Y = 200 });
			await entity.OnRemoveAsync<MoveScript, MoveCommand>();

			entity.SetAsync(new MoveCommand { X = 400, Y = 400 });
			await entity.OnRemoveAsync<MoveScript, MoveCommand>();

			entity.SetAsync(new MoveCommand { X = 200, Y = 400 });
			await entity.OnRemoveAsync<MoveScript, MoveCommand>();
		}
	}

	public void OnRemove(Entity entity)
	{
		entity.Remove<MoveCommand>();
	}
}
