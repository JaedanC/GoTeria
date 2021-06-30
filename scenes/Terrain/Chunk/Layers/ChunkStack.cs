using Godot;
using Godot.Collections;

public class ChunkStack
{
    private readonly ChunkLayer<Block> blocks;
    private readonly ChunkLayer<Wall> walls;
    private readonly Array<ImageTexture> textures;
    private Vector2 chunkSize;
    public Block[] Blocks => blocks.Blocks;
    public Wall[] Walls => walls.Blocks;

    public ChunkStack()
    {
        blocks = new ChunkLayer<Block>();
        walls = new ChunkLayer<Wall>();
        textures = new Array<ImageTexture> {new ImageTexture(), new ImageTexture()};
    }

    public void AllocateMemory(Vector2 chunkSize)
    {
        this.chunkSize = chunkSize;
        blocks.AllocateMemory(chunkSize);
        walls.AllocateMemory(chunkSize);
    }

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, Image worldBlocksImages, Image worldWallsImage)
    {
        this.chunkSize = chunkSize;
        blocks.Create(chunkPosition, chunkSize, worldBlocksImages);
        walls.Create(chunkPosition, chunkSize, worldWallsImage);
    }

    public Array<ImageTexture> ComputeAndGetTextures()
    {
        textures[0].CreateFromImage(walls.ChunkLayerImage, 0);
        textures[1].CreateFromImage(blocks.ChunkLayerImage, 0);
        return textures;
    }

    public IBlock GetTopIBlock(Vector2 blockPosition)
    {
        if (Helper.OutOfBounds(blockPosition, chunkSize))
        {
            return null;
        }
        int blockIndex = Chunk.BlockPositionToBlockIndex(chunkSize, blockPosition);
        if (blockIndex >= Blocks.Length)
        {
            GD.Print(Blocks.Length + " " + blockIndex);
            return null;
        }
        Block topBlock = Blocks[blockIndex];
        if (topBlock.IsSolid())
            return topBlock;
        return Walls[blockIndex];
    }

    public Image GetBlocksImage()
    {
        return blocks.ChunkLayerImage;
    }

    public Image GetWallsImage()
    {
        return walls.ChunkLayerImage;
    }
}
