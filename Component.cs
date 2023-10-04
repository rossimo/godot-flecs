using System;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)
]
public sealed class Component : Attribute
{
    public Component()
    {
    }
}