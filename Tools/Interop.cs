using Godot;
using Flecs.NET.Core;
using System.Reflection;
using Flecs.NET.Bindings;

struct Parent { }

public static class Interop
{
    public static Entity CreateEntity(this Node node, World world, Entity entity = default)
    {
        if (!entity.IsValid())
        {
            var name = node.GetPath();
            entity = world.Entity(name);

            entity.ReflectionSet(node);
            entity.SetSecond<Parent, Node>(node);

            node.SetEntity(entity);
            node.TreeExiting += () => world.DeleteWith(entity);
        }

        node.ChildEnteredTree += (Node node) =>
        {
            if (entity.IsValid() && node.HasComponent())
            {
                entity.ReflectionSet(node);
            }
        };

        foreach (var child in node.GetChildren())
        {
            if (child.HasMany())
            {
                var childEntity = child.CreateEntity(world);
                childEntity.ChildOf(entity);
            }
            else
            {
                entity.ReflectionSet(child);
            }

            if (!child.HasComponent())
            {
                child.CreateEntity(world, entity);
            }
        }

        return entity;
    }

    public static void SetEntity(this GodotObject @object, Entity entity)
    {
        @object.SetMeta("entity", entity.Id.Value);
    }

    public static Entity GetEntity(this GodotObject @object, World world)
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
            return @object.GetEntity(world);
        }
    }

    public static Entity FindEntity(this Node node, World world)
    {
        var entity = node.GetEntity(world);
        if (entity.IsValid())
            return entity;

        var parent = node.GetParentOrNull<Node>();

        return parent == null
            ? Entity.Null()
            : parent.FindEntity(world);
    }

    public static bool HasComponent(this Type type)
    {
        return type.GetCustomAttributes(typeof(Component), true).Length > 0;
    }

    public static bool HasComponent(this Node node)
    {
        return node.GetType().HasComponent();
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
        var typeName = typeof(T).Name;
        var componentType = entity.CsWorld().Component<T>();

        entity.Children((Entity triggerEntity) =>
        {
            if (triggerEntity.Has<T>())
            {
                var triggerComponent = triggerEntity.Get<T>();
                var target = triggerComponent.Target;
                triggerEntity.Each((Id id) =>
                {
                    if (id.IsEntity() && id.TypeId() != componentType.Id.Value)
                    {
                        var symbol = id.Entity().Symbol();
                        var component = triggerEntity.ReflectionGet(symbol);
                        var clone = component is Node node
                            ? node.Duplicate()
                            : component;

                        if (clone == null)
                        {
                            throw new Exception($"Unable to create {symbol}");
                        }

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

    public static void Systems(World world)
    {
        world.Observer(
            filter: world.FilterBuilder().Term<Native.EcsComponent>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity) =>
            {
                var type = TypeByName(entity.Symbol());
                if (type == null) return;

                if (type.IsSubclassOf(typeof(Node)))
                {
                    removeNodeSystemMethod
                        .MakeGenericMethod(new Type[] { type })
                        .Invoke(null, new object?[] { world });

                    setNodeSystemMethod
                        .MakeGenericMethod(new Type[] { type })
                        .Invoke(null, new object?[] { world });
                }

                if (type.IsAssignableTo(typeof(Script)))
                {
                    scriptNodeSystemMethod
                        .MakeGenericMethod(new Type[] { type })
                        .Invoke(null, new object?[] { world });
                }
            });
    }

    private static MethodInfo scriptNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void ScriptNodeSystem[T](Flecs.NET.Core.World)");

    public static void ScriptNodeSystem<T>(this World world) where T : Script
    {
        world.Observer(
            filter: world.FilterBuilder<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref T component) =>
            {
                _ = component.Run(entity).ContinueWith(task =>
                {
                    if (entity.IsAlive())
                    {
                        entity.Remove<T>();
                    }

                    if (task.Exception != null && (task.Exception.InnerException is not EntityDeadException))
                    {
                        GD.PrintErr(task.Exception);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            });
    }

    private static MethodInfo removeNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void RemoveNodeSystem[T](Flecs.NET.Core.World)");

    public static void RemoveNodeSystem<T>(this World world) where T : Node
    {
        world.Observer(
            filter: world.FilterBuilder<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Entity entity, ref T component) =>
            {
                if (GDScript.IsInstanceValid(component))
                {
                    component.QueueFree();
                }
            });
    }

    private static MethodInfo setNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void SetNodeSystem[T](Flecs.NET.Core.World)");

    public static void SetNodeSystem<T>(this World world) where T : Node
    {
        world.Observer(
            filter: world.FilterBuilder<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref T node) =>
            {
                if (entity.Has<Parent, Node>() &&
                    node.GetParentOrNull<Node>() == null)
                {
                    var parent = entity.GetSecond<Parent, Node>();

                    if (!node.HasMany())
                    {
                        foreach (var child in parent.GetChildren())
                        {
                            if (child.GetType() == node.GetType())
                            {
                                child.ReplaceBy(node);
                                child.QueueFree();
                                return;
                            }
                        }
                    }

                    parent.AddChild(node);
                }
            });
    }

    public static void Cleanup(this Entity entity)
    {
        var count = 0;

        entity.Each((id) => count++);

        if (count == 0)
        {
            entity.Destruct();
        }
    }

    public static Type? TypeByName(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
        {
            var type = assembly.GetType(name);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
