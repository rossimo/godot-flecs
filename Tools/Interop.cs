using Godot;
using Flecs.NET.Core;
using System.Reflection;
using Flecs.NET.Bindings;

public static class Interop
{
    public static Entity CreateEntity(this Node node, World world)
    {
        var entity = world.Entity(node.GetPath());

        node.ChildEnteredTree += component =>
        {
            if (component is not Entity2D)
            {
                entity.SetNode(component);
            }
        };

        node.TreeExiting += () =>
        {
            entity.DestructSafely();
        };

        entity.SetNode(node);

        foreach (var component in node.GetChildren())
        {
            entity.SetNode(component);
        }

        return entity;
    }

    public static void SetNode(this Entity entity, Node component)
    {
        var type = component.GetType();
        MethodInfo? set = null;

        if (setCache.ContainsKey(type))
        {
            set = setCache[type];
        }

        if (set == null)
        {
            set = setMethod.MakeGenericMethod([type]);
            setCache.Add(type, set);
        }

        set.Invoke(entity, [entity, component]);
    }

    private static Dictionary<Type, MethodInfo> setCache = new Dictionary<Type, MethodInfo>();

    private static MethodInfo setMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void SetNode[T](Flecs.NET.Core.Entity, T)");

    public static void SetNode<T>(this Entity entity, T component) where T : Node
    {
        var world = entity.CsWorld();

        // this component has already been set
        if (component.GetEntity(world).IsAlive())
        {
            return;
        }

        if (component.HasMany())
        {
            var many = world.Entity();
            entity.Set(many, component);
        }
        else
        {
            entity.Set(component);
        }

        component.TreeExiting += () =>
        {
            if (!entity.IsAlive())
            {
                return;
            }

            if (component.HasMany())
            {
                entity.Each<T>(many =>
                {
                    if (entity.Has<T>(many) && entity.Get<T>(many) == component)
                    {
                        entity.Remove<T>(many);
                    }
                });
            }
            else
            {
                if (entity.Has<T>() && entity.Get<T>() == component)
                {
                    entity.Remove<T>();
                }
            }
        };
    }

    public static void SetEntityMeta(this GodotObject @object, Entity entity)
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
            return default;
        }
    }

    public static UntypedComponent GetComponent(this object @object, World world)
    {
        return world.Component(@object.GetType().Name);
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
        self.Each<T>(many =>
        {
            Trigger(self, other, self.Get<T>(many));
        });

        if (self.Has<T>())
        {
            Trigger(self, other, self.Get<T>());
        }
    }

    private static void Trigger<T>(Entity self, Entity other, T trigger) where T : Trigger
    {
        if (self.Has<Debug>())
        {
            GD.Print($"{self} triggering {trigger.GetType()}");
        }

        switch (trigger.Target)
        {
            case TargetSelf:
                trigger.Invoke(self);
                break;

            case TargetOther:
                if (other.IsAlive())
                {
                    trigger.Invoke(other);
                }
                break;
        }
    }

    public static void Observers(World world)
    {
        world.Observer()
            .Term<Native.EcsComponent>()
            .Event(Ecs.OnSet)
            .Each((Entity entity) =>
            {
                var type = TypeByName(entity.Symbol());
                if (type == null) return;

                if (type.IsSubclassOf(typeof(Node)))
                {
                    initializeNodeComponentMethod
                        .MakeGenericMethod([type])
                        .Invoke(null, [world, entity]);
                }

                if (type.IsAssignableTo(typeof(Script)))
                {
                    initializeScriptComponentMethod
                        .MakeGenericMethod([type])
                        .Invoke(null, [world, entity]);
                }
            });
    }

    private static MethodInfo initializeNodeComponentMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void InitializeNodeComponent[N](Flecs.NET.Core.World, Flecs.NET.Core.Entity)");

    public static void InitializeNodeComponent<N>(World world, Entity componentId) where N : Node
    {
        SetNodeSystem<N>(world, componentId);
        RemoveNodeSystem<N>(world);
    }

    private static MethodInfo initializeScriptComponentMethod = typeof(Interop)
        .GetMethods()
        .First(m => m.ToString() == "Void InitializeScriptComponent[S](Flecs.NET.Core.World, Flecs.NET.Core.Entity)");

    public static void InitializeScriptComponent<S>(World world, Entity componentId) where S : Script
    {
        ScriptNodeObserver<S>(world);
    }

    public static void ScriptNodeObserver<S>(this World world) where S : Script
    {
        world.Observer<S>()
            .Event(Ecs.OnSet)
            .Each((Entity entity, ref S script) => RunScript(entity, script));
    }

    static async void RunScript<S>(Entity entity, S script) where S : Script
    {
        var entityId = entity.Id.Value;

        var world = entity.CsWorld();

        try
        {
            await script.Run(entity);
            script.Invoke(entity);
        }
        catch (Exception exception)
        {
            if (((exception is DeadEntityException entityEx && entityEx.EntityId != entity.Id.Value)
                    || exception is not DeadEntityException) &&
                ((exception is ScriptRemovedException scriptEx && scriptEx.EntityId != entityId)
                    || exception is not ScriptRemovedException) &&
                !entity.Has<Destructing>() &&
                !world.Has<Destructing>())
            {
                GD.PrintErr(exception);
            }
        }
        finally
        {
            if (GDScript.IsInstanceValid(script))
            {
                script.QueueFree();
            }
        }
    }

    public static void RemoveNodeSystem<C>(this World world) where C : Node
    {
        world.Observer()
            .Term<C>()
            .Event(Ecs.OnRemove)
            .Each((Entity entity, ref C component) =>
            {
                if (entity.Has<Debug>())
                {
                    GD.Print($"{entity} remove {component.GetType()}");
                }

                if (component is IAsync async)
                {
                    async.SetException(world, new ComponentRemovedException<C>(entity));
                }

                if (GDScript.IsInstanceValid(component))
                {
                    component.QueueFree();
                }
            });
    }

    public static void SetNodeSystem<C>(this World world, Entity componentId) where C : Node
    {
        world.Observer<C>()
            .Event(Ecs.OnSet)
            .Each((Entity entity, ref C component) =>
            {
                if (entity.Has<Debug>())
                {
                    GD.Print($"{entity} set {component.GetType()}");
                }

                component.SetEntityMeta(entity);

                if (entity.Has<Entity2D>())
                {
                    var entityNode = entity.Get<Entity2D>();

                    if (GDScript.IsInstanceValid(entityNode) && component != entityNode)
                    {
                        var componentDeref = component;
                        if (!component.HasMany())
                        {
                            foreach (var previous in entityNode.GetChildren().Where(sibling =>
                                        sibling != componentDeref &&
                                        sibling.GetType() == componentDeref.GetType()))
                            {
                                previous.QueueFree();
                            }
                        }

                        if (component.GetParentOrNull<Node>() == null)
                        {
                            if (component is IBootstrap bootstrap && !bootstrap.Bootstrapped())
                            {
                                bootstrap.Bootstrap();
                            }

                            entityNode.AddChild(component);
                        }
                    }
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


    public static void DestructSafely(this Entity entity)
    {
        entity.Add<Destructing>();
        entity.Destruct();
    }

    public static void AssertAlive(this Entity entity)
    {
        if (!entity.IsValid() || !entity.IsAlive())
        {
            throw new DeadEntityException(entity);
        }
    }

    public static IEnumerable<Entity> All(this Query query)
    {
        var entities = new List<Entity>();

        query.Each((Entity entity) =>
        {
            entities.Add(entity);
        });

        return entities;
    }

    public static IEnumerable<C> All<C>(this Query query)
    {
        var components = new List<C>();

        query.Each((ref C component) =>
        {
            components.Add(component);
        });

        return components;
    }

    public static Routine Async(this Routine routine, World world)
    {
        world.Routine<Asyncs>()
            .NoReadonly()
            .Each((ref Asyncs tasks) =>
            {
                world.DeferSuspend();

                try
                {
                    tasks.Async();
                }
                finally
                {
                    world.DeferResume();
                }
            });

        return routine;
    }
}

public struct Destructing;

public class ComponentRemovedException<C> : Exception
{
    public readonly Entity Entity;

    public ComponentRemovedException(Entity entity) : base($"{typeof(C)} has been removed from {entity}")
    {
        Entity = entity;
    }
}