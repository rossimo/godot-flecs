using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png")]
public partial class WanderScript : Script
{
	[Export]
	public int Radius { get; set; } = 200;

	public async override Task Run(Entity entity)
	{
		var origin = entity.Get<CharacterBody2D>().GlobalPosition;
		var random = new Random();
		var originX = origin.X;
		var originY = origin.Y;

		while (true)
		{
			var theta = random.Within(2d * Math.PI);
			var radius = random.Within(Radius / 2, Radius);

			try
			{
				await entity.Task(new Move()
				{
					Position = new Vector2()
					{
						X = Convert.ToSingle(originX + radius * Math.Cos(theta)),
						Y = Convert.ToSingle(originY + radius * Math.Sin(theta)),
					}
				});
			}
			catch (CollisionException collision)
			{
				if (collision.Other.IsAlive())
				{
					var other = collision.Other;

					if (other.Has<Player>())
					{
						await Task.WhenAll(
							entity.SetAsync(new Talk() { Text = "Outta my way!" }),
							other.SetAsync(new Talk() { Text = "Ah!" }));
					}
					else if (entity.Has<Enemy>() && other.Has<Enemy>())
					{
						var enemies = new[] { entity, other }.OrderBy(a => random.Next()).ToList();

						await Task.WhenAll(
							enemies[0].SetAsync(new Talk() { Text = "Pardon me" }),
							enemies[1].SetAsync(new Talk() { Text = "Excuse me" }));
					}
				}
			}
		}
	}
}

public static class WanderScriptUtils
{
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
