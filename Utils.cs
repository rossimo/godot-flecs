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
            filter: world.FilterBuilder<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (ref T component) =>
            {
                component.QueueFree();
            });
    }

    private static MethodInfo setSystemComponentMethod = typeof(Utils)
        .GetMethods()
        .First(m => m.ToString() == "Void SetComponentSystem[T](Flecs.NET.Core.World)");

    public static void SetComponentSystem<T>(this World world) where T : Node
    {
        world.Observer(
            filter: world.FilterBuilder<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref T component) =>
            {
                if (entity.Has<EntityNode, Node>() && component.GetParentOrNull<Node>() == null)
                {
                    var root = entity.GetSecond<EntityNode, Node>();
                    root.AddChild(component);
                }
            });
    }
}