using Godot;
using System;

[GlobalClass, Component]
public partial class MyComponent : Node
{
	[Export]
	public int Value { get; set; }
}
