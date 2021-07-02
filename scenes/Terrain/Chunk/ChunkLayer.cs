using Godot;


public class ChunkLayer
{
    public Image ChunkLayerImage { get; private set; }

    // This allocates the chunk's memory, and does so in this thread to improve performance
    // for the first couple times this needs to be run.
    public void AllocateMemory(Vector2 chunkSize)
    {
        Developer.AssertGreaterThan(chunkSize.x, 0, "Chunk width must be at least 1.");
        Developer.AssertGreaterThan(chunkSize.y, 0, "Chunk height must be at least 1.");
        
        ChunkLayerImage = new Image();
        ChunkLayerImage.Create((int)chunkSize.x, (int)chunkSize.y, false, Image.Format.Rgba8);
        ChunkLayerImage.Lock();
    }
}
