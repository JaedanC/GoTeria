using Godot;
using System;

public class WorldSpawn : Node
{
    private const String title = "Teria";
    private InputLayering inputLayering;

    public override void _Ready()
    {
        inputLayering = GetNode<InputLayering>("/root/InputLayering");
    }

    public override void _Process(float delta)
    {
        OS.SetWindowTitle(String.Format("{0} | FPS: {1}", title, Engine.GetFramesPerSecond()));

        if (inputLayering.PopActionPressed("toggle_fullscreen"))
        {
            OS.WindowFullscreen = !OS.WindowFullscreen;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            InputEventKey keyEvent = (InputEventKey)@event;
            if (keyEvent.Alt && keyEvent.Scancode == (uint)KeyList.Enter && keyEvent.IsPressed())
            {
                OS.WindowFullscreen = !OS.WindowFullscreen;
            }
        }
    }

}
