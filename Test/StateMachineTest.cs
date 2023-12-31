using Godot;
using GodotTestDriver;
using FluentAssertions;
using Chickensoft.GoDotLog;
using Chickensoft.GoDotTest;
using GodotTestDriver.Util;

public class StateMachineTest : TestClass
{
    private readonly ILog LOG = new GDLog(nameof(StateMachineTest));

    private Fixture Fixture;

    public StateMachineTest(Node node) : base(node)
    {
        Fixture = new Fixture(TestScene.GetTree());
    }

    [Cleanup]
    public async Task Cleanup()
    {
        await Fixture.Cleanup();
    }

    [Test]
    public async Task BootstrapTest()
    {
        var worldNode = new WorldNode();
        var world = worldNode.World;

        StateMachines.Observers(world);
        StateMachines.Systems(world);

        {
            var machine = new StateMachine()
            {
                Name = "machine",
                State = "state1"
            };

            var state1 = new State() { Name = "state1" };
            state1.AddChild(new Label() { Text = "state 1 text" });
            machine.AddChild(state1);

            var state2 = new State() { Name = "state2" };
            state2.AddChild(new Label() { Text = "state 2 text" });
            machine.AddChild(state2);

            var entityNode = new Entity2D() { Name = "entity" };
            entityNode.AddChild(machine);

            worldNode.AddChild(entityNode);
        }

        worldNode = Bootstrap.PrepareNode(worldNode);
        worldNode = await Fixture.AddToRoot(worldNode);

        {
            var node = worldNode.GetNode("entity");
            var entity = node.GetEntity(world);
            var machine = node.GetNode<StateMachine>("machine");

            {
                entity.Has<Label>().Should().BeTrue();
                entity.Get<Label>().Text.Should().Be("state 1 text");

                machine.GetChildren().Count().Should().Be(1);

                var child = machine.GetChildren().First();
                child.Name.Should().Be(new StringName("state1"));
            }

            {
                machine.ChangeState("state2");

                entity.Has<Label>().Should().BeTrue();
                entity.Get<Label>().Text.Should().Be("state 2 text");

                machine.GetChildren().Count().Should().Be(1);

                var child = machine.GetChildren().First();
                child.Name.Should().Be(new StringName("state2"));
            }
        }
    }
}