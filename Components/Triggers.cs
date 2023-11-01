using Godot;
using Flecs.NET.Core;

public static class Triggers
{
	public static FilterBuilder NotTrigger(this FilterBuilder builder) =>
		builder
            .Term<AreaTrigger>().Not()
            .Term<CollisionTrigger>().Not()
            .Term<ClickTrigger>().Not()
            .Term<InteractTrigger>().Not()
            .Term<TimerTrigger>().Not();
}
