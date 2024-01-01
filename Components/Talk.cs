using Godot;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class Talk : Node2D
{
    [Export]
    public string Text
    {
        get { return _Text; }
        set
        {
            _Text = value;
            if (Label != null)
            {
                Label.Text = _Text;
            }
        }
    }

    [Export]
    public float Duration { get; set; } = 2;

    private string _Text = "";

    private Label? Label = null;

    public override void _Ready()
    {
        base._Ready();

        Position = new Vector2(0, -25);

        Label = new Label()
        {
            Text = Text,
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(600, 50),
            Position = new Vector2(-300, -25)
        };

        AddChild(Label);

        if (Duration > 0)
        {
            var timer = GetTree().CreateTimer(Duration);
            timer.Timeout += QueueFree;
        }
    }
}
