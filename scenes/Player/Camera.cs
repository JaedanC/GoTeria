using Godot;
using System;

public class Camera : Camera2D
{
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        Align();
    }
}
