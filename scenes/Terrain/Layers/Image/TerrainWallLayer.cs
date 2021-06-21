using Godot;
using System;

public class TerrainWallLayer : ITerrainLayer
{
    private Image wallsImage;
    private BlockMapping walls;
    public Image WorldImage { get { return wallsImage; } }
    public BlockMapping BlockList { get { return walls; } }

    public TerrainWallLayer(String wallsImagePath)
    {
        Texture worldWallsTexture = (Texture)GD.Load(wallsImagePath);
        wallsImage = worldWallsTexture.GetData();
        walls = new BlockMapping();

        SetupBlockMappings();
    }

    public void Lock()
    {
        wallsImage.Lock();
    }

    private void SetupBlockMappings()
    {
        walls.AddBlock(new BlockInformation("Dirt Wall", 88, 61, 46));
        walls.AddBlock(new BlockInformation("Stone Wall", 85, 102, 103));
        walls.AddBlock(new BlockInformation("Mud Wall", 52, 43, 45));
    }
}
