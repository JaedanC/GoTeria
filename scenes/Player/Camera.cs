using Godot;
using System;

public class Camera : Camera2D
{
    public override void _Ready()
    {
        Name = "Camera";
    }

    // This is required to stop the camera from being a frame behind the player.
    public override void _Process(float delta)
    {
        Align();
    }
}
