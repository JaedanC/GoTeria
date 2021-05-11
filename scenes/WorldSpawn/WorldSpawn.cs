using Godot;
using System;

public class WorldSpawn : Node
{
    private String title = "Teria";

    public override void _Process(float delta)
    {
        OS.SetWindowTitle(String.Format("{0} | FPS: {1}", title, Engine.GetFramesPerSecond()));
    }

}
