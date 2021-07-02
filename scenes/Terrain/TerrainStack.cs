using Godot;


public class TerrainStack
{
    public Image WorldBlocksImage { get; }

    public Image WorldWallsImage { get; }

    public Image WorldLiquidsImage { get; }

    public TerrainStack(Image blocks, Image walls, Image liquids)
    {
        this.WorldBlocksImage = blocks;
        this.WorldWallsImage = walls;
        this.WorldLiquidsImage = liquids;
        this.WorldBlocksImage.PremultiplyAlpha();
        this.WorldWallsImage.PremultiplyAlpha();
        this.WorldLiquidsImage.PremultiplyAlpha();
        walls.Lock();
        blocks.Lock();
        liquids.Lock();
    }
}
