using Godot;


public class ChunkLayer<T> where T : IBlock, new()
{
    private Vector2 chunkSize;
    public T[] Blocks { get; private set; }

    public Image ChunkLayerImage { get; private set; }

    private bool allocated = false;

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, Image worldLayerImage)
    {
        ChunkLayerImage.Fill(Colors.Red);
        ChunkLayerImage.BlitRect(worldLayerImage, new Rect2(chunkPosition * chunkSize, chunkSize), Vector2.Zero);

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
            if (Blocks[blockIndex] == null)
                Blocks[blockIndex] = new T();

            Blocks[blockIndex].Id = (int)pixel.a;
            Blocks[blockIndex].Colour = pixel;
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

        this.chunkSize = chunkSize;
        Blocks = new T[(int)(chunkSize.x * chunkSize.y)];
        ChunkLayerImage = new Image();
        ChunkLayerImage.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        ChunkLayerImage.Lock();
        allocated = true;
    }
}
