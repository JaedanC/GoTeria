using Godot;
using System;

public class TerrainStack : ITerrainStack
{
    private ITerrainLayer walls;
    private ITerrainLayer blocks;
    public Image WorldBlocksImage { get { return blocks.WorldImage; } }
    public Image WorldWallsImage { get { return walls.WorldImage; } }

    public TerrainStack(Image blocksImage, Image wallsImage)
    {
        blocks = new TerrainBlockLayer(blocksImage);
        walls = new TerrainWallLayer(wallsImage);
        walls.Lock();
        blocks.Lock();
    }
}
