using Godot;
using GodotTestDriver;
using FluentAssertions;
using Chickensoft.GoDotLog;
using Chickensoft.GoDotTest;
using GodotTestDriver.Util;

public class EntityTest : TestClass
{
    private readonly ILog LOG = new GDLog(nameof(EntityTest));

    private Fixture Fixture;

    public EntityTest(Node node) : base(node)
    {
        Fixture = new Fixture(TestScene.GetTree());
    }

    [Cleanup]
    public async Task Cleanup()
    {
        await Fixture.Cleanup();
    }

    [Test]
    public async Task CreateEntity()
    {
        var entityNode = new Entity2D();

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        entity.IsAlive().Should().BeTrue();
    }

    [Test]
    public async Task CreateEntityWithComponent()
    {
        var spriteNode = new Sprite2D();

        var entityNode = new Entity2D();
        entityNode.AddChild(spriteNode);

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        var spriteComponent = entity.Get<Sprite2D>();
        spriteComponent.Should().Be(spriteNode);
    }

    [Test]
    public async Task AddComponent()
    {
        var entityNode = new Entity2D();

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        var sprite = new Sprite2D();
        entity.Set(sprite);

        entityNode.GetChildren().Should().Contain(sprite);
    }

    [Test]
    public async Task AddNode()
    {
        var entityNode = new Entity2D();

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var spriteNode = new Sprite2D();
        entityNode.AddChild(spriteNode);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        var spriteComponent = entity.Get<Sprite2D>();
        spriteComponent.Should().Be(spriteNode);
    }

    [Test]
    public async Task AddSimilarNode()
    {
        var entityNode = new Entity2D();

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var sprite1Node = new Sprite2D();
        entityNode.AddChild(sprite1Node);

        var sprite2Node = new Sprite2D();
        entityNode.AddChild(sprite2Node);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        var spriteComponent = entity.Get<Sprite2D>();
        spriteComponent.Should().Be(sprite2Node);

        await Fixture.Tree.NextFrame();

        GDScript.IsInstanceValid(sprite1Node).Should().BeFalse();
    }

    [Test]
    public async Task FreeNode()
    {
        var entityNode = new Entity2D();

        var worldNode = new WorldNode();
        worldNode.AddChild(entityNode);

        await Fixture.AddToRoot(worldNode);

        var spriteNode = new Sprite2D();
        entityNode.AddChild(spriteNode);

        var world = worldNode.World;
        var entity = entityNode.GetEntity(world);

        entity.Has<Sprite2D>().Should().BeTrue();

        spriteNode.QueueFree();
        await Fixture.Tree.NextFrame();

        entity.Has<Sprite2D>().Should().BeFalse();
    }
}