using Godot;


public class WorldImage
{
    private Terrain terrain;
    private Image blocks;
    private Image walls;
    private Image liquids;

    public WorldImage(Terrain terrain, Image blocks, Image walls, Image liquids)
    {
        this.terrain = terrain;
        this.blocks = blocks;
        this.walls = walls;
        this.liquids = liquids;
    }
    
    public Block GetBlockPixelv(Vector2 pixelPosition)
    {
        return Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()) ?
            null : Tiles.GetBlock(blocks.GetPixelv(pixelPosition));
    }
    
    public Wall GetWallPixelv(Vector2 pixelPosition)
    {
        return Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()) ?
            null : Tiles.GetWall(walls.GetPixelv(pixelPosition));
    }
    
    public Liquid GetLiquidPixelv(Vector2 pixelPosition)
    {
        return Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()) ?
            null : Tiles.GetLiquid(liquids.GetPixelv(pixelPosition));
    }
    
    public MasterBlock GetTopTilePixelv(Vector2 pixelPosition)
    {
        if (Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()))
            return null;

        Color blocksColour = blocks.GetPixelv(pixelPosition);
        if (!IsAir(blocksColour))
            return Tiles.GetBlock(blocksColour);
        Color liquidColour = liquids.GetPixelv(pixelPosition);
        if (!IsAir(liquidColour))
            return Tiles.GetLiquid(liquidColour);
        return Tiles.GetWall(walls.GetPixelv(pixelPosition));
    }

    private static bool IsAir(Color colour)
    {
        return Helper.EmptyColour.Equals(colour);
    }

    public bool SetBlockPixelv(Vector2 pixelPosition, Tiles.Blocks block)
    {
        if (Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()))
            return false;
        
        blocks.SetPixelv(pixelPosition, Tiles.GetBlockInstance(block).Colour);
        return true;
    }
    
    public bool SetWallPixelv(Vector2 pixelPosition, Tiles.Walls wall)
    {
        if (Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()))
            return false;
        
        walls.SetPixelv(pixelPosition, Tiles.GetWallInstance(wall).Colour);
        return true;
    }
    
    public bool SetLiquidPixelv(Vector2 pixelPosition, Tiles.Liquids liquid)
    {
        if (Helper.OutOfBounds(pixelPosition, terrain.GetWorldSize()))
            return false;
        
        liquids.SetPixelv(pixelPosition, Tiles.GetLiquidInstance(liquid).Colour);
        return true;
    }

    public void BlockSnapshot(Image dest, Vector2 startingPixel)
    {
        dest.BlitRect(blocks, new Rect2(startingPixel, dest.GetSize()), Vector2.Zero);
    }
    
    public void WallSnapshot(Image dest, Vector2 startingPixel)
    {
        dest.BlitRect(walls, new Rect2(startingPixel, dest.GetSize()), Vector2.Zero);
    }
    
    public void LiquidSnapshot(Image dest, Vector2 startingPixel)
    {
        dest.BlitRect(liquids, new Rect2(startingPixel, dest.GetSize()), Vector2.Zero);
    }
}
