using Godot;
using System;
using System.Collections.Generic;

public class BlockMapping {
    public String Name;
    public Color ReadableColour;
    public Color SaveableColour;
    // public Sprite sprite;

    public BlockMapping(String name, byte red, byte green, byte blue)
    {
        this.Name = name;
        this.ReadableColour = Color.Color8(red, green, blue);
    }

    public void SetSaveableColour(Color saveableColour)
    {
        this.SaveableColour = saveableColour;
    }
}

public class BlockList
{
    private HashSet<BlockMapping> blocks;
    private uint rgb;

    public BlockList()
    {
        rgb = 0;
        blocks = new HashSet<BlockMapping>();
    }

    /* TODO: This should be called by reading the contents of a file. */
    public void AddBlock(BlockMapping blockMapping)
    {
        rgb += 1;

        byte red = (byte)(rgb & 0xFF);
        byte green = (byte)((rgb & 0xFF00) >> 8);
        byte blue = (byte)((rgb & 0xFF0000) >> 16);
        if (rgb >= 256 << 16)
        {
            throw new InvalidOperationException();
        }

        Color nextSaveableColour = Color.Color8(red, green, blue);
        blockMapping.SetSaveableColour(nextSaveableColour);
        // GD.Print(String.Format("Red: {0}, Green: {1}, Blue: {2}, Type: {3}", red, green, blue, blockMapping.Name));
    }

    
}