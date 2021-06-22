using Godot;
using System;

public class TerrainStack : ITerrainStack
{
    private ITerrainLayer walls;
    private ITerrainLayer blocks;
    public Image WorldBlocksImage { get { return blocks.WorldImage; } }
    public Image WorldWallsImage { get { return walls.WorldImage; } }

    public TerrainStack(String blocksImagePath, String wallsImagePath)
    {
        walls = new TerrainWallLayer(wallsImagePath);
        blocks = new TerrainBlockLayer(blocksImagePath);
        walls.Lock();
        blocks.Lock();
    }
}
