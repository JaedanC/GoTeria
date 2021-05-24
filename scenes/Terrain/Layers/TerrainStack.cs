using Godot;
using System;

public class TerrainStack {
    private ITerrainLayer walls;
    private ITerrainLayer blocks;

    public TerrainStack(String blocksImagePath, String wallsImagePath)
    {
        walls = new TerrainWallLayer(wallsImagePath);
        blocks = new TerrainBlockLayer(blocksImagePath);

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