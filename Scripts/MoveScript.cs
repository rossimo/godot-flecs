using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class MoveScript : Node, Script
{
    public async Task Run(Entity entity)
    {
        while (true)
        {
            entity.Set(new MoveCommand { X = 200, Y = 200 });
            await entity.OnRemoveAsync<MoveCommand>();

            entity.Set(new MoveCommand { X = 300, Y = 200 });
            await entity.OnRemoveAsync<MoveCommand>();

            entity.Set(new MoveCommand { X = 300, Y = 300 });
            await entity.OnRemoveAsync<MoveCommand>();

            entity.Set(new MoveCommand { X = 200, Y = 300 });
            await entity.OnRemoveAsync<MoveCommand>();
        }
    }
}