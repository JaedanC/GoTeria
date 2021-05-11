using Godot;
using System;

public class GravityComponent : Node2D
{
    private bool enabled = false;

    // Called when the node enters the scene tree for the first time.
    public override void _PhysicsProcess(float _delta)
    {
        if (enabled)
        {
            Player parent = GetParent<Player>();
            parent.SetVelocity(parent.GetVelocity() + new Vector2(0, 98));
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
