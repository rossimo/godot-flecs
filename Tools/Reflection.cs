using Godot;
using Flecs.NET.Core;
using System.Reflection;

public static class Reflection
{
    private static Dictionary<Type, MethodInfo> setComponentCache = new Dictionary<Type, MethodInfo>();

    private static Dictionary<string, MethodInfo> getComponentCache = new Dictionary<string, MethodInfo>();

    private static Dictionary<Type, MethodInfo> removeComponentCache = new Dictionary<Type, MethodInfo>();

    private static MethodInfo entitySetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Set[T](T)");

    private static MethodInfo entityGetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "T& Get[T]()");

    private static MethodInfo entityRemoveComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Remove[T]()");

    public static void ReflectionSet(this Entity entity, object component)
    {
        var type = component.GetType();
        MethodInfo? set = null;

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

    public static object? ReflectionGet(this Entity entity, string typeName)
    {
        MethodInfo? get = null;

        if (getComponentCache.ContainsKey(typeName))
        {
            get = getComponentCache[typeName];
        }

        if (get == null)
        {
            var type = Assembly.GetExecutingAssembly().GetType(typeName);
            if (type == null) throw new Exception($"Type {typeName} not found");

            get = entityGetComponentdMethod.MakeGenericMethod(new Type[] { type });
            getComponentCache.Add(typeName, get);
        }

        return get.Invoke(entity, Array.Empty<object>());
    }

    public static void ReflectionRemove(this Entity entity, Type type)
    {
        MethodInfo? remove = null;

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
}
