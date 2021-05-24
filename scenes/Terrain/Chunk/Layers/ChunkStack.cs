using Godot;
using Godot.Collections;
using System;

public class ChunkStack {
    private ChunkLayer<Block> _blocks;
    private ChunkLayer<Wall> _walls;
    private Array<ImageTexture> _textures;
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
        _blocks.AllocateMemory(chunkSize);
        _walls.AllocateMemory(chunkSize);
    }

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, Image worldBlocksImages, Image worldWallsImage)
    {
        _blocks.Create(chunkPosition, chunkSize, worldBlocksImages);
        _walls.Create(chunkPosition, chunkSize, worldWallsImage);
    }

    public Array<ImageTexture> ComputeAndGetTextures()
    {
        _textures[0].CreateFromImage(_walls.ChunkLayerImage, 0);
        _textures[1].CreateFromImage(_blocks.ChunkLayerImage, 0);
        return _textures;
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
