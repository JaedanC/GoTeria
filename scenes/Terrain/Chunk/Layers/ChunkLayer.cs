using Godot;
using System;
using System.Diagnostics;

public class ChunkLayer<T> where T : IBlock, new()
{
    private T[] _blocks;
    private Image _image;
    private Vector2 _chunkSize;
    public T[] Blocks { get { return _blocks; } }
    public Image ChunkLayerImage { get { return _image; } }
    private bool allocated = false;

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, Image worldLayerImage)
    {
        _image.Fill(Colors.Red);
        _image.BlitRect(worldLayerImage, new Rect2(chunkPosition * chunkSize, chunkSize), Vector2.Zero);
        
        for (int j = 0; j < chunkSize.y; j++)
        for (int i = 0; i < chunkSize.x; i++)
        {
            Vector2 blockPosition = new Vector2(i, j);
            Vector2 worldBlockPosition = chunkPosition * chunkSize + blockPosition;

            // Grab the colour for the pixel from the world image. If the pixel
            // goes out of bounds then just draw Red. This happens when the image is
            // not a multiple of the chunk size.
            Color pixel;
            if (worldBlockPosition.x < 0 || worldBlockPosition.y < 0 || 
                worldBlockPosition.x >= worldLayerImage.GetWidth() ||
                worldBlockPosition.y >= worldLayerImage.GetHeight())
            {
                pixel = Colors.Red;
            }
            else
                pixel = worldLayerImage.GetPixelv(worldBlockPosition);

            int blockIndex = Chunk.BlockPositionToBlockIndex(chunkSize, blockPosition);
            if (_blocks[blockIndex] == null)
                _blocks[blockIndex] = new T();

            _blocks[blockIndex].Id = (int)pixel.a;
            _blocks[blockIndex].Colour = pixel;
        }
    }

    // This allocates the chunk's memory, and does so in this thread to improve performance
    // for the first couple times this needs to be run.
    public void AllocateMemory(Vector2 chunkSize)
    {
        if (chunkSize.x == 0)
        {
            GD.Print("What?");
        }

        _chunkSize = chunkSize;
        _blocks = new T[(int)(chunkSize.x * chunkSize.y)];
        _image = new Image();
        _image.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        _image.Lock();
        allocated = true;
    }
}
