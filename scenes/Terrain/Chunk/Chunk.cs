using Godot;
using System.Collections.Generic;


public class Chunk : Node2D, IResettable
{
    public enum LoadingPhase
    {
        NeedsLoading,
        NeedsLighting,
        ReadyToDraw
    }

    private Terrain terrain;
    private ChunkLighting chunkLighting;
    private Vector2 chunkSize;
    private Vector2 blockPixelSize;
    private bool memoryAllocated;
    private ChunkStack chunkStack;
    public Vector2 ChunkPosition { get; private set; }
    private bool loadingDone;
    private bool lightingDone;

    public static IList<Vector2> GetDependencies(Vector2 chunkPosition, Vector2 worldSizeInChunks)
    {
        List<Vector2> legit = new List<Vector2>();
        List<Vector2> dependencies = new List<Vector2>() {
            new Vector2(chunkPosition.x,     chunkPosition.y),
            new Vector2(chunkPosition.x - 1, chunkPosition.y),
            new Vector2(chunkPosition.x - 1, chunkPosition.y - 1),
            new Vector2(chunkPosition.x,     chunkPosition.y - 1),
            new Vector2(chunkPosition.x + 1, chunkPosition.y - 1),
            new Vector2(chunkPosition.x + 1, chunkPosition.y),
            new Vector2(chunkPosition.x + 1, chunkPosition.y + 1),
            new Vector2(chunkPosition.x    , chunkPosition.y + 1),
            new Vector2(chunkPosition.x - 1, chunkPosition.y + 1),
        };

        foreach (Vector2 dependency in dependencies)
        {
            if (Helper.InBounds(dependency, worldSizeInChunks))
            {
                legit.Add(dependency);
            }
        }
        return legit;
    }

    public Chunk()
    {
        Name = "Chunk";
    }

    /* This function should only be run once. This initialises a chunk to have the memory it requires
    to be allocated on the heap. */
    public void AllocateMemory(params object[] memoryAllocationParameters)
    {
        Developer.AssertFalse(memoryAllocated, "Don't allocate memory twice for chunk.");
        WorldImage worldImage = (WorldImage)memoryAllocationParameters[0];
        chunkSize = (Vector2)memoryAllocationParameters[1];
        chunkStack = new ChunkStack(worldImage, chunkSize);
        memoryAllocated = true;
    }

    /* This is the method that is called when a chunk is reset before it is reused. */
    public void Initialise(object[] parameters)
    {
        this.ChunkPosition = (Vector2)parameters[0];
        this.blockPixelSize = (Vector2)parameters[1];
        this.chunkSize = (Vector2)parameters[2];
        this.terrain = (Terrain)parameters[3];
        this.chunkLighting = (ChunkLighting)parameters[4];
        this.lightingDone = false;
        this.loadingDone = false;
        this.Position = blockPixelSize * ChunkPosition * chunkSize;
    }

    public LoadingPhase GetLoadingPhase()
    {
        if (!loadingDone)
            return LoadingPhase.NeedsLoading;

        if (!lightingDone)
            return LoadingPhase.NeedsLighting;

        return LoadingPhase.ReadyToDraw;
    }

    public void Create(Vector2 chunkPosition, Vector2 chunkSize, WorldImage worldImage)
    {
        if (!memoryAllocated)
            AllocateMemory(worldImage, chunkSize);

        chunkStack.Initialise(chunkPosition);
        loadingDone = true;
    }

    public IList<Vector2> GetDependencies()
    {
        return GetDependencies(ChunkPosition, terrain.GetWorldSizeInChunks());
    }

    public override void _Draw()
    {
        // DrawCircle(Vector2.Zero, 2, Color.aquamarine);
        DrawChunk();
    }

    public void ComputeLightingPass()
    {
        // GD.Print("Computed Lighting for chunk: " + ChunkPosition);
        // chunkLighting.ComputeLightingPass();
        // terrain.LightingEngine.LightChunk(this);
        chunkLighting.CalculateLightOrUseCachedLight(this);
        lightingDone = true;
        // Update();
    }

    public void OnDeath()
    {
        // chunkLighting.SaveLight();
    }

    /* Draw the chunk to the screen using my special colour formula. This function
    Is run when a chunk is created however, we only want it to count as being
    run after all the blocks have been loaded.*/
    private void DrawChunk()
    {
        if (!loadingDone || !lightingDone)
            return;

        DrawSetTransform(Vector2.Zero, 0, blockPixelSize);
        foreach (ImageTexture texture in chunkStack.ComputeAndGetTextures())
        {
            DrawTexture(texture, Vector2.Zero);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
