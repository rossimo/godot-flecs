using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png")]
public partial class WanderScript : Script
{
	public async override Task Run(Entity entity)
	{
		var origin = entity.Get<CharacterBody2D>().GlobalPosition;
		var random = new Random();
		var originX = origin.X;
		var originY = origin.Y;

		try
		{
			while (true)
			{
				var angle = random.NextSingle() * Mathf.Pi * 2;
				var theta = random.Within(2d * Math.PI);
				var radius = random.Within(100, 200);

				await entity.OnChangeAsync(this, new MoveCommand()
				{
					Position = new Vector2()
					{
						X = Convert.ToSingle(originX + radius * Math.Cos(theta)),
						Y = Convert.ToSingle(originY + radius * Math.Sin(theta)),
					}
				});
			}
		}
		catch (ScriptRemovedException)
		{
			await entity.RemoveAsync<MoveCommand>(this);
		}
	}
}

public static class WanderScriptUtils
{
	public static async Task OnChangeAsync<T>(this Entity entity, WanderScript script, T component) =>
		await entity.OnChangeAsync<WanderScript, T>(script, component);

	public static double Within(this Random random, double bound)
	{
		return random.Within(0, bound);
	}

	public static double Within(this Random random, double firstBound, double secondBound)
	{
		var delta = firstBound - secondBound;
		var value = random.NextDouble();

		return firstBound - delta * value;
	}

	public static int Within(this Random random, int bound)
	{
		return random.Within(0, bound);
	}

	public static int Within(this Random random, int firstBound, int secondBound)
	{
		var delta = firstBound - secondBound;
		var value = random.Next(Math.Abs(delta));

		return firstBound + (secondBound > firstBound ? 1 : -1) * value;
	}
}
