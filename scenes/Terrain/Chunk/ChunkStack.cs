using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class ChunkStack
{
    private readonly WorldImage worldImage;
    private readonly Image blocks;
    private readonly Image walls;
    private readonly Image liquids;
    private readonly Array<ImageTexture> textures;
    private readonly Vector2 chunkSize;
    private Vector2 chunkPosition;

    public ChunkStack(WorldImage worldImage, Vector2 chunkSize)
    {
        this.worldImage = worldImage;
        this.chunkSize = chunkSize;
        blocks = new Image();
        walls = new Image();
        liquids = new Image();
        blocks.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        walls.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        liquids.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        blocks.Lock();
        walls.Lock();
        liquids.Lock();
        textures = new Array<ImageTexture> {new ImageTexture(), new ImageTexture(), new ImageTexture()};
    }

    public void Initialise(Vector2 chunkPosition)
    {
        this.chunkPosition = chunkPosition;
    }

    public IEnumerable<ImageTexture> ComputeAndGetTextures()
    {
        worldImage.BlockSnapshot(blocks, chunkPosition * chunkSize);
        worldImage.WallSnapshot(walls, chunkPosition * chunkSize);
        worldImage.LiquidSnapshot(liquids, chunkPosition * chunkSize);
        textures[0].CreateFromImage(walls, 0);
        textures[1].CreateFromImage(blocks, 0);
        textures[2].CreateFromImage(liquids, 0);
        return textures;
    }
}
