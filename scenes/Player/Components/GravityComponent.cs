using Godot;
using System;

public class GravityComponent : Node2D
{
    private bool enabled = true;

    public override void _Ready()
    {
        Name = "GravityComponent";
    }

    /* Adds Gravity to the Node. */
    public override void _PhysicsProcess(float _delta)
    {
        if (enabled)
        {
            ICollidable parent = GetParent<ICollidable>();
            parent.SetVelocity(parent.GetVelocity() + new Vector2(0, 98));
        }
    }
}