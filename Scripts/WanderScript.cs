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
				await entity.Set<WanderScript, Move>(this, new Move()
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

					if (entity.Has<Enemy>() && other.Has<Player>())
					{
						entity.Set(new Talk() { Text = "Outta my way!" });
						other.Set(new Talk() { Text = "Ah!" });
					}
					else if (entity.Has<Enemy>() && other.Has<Enemy>())
					{
						var enemies = new[] { entity, other }.Shuffle().ToArray();

						enemies[0].Set(new Talk() { Text = "Pardon me" });
						enemies[1].Set(new Talk() { Text = "Excuse me" });
					}
				}
			}
		}
	}
}
