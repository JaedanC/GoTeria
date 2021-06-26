using Godot;
using System;

public class TerrainBlockLayer : ITerrainLayer
{
    private Image blocksImage;
    private BlockMapping blocks;
    public Image WorldImage { get { return blocksImage; } }
    public BlockMapping BlockList { get { return blocks; } }

    public TerrainBlockLayer(Image blocksImage)
    {
        this.blocksImage = blocksImage;
        blocks = new BlockMapping();

        SetupBlockMappings();
    }

    public void Lock()
    {
        blocksImage.Lock();
    }

    private void SetupBlockMappings()
    {
        blocks.AddBlock(new BlockInformation("Dirt", 151, 107, 75));
        blocks.AddBlock(new BlockInformation("Sand", 255, 218, 56));
        blocks.AddBlock(new BlockInformation("Stone", 128, 128, 128));
    }
}
