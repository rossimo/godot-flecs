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
				SetAsync(entity, new MoveCommand { X = 200, Y = 200 });
				await OnRemoveAsync<MoveScript, MoveCommand>(entity);

				SetAsync(entity, new MoveCommand { X = 400, Y = 200 });
				await OnRemoveAsync<MoveScript, MoveCommand>(entity);

				SetAsync(entity, new MoveCommand { X = 400, Y = 400 });
				await OnRemoveAsync<MoveScript, MoveCommand>(entity);

				SetAsync(entity, new MoveCommand { X = 200, Y = 400 });
				await OnRemoveAsync<MoveScript, MoveCommand>(entity);
			}
		}
		catch (ScriptRemovedException)
		{
			RemoveAsync<MoveCommand>(entity);
		}
	}
}
