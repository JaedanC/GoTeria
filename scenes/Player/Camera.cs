using Godot;
using System;

public class Camera : Camera2D
{
    private const float ZOOM_CLAMP = 20f;

    /* Hide the Camera2D's zoom so we can clamp the value. */
    public new Vector2 Zoom
    {
        get
        {
            return base.Zoom;
        }
        set
        {
            base.Zoom = new Vector2(
                Mathf.Clamp(value.x, 1, ZOOM_CLAMP),
                Mathf.Clamp(value.y, 1, ZOOM_CLAMP)
            );
        }
    }
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
