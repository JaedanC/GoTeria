using Godot;

public class TerrainWallLayer : ITerrainLayer
{
    public Image WorldImage { get; }

    public BlockMapping BlockList { get; }

    public TerrainWallLayer(Image wallsImage)
    {
        WorldImage = wallsImage;
        BlockList = new BlockMapping();

        SetupBlockMappings();
    }

    public void Lock()
    {
        WorldImage.Lock();
    }

    private void SetupBlockMappings()
    {
        BlockList.AddBlock(new BlockInformation("Dirt Wall", 88, 61, 46));
        BlockList.AddBlock(new BlockInformation("Stone Wall", 85, 102, 103));
        BlockList.AddBlock(new BlockInformation("Mud Wall", 52, 43, 45));
    }
}
