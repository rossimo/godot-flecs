using Godot;
using Flecs.NET.Core;
using System.Reflection;

struct EntityNode { }

public static class Utils
{
    public static Entity DiscoverFlatEntity(this Node node, World world, Entity entity = default)
    {
        if (!entity.IsValid())
        {
            entity = world.Entity(node.Name);
            entity.DiscoverAndSet(node);
            entity.SetSecond<EntityNode, Node>(node);
        }

        foreach (var child in node.GetChildren())
        {
            entity.DiscoverAndSet(child);
            child.DiscoverFlatEntity(world, entity);
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

        GD.Print($"Setting {type} to {entity}");
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

    public delegate void ForEach<T1, T2>(ref T1 c1, ref T2 c2);

    public static Routine Routine<T1, T2>(this World world, string name = "", ForEach<T1, T2> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>().Term<T2>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);
                var c2 = it.Field<T2>(2);

                foreach (int i in it)
                    callback(ref c1[i], ref c2[i]);
            });
    }

    public delegate void ForEachEntity<T1, T2>(Entity entity, ref T1 c1, ref T2 c2);

    public static Routine Routine<T1, T2>(this World world, string name = "", ForEachEntity<T1, T2> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>().Term<T2>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);
                var c2 = it.Field<T2>(2);

                foreach (int i in it)
                    callback(it.Entity(i), ref c1[i], ref c2[i]);
            });
    }

    public delegate void ForEach<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3);

    public static Routine Routine<T1, T2, T3>(this World world, string name = "", ForEach<T1, T2, T3> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>().Term<T2>().Term<T3>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);
                var c2 = it.Field<T2>(2);
                var c3 = it.Field<T3>(3);

                foreach (int i in it)
                    callback(ref c1[i], ref c2[i], ref c3[i]);
            });
    }

    public delegate void ForEachEntity<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);

    public static Routine Routine<T1, T2, T3>(this World world, string name = "", ForEachEntity<T1, T2, T3> callback = default)
    {
        return world.Routine(
            name: name,
            filter: world.FilterBuilder().Term<T1>().Term<T2>().Term<T3>(),
            callback: (Iter it) =>
            {
                var c1 = it.Field<T1>(1);
                var c2 = it.Field<T2>(2);
                var c3 = it.Field<T3>(3);

                foreach (int i in it)
                    callback(it.Entity(i), ref c1[i], ref c2[i], ref c3[i]);
            });
    }
    public static void PrepareGodotComponents(this World world)
    {
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(
                type => type.GetCustomAttributes(typeof(Component), true).Length > 0 &&
                type.IsSubclassOf(typeof(Node)));

        foreach (var type in types)
        {
            removeComponentSystemMethod
                .MakeGenericMethod(new Type[] { type })
                .Invoke(null, new object?[] { world });

            setSystemComponentMethod
                .MakeGenericMethod(new Type[] { type })
                .Invoke(null, new object?[] { world });
        }
    }

    private static MethodInfo removeComponentSystemMethod = typeof(Utils)
        .GetMethods()
        .First(m => m.ToString() == "Void RemoveComponentSystem[T](Flecs.NET.Core.World)");

    public static void RemoveComponentSystem<T>(this World world) where T : Node
    {
        world.Observer(
            filter: world.FilterBuilder().Term<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Iter it, int i) =>
            {
                var component = it.Field<T>(1)[i];
                component.Free();
            });
    }

    private static MethodInfo setSystemComponentMethod = typeof(Utils)
        .GetMethods()
        .First(m => m.ToString() == "Void SetComponentSystem[T](Flecs.NET.Core.World)");

    public static void SetComponentSystem<T>(this World world) where T : Node
    {
        world.Observer(
            filter: world.FilterBuilder().Term<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Iter it, int i) =>
            {
                var entity = it.Entity(i);
                var component = it.Field<T>(1)[i];

                if (entity.Has<EntityNode, Node>() && component.GetParentOrNull<Node>() == null)
                {
                    var root = entity.GetSecond<EntityNode, Node>();
                    root.AddChild(component);
                }
            });
    }
}