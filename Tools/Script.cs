using Flecs.NET.Core;
using System.Reflection;

public static class Script
{
    public static void OnSet(World world)
    {
        world.Observer(
            filter: world.FilterBuilder().Term(Ecs.Wildcard),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Iter iter) =>
            {
                var id = iter.Id(1);
                var symbol = id.TypeId().Symbol();

                if (id.IsEntity())
                {
                    foreach (var i in iter)
                    {
                        var entity = iter.Entity(i);
                        var type = Assembly.GetExecutingAssembly().GetType(symbol);
                        Console.WriteLine($"+ {entity} {entity.Symbol()}");
                    }
                }
            }
        );
    }
}