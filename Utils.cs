using Godot;
using Flecs.NET.Core;
using System.Reflection;

struct EntityNode { }

public static class Utils
{
    public static Entity DiscoverEntity(this Node node, World world, Entity entity = default)
    {
        if (!entity.IsValid())
        {
            var name = node.GetPath();
            GD.Print($"Creating {name}");
            entity = world.Entity(name);
            entity.ReflectionSet(node);
            entity.SetSecond<EntityNode, Node>(node);
            node.SetEntity(entity);
            node.TreeExiting += () => world.DeleteWith(entity);
        }

        node.ChildEnteredTree += (Node node) =>
        {
            if (entity.IsValid() && node.GetType().HasComponent())
            {
                entity.ReflectionSet(node);
            }
        };

        foreach (var child in node.GetChildren())
        {
            var type = child.GetType();
            if (type.HasMany())
            {
                var element = child.DiscoverEntity(world);
                GD.Print($"Adding child {element} to {entity}");
                element.ChildOf(entity);
            }
            else
            {
                entity.ReflectionSet(child);
            }

            if (type.HasComponent())
            {
                child.TreeExiting += () => entity.ReflectionRemove(type);
            }
            else
            {
                child.DiscoverEntity(world, entity);
            }
        }

        return entity;
    }

    public static void SetEntity(this GodotObject @object, Entity entity)
    {
        @object.SetMeta("entity", entity.Id.Value);
    }

    public static Entity DeserializeEntity(this GodotObject @object, World world)
    {
        if (!@object.HasMeta("entity"))
            return Entity.Null();

        var value = @object.GetMeta("entity").AsUInt64();
        return world.Entity(value);
    }

    public static Entity FindEntity(this GodotObject @object, World world)
    {
        if (@object is Node node)
        {
            return node.FindEntity(world);
        }
        else
        {
            return @object.DeserializeEntity(world);
        }
    }

    public static Entity FindEntity(this Node node, World world)
    {
        var entity = node.DeserializeEntity(world);
        if (entity.IsValid())
            return entity;

        var parent = node.GetParentOrNull<Node>();

        return parent == null
            ? Entity.Null()
            : parent.FindEntity(world);
    }

    private static Dictionary<Type, MethodInfo> setComponentCache = new Dictionary<Type, MethodInfo>();

    private static Dictionary<string, MethodInfo> getComponentCache = new Dictionary<string, MethodInfo>();

    private static Dictionary<Type, MethodInfo> removeComponentCache = new Dictionary<Type, MethodInfo>();


    private static MethodInfo entitySetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Set[T](T)");

    private static MethodInfo entityGetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "T& Get[T]()");

    public static void ReflectionSet(this Entity entity, object component)
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

    public static object ReflectionGet(this Entity entity, string componentName)
    {
        MethodInfo get = null;

        if (getComponentCache.ContainsKey(componentName))
        {
            get = getComponentCache[componentName];
        }

        if (get == null)
        {
            var type = Assembly.GetExecutingAssembly().GetType(componentName);

            get = entityGetComponentdMethod.MakeGenericMethod(new Type[] { type });
            getComponentCache.Add(componentName, get);
        }

        return get.Invoke(entity, Array.Empty<object>());
    }

    private static MethodInfo entityRemoveComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Remove[T]()");

    public static void ReflectionRemove(this Entity entity, Type type)
    {
        MethodInfo remove = null;

        if (removeComponentCache.ContainsKey(type))
        {
            remove = removeComponentCache[type];
        }

        if (remove == null)
        {
            remove = entityRemoveComponentdMethod.MakeGenericMethod(new Type[] { type });
            removeComponentCache.Add(type, remove);
        }

        remove.Invoke(entity, Array.Empty<object>());
    }

    public static void PrepareGodotComponents(this World world)
    {
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type.HasComponent() &&
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
            callback: (Entity entity, ref T component) =>
            {
                if (GDScript.IsInstanceValid(component))
                {
                    GD.Print($"Removing {component.GetType()} from {entity}");
                    component.QueueFree();
                }
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
                if (GDScript.IsInstanceValid(component) &&
                    entity.Has<EntityNode, Node>() &&
                    component.GetParentOrNull<Node>() == null)
                {
                    GD.Print($"Setting {component.GetType()} to {entity}");
                    var root = entity.GetSecond<EntityNode, Node>();
                    root.AddChild(component);
                }
            });
    }

    public static bool HasComponent(this Type type)
    {
        return type.GetCustomAttributes(typeof(Component), true).Length > 0;
    }

    public static bool HasMany(this Type type)
    {
        return type.GetCustomAttributes(typeof(Many), true).Length > 0;
    }

    public static bool HasMany(this Node node)
    {
        return node.GetType().HasMany();
    }

    public static void Trigger<T>(this Entity entity, Entity other = default) where T : Trigger
    {
        if (!entity.IsValid())
        {
            return;
        }

        var typeName = typeof(T).Name;
        entity.Children((Entity triggerEntity) =>
        {
            if (triggerEntity.Has<T>())
            {
                var triggerComponent = triggerEntity.Get<T>();
                var target = triggerComponent.Target;
                triggerEntity.Each((Id id) =>
                {
                    var name = id.ToString();
                    if (id.IsEntity() && name != typeName)
                    {
                        var component = triggerEntity.ReflectionGet(name);
                        var clone = component is Node node
                            ? node.Duplicate()
                            : component;

                        if (target is TargetSelf)
                        {
                            entity.ReflectionSet(clone);
                        }
                        else if (target is TargetOther && other.IsValid())
                        {
                            other.ReflectionSet(clone);
                        }
                    }
                });
            }
        });
    }
}
