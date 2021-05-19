using Godot;
using System;
using System.Collections.Generic;


public class BlockMapping
{
    private HashSet<BlockInformation> blocks;
    private uint rgb;

    public BlockMapping()
    {
        rgb = 0;
        blocks = new HashSet<BlockInformation>();
    }

    /* TODO: This should be called by reading the contents of a file. */
    public void AddBlock(BlockInformation blockInformation)
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
        blockInformation.SetSaveableColour(nextSaveableColour);
        // GD.Print(String.Format("Red: {0}, Green: {1}, Blue: {2}, Type: {3}", red, green, blue, blockMapping.Name));
    }

    
}