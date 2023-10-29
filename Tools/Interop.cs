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
            if (entity.IsValid())
            {
                DiscoverComponent(entity, node);
            }
        };

        foreach (var child in node.GetChildren())
        {
            DiscoverComponent(entity, child);
        }

        return entity;
    }

    public static void DiscoverComponent(Entity entity, Node node)
    {
        var world = entity.CsWorld();

        if (node.HasMany())
        {
            var child = node.CreateEntity(world);
            child.ChildOf(entity);
        }
        else
        {
            if (entity.ReflectionHas(node.GetType()))
            {
                var existing = entity.ReflectionGet(node.GetType());
                if (existing == node) return;
            }

            entity.ReflectionSet(node);
        }
    }

    public static void SetEntity(this GodotObject @object, Entity entity)
    {
        @object.SetMeta("entity", entity.Id.Value);
    }

    public static Entity GetEntity(this GodotObject @object, World world)
    {
        if (@object.HasMeta("entity"))
        {
            var id = @object.GetMeta("entity").AsUInt64();
            return world.Entity(id);
        }
        else
        {
            return Entity.Null();
        }
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
        {
            return entity;
        }

        var parent = node.GetParentOrNull<Node>();

        return parent == null
            ? Entity.Null()
            : parent.FindEntity(world);
    }

    public static bool HasMany(this Type type)
    {
        return type.GetCustomAttributes(typeof(Many), true).Length > 0;
    }

    public static bool HasMany(this Node node)
    {
        return node.GetType().HasMany();
    }

    public static void Trigger<T>(this Entity self, Entity other = default) where T : Trigger
    {
        var typeName = typeof(T).Name;
        var componentType = self.CsWorld().Component<T>();

        self.Children((Entity triggerEntity) =>
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
                            self.ReflectionSet(clone);
                        }
                        else if (target is TargetOther && other.IsAlive())
                        {
                            other.ReflectionSet(clone);
                        }
                    }
                });
            }
        });
    }

    public static void Observers(World world)
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
                    scriptNodeObserverMethod
                        .MakeGenericMethod(new Type[] { type })
                        .Invoke(null, new object?[] { world });

                    scriptNodeSystemMethod
                        .MakeGenericMethod(new Type[] { type })
                        .Invoke(null, new object?[] { world });
                }
            });
    }

    private static MethodInfo scriptNodeObserverMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void ScriptNodeObserver[S](Flecs.NET.Core.World)");

    public static void ScriptNodeObserver<S>(this World world) where S : Script
    {
        world.Observer(
            filter: world.FilterBuilder<S>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref S component) =>
            {
                Task<S>(entity, component);
            });
    }

    private static MethodInfo scriptNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void ScriptNodeSystem[S](Flecs.NET.Core.World)");

    public struct ScriptIterator { }

    public static void ScriptNodeSystem<S>(this World world) where S : Script
    {
        world.Add<ScriptIterator>();

        var query = world.Query(filter: world.FilterBuilder().With<S>());

        world.Routine(
            filter: world.FilterBuilder<ScriptIterator>(),
            routine: world.RoutineBuilder().NoReadonly(),
            callback: (Entity entity) =>
            {
                var scripts = query.All<S>();
                if (scripts.Count() == 0) return;

                world.DeferSuspend();
                foreach (var script in scripts)
                {
                    while (script.Iterate()) { }
                }
                world.DeferResume();
            });
    }

    static async void Task<T>(Entity entity, Script script)
    {
        try
        {
            await script.Run(entity);

            entity.Conclude(script);
        }
        catch (Exception exception)
        {
            if ((exception is not DeadEntityException) &&
                (exception is not ScriptRemovedException))
            {
                GD.PrintErr(exception);
            }
        }
        finally
        {
            if (entity.IsAlive())
            {
                entity.Remove<T>();
            }
        }
    }

    private static MethodInfo removeNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void RemoveNodeSystem[C](Flecs.NET.Core.World)");

    public static void RemoveNodeSystem<C>(this World world) where C : Node
    {
        world.Observer(
            filter: world.FilterBuilder<C>(),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Entity entity, ref C component) =>
            {
                if (GDScript.IsInstanceValid(component))
                {
                    component.QueueFree();
                }
            });
    }

    private static MethodInfo setNodeSystemMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void SetNodeSystem[C](Flecs.NET.Core.World)");

    public static void SetNodeSystem<C>(this World world) where C : Node
    {
        world.Observer(
            filter: world.FilterBuilder<C>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref C node) =>
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

    public static void AssertAlive(this Entity entity)
    {
        if (!entity.IsAlive())
        {
            throw new DeadEntityException(entity);
        }
    }

    public static void Conclude<N>(this Entity entity, N node) where N : Node
    {
        if (!GDScript.IsInstanceValid(node))
        {
            return;
        }

        var children = node.GetChildren();

        var remove = true;

        foreach (var child in children)
        {
            if (child is N)
            {
                remove = false;
            }

            entity.ReflectionSet(child);
        }

        if (remove)
        {
            entity.Remove<N>();
        }
    }

    public static IEnumerable<Entity> All(this Query query)
    {
        List<Entity>? entities = null;

        query.Each((Entity entity) =>
        {
            if (entities == null)
            {
                entities = new List<Entity>();
            }

            entities.Add(entity);
        });

        return entities == null
            ? Array.Empty<Entity>()
            : entities;
    }

    public static IEnumerable<S> All<S>(this Query query)
    {
        List<S>? components = null;

        query.Each((ref S script) =>
        {
            if (components == null)
            {
                components = new List<S>();
            }

            components.Add(script);
        });

        return components == null
            ? Array.Empty<S>()
            : components;
    }
}
