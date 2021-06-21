using Godot;
using System;

public class BlockInformation
{
    public String Name;
    public Color ReadableColour;
    public Color SaveableColour;
    // public Sprite sprite;

    public BlockInformation(String name, byte red, byte green, byte blue)
    {
        this.Name = name;
        this.ReadableColour = Color.Color8(red, green, blue);
    }

    public void SetSaveableColour(Color saveableColour)
    {
        this.SaveableColour = saveableColour;
    }
}
