using Godot;
using Flecs.NET.Core;

public partial class BootstrapNode2D : Node2D, IBootstrap, IComplete
{
    public List<Node> Components = new List<Node>();

    public void Bootstrap()
    {
        foreach (var child in GetChildren())
        {
            Components.Add(child.Duplicate());

            child.QueueFree();
            RemoveChild(child);
        }

        SetMeta("bootstrap", true);
    }

    public bool Bootstrapped()
    {
        return GetMeta("bootstrap", false).AsBool();
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Bootstrapped() && GetChildren().Count > 0)
        {
            GD.PrintErr($"{GetPath()} is being bootstrapped late with {GetChildren().Count} children!");
            Bootstrap();
        }
    }

    public virtual void Complete(Entity entity)
    {
        foreach (var component in Components)
        {
            entity.SetNode(component.Duplicate());
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        foreach (var component in Components)
        {
            component.Free();
        }

        Components.Clear();
    }
}