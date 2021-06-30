using Godot;

public class TerrainBlockLayer : ITerrainLayer
{
    public Image WorldImage { get; }

    public BlockMapping BlockList { get; }

    public TerrainBlockLayer(Image blocksImage)
    {
        WorldImage = blocksImage;
        BlockList = new BlockMapping();

        SetupBlockMappings();
    }

    public void Lock()
    {
        WorldImage.Lock();
    }

    private void SetupBlockMappings()
    {
        BlockList.AddBlock(new BlockInformation("Dirt", 151, 107, 75));
        BlockList.AddBlock(new BlockInformation("Sand", 255, 218, 56));
        BlockList.AddBlock(new BlockInformation("Stone", 128, 128, 128));
    }
}
