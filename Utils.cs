using Godot;
using Flecs.NET.Core;
using System.Reflection;

public static class Utils
{
    public static Entity DiscoverEntity(this Node node, World world, Entity parent = default)
    {
        var entity = Entity.Null();

        foreach (var child in node.GetChildren())
        {
            var component = child.GetType().GetCustomAttribute<Component>(true);
            if (component != null)
            {
                if (!entity.IsValid())
                {
                    entity = world.Entity();

                    if (parent.IsValid())
                    {
                        entity.ChildOf(parent);
                    }
                }

                entity.DiscoverAndSet(child);
            }
            else
            {
                child.DiscoverEntity(world, entity);
            }
        }

        return entity;
    }

    private static Dictionary<Type, MethodInfo> setComponentCache = new Dictionary<Type, MethodInfo>();

    private static MethodInfo entitySetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Set[T](T)");

    public static void DiscoverAndSet(this Entity entity, object component)
    {
        var type = component.GetType();
        MethodInfo set = null;

        if (setComponentCache.ContainsKey(type))
        {
            set = setComponentCache[type];
        }

        if (set == null)
        {
            set = entitySetComponentdMethod.MakeGenericMethod(new Type[] { type });
            setComponentCache.Add(type, set);
        }

        set.Invoke(entity, new[] { component });
    }

    public delegate void ForEach<T1>(ref T1 c1);

    public static Routine Routine<T1>(this World world, string name = "", ForEach<T1> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);

                foreach (int i in it)
                    callback(ref c1[i]);
            });
    }

    public delegate void ForEachEntity<T1>(Entity entity, ref T1 c1);

    public static Routine Routine<T1>(this World world, string name = "", ForEachEntity<T1> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);

                foreach (int i in it)
                    callback(it.Entity(i), ref c1[i]);
            });
    }
}