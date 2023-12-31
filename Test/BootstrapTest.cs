using Godot;
using GodotTestDriver;
using FluentAssertions;
using Chickensoft.GoDotLog;
using Chickensoft.GoDotTest;

public class BootstrapTest : TestClass
{
    private readonly ILog LOG = new GDLog(nameof(BootstrapTest));

    private Fixture Fixture;

    public BootstrapTest(Node testScene) : base(testScene)
    {
        Fixture = new Fixture(TestScene.GetTree());
    }

    [Cleanup]
    public async Task Cleanup()
    {
        await Fixture.Cleanup();
    }

    [Test]
    public async Task Test()
    {
        await Fixture.AddToRoot(new BootstrapNode()
        {
            Name = "Bootstrap",
            Scene = ResourceLoader.Load<PackedScene>("res://game.tscn")
        });

        var bootstrap = Fixture.Tree.Root.GetNode("Bootstrap");
        bootstrap.Should().BeNull();

        var game = Fixture.Tree.Root.GetNode("Game");
        game.Should().NotBeNull();
    }
}