using Godot;
using System;

public class Camera : Camera2D
{
    public override void _Ready()
    {
        Name = "Camera";
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        Align();
    }
}
