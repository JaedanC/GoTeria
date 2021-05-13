using Godot;
using System;

public class GravityComponent : Node2D
{
    private bool enabled = false;

    public override void _Ready()
    {
        Name = "GravityComponent";
    }

    /* Adds Gravity to the Node. */
    // TODO: In future, use an Interface that has a Velocity.
    public override void _PhysicsProcess(float _delta)
    {
        if (enabled)
        {
            ICollidable parent = GetParent<ICollidable>();
            parent.Velocity = parent.Velocity + new Vector2(0, 98);
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
