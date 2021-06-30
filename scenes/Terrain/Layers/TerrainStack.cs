using Godot;

public class TerrainStack : ITerrainStack
{
    private readonly ITerrainLayer walls;
    private readonly ITerrainLayer blocks;
    public Image WorldBlocksImage => blocks.WorldImage;
    public Image WorldWallsImage => walls.WorldImage;

    public TerrainStack(Image blocksImage, Image wallsImage)
    {
        blocks = new TerrainBlockLayer(blocksImage);
        walls = new TerrainWallLayer(wallsImage);
        walls.Lock();
        blocks.Lock();
    }
}
