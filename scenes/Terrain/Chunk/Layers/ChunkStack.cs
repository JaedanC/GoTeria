using Godot;
using Godot.Collections;
using System;

public class ChunkStack
{
    private ChunkLayer<Block> _blocks;
    private ChunkLayer<Wall> _walls;
    private Array<ImageTexture> _textures;
    private Vector2 _chunkSize;
    public Block[] Blocks { get { return _blocks.Blocks; } }
    public Wall[] Walls { get { return _walls.Blocks; } }

    public ChunkStack()
    {
        _blocks = new ChunkLayer<Block>();
        _walls = new ChunkLayer<Wall>();
        _textures = new Array<ImageTexture>();
        _textures.Add(new ImageTexture());
        _textures.Add(new ImageTexture());
    }

    public void AllocateMemory(Vector2 chunkSize)
    {
        _chunkSize = chunkSize;
        _blocks.AllocateMemory(chunkSize);
        _walls.AllocateMemory(chunkSize);
    }

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, Image worldBlocksImages, Image worldWallsImage)
    {
        _chunkSize = chunkSize;
        _blocks.Create(chunkPosition, chunkSize, worldBlocksImages);
        _walls.Create(chunkPosition, chunkSize, worldWallsImage);
    }

    public Array<ImageTexture> ComputeAndGetTextures()
    {
        _textures[0].CreateFromImage(_walls.ChunkLayerImage, 0);
        _textures[1].CreateFromImage(_blocks.ChunkLayerImage, 0);
        return _textures;
    }

    public IBlock GetTopIBlock(Vector2 blockPosition)
    {
        if (Helper.OutOfBounds(blockPosition, _chunkSize))
        {
            return null;
        }
        int blockIndex = Chunk.BlockPositionToBlockIndex(_chunkSize, blockPosition);
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
        return _blocks.ChunkLayerImage;
    }

    public Image GetWallsImage()
    {
        return _walls.ChunkLayerImage;
    }
}
