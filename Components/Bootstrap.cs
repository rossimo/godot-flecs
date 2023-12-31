using Godot;
using Flecs.NET.Core;

public partial class BootstrapNode2D : Node2D, IBootstrap, IComplete
{
    public List<Node> Components = new List<Node>();

    public void Bootstrap()
    {
        foreach (var state in GetChildren())
        {
            Components.Add(state.Duplicate());

            state.QueueFree();
            RemoveChild(state);
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

    public void Complete(Entity entity)
    {
        foreach (var component in Components)
        {
            entity.SetNode(component.Duplicate());
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        foreach (var item in Components)
        {
            item.Free();
        }

        Components.Clear();
    }
}