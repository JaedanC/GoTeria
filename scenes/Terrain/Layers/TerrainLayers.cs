using Godot;
using System;

public class TerrainLayers {
    private ITerrainLayer walls;
    private ITerrainLayer blocks;

    public TerrainLayers(String blocksImagePath, String wallsImagePath)
    {
        walls = new WallLayer(wallsImagePath);
        blocks = new BlockLayer(blocksImagePath);

        walls.Lock();
        blocks.Lock();
    }

    public Image GetWorldBlocksImage()
    {
        return blocks.WorldImage;
    }

    public Image GetWorldWallsImage()
    {
        return walls.WorldImage;
    }
}