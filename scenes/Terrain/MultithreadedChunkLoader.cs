using Godot;
using Godot.Collections;
using System.Collections.Generic;


public class MultithreadedChunkLoader
{
    private Terrain terrain;
    private ThreadPool threadPool;
    bool singleThreadedThreadPool;

    private ConcurrentSet<Vector2> chunksLoading;
    private ConcurrentSet<Vector2> chunksLighting;

    public MultithreadedChunkLoader(Terrain terrain, ThreadPool threadPool, bool singleThreadedThreadPool)
    {
        this.terrain = terrain;
        this.threadPool = threadPool;
        this.singleThreadedThreadPool = singleThreadedThreadPool;

        chunksLoading = new ConcurrentSet<Vector2>();
        chunksLighting = new ConcurrentSet<Vector2>();
    }

    public void LightAllChunks(Terrain terrain, Vector2 worldSizeInChunks)
    {
        Developer.Fail("Unused");

        for (int i = 0; i < worldSizeInChunks.x; i++)
        for (int j = 0; j < worldSizeInChunks.y; j++)
        {
            // BeginLightingChunk(new Vector2(i, j));
        }

        for (int i = 0; i < worldSizeInChunks.x; i++)
        for (int j = 0; j < worldSizeInChunks.y; j++)
        {
            // FinishLightingChunkForcefully(new Vector2(i, j));
        }
    }

    /* Starts to load a chunk if it needs to be loaded and isn't already loading. */
    public bool BeginLoadingChunk(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent, bool lazy)
    {
        Developer.AssertTrue(Helper.InBounds(chunkPosition, terrain.GetWorldSizeInChunks()), "BeginLoadingChunk(): " + chunkPosition + " was out of bounds.");
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "BeginLoadingChunk() requires the lock. ");

        if (chunksLoading.Contains(chunkPosition))
            return false;

        Chunk chunk = terrain.GetOrInstanceChunkInto(chunkPosition, loadedChunksConcurrent, lazy);
        if (chunk.GetLoadingPhase() != Chunk.LoadingPhase.NeedsLoading)
            return false;

        chunksLoading.Add(chunkPosition);
        GD.Print("Loading Chunk: " + chunkPosition);

        Array<object> chunkData = new Array<object>{
            chunkPosition,
            chunk
        };

        threadPool.SubmitTask(this, "ThreadedLoadChunk", chunkData, 0, "loadingChunk", chunk.ChunkPosition);
        return true;
    }

    /* This method requires the lock, but it also requires that the lock is released at some point on another thread. This is
    because will spawn a thread a ask for the lock a second time. If the original function caller has not released the lock, a
    deadlock occurs. */
    public void BeginLightingChunk(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent, bool lazy)
    {
        Developer.AssertTrue(Helper.InBounds(chunkPosition, terrain.GetWorldSizeInChunks()), "BeginLightingChunk(): " + chunkPosition + " was out of bounds.");
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "BeginLightingChunk() requires the lock. ");

        // The chunk is already lighting
        if (chunksLighting.Contains(chunkPosition))
            return;
        
        Chunk chunk = terrain.GetOrInstanceChunkInto(chunkPosition, loadedChunksConcurrent, lazy);
        Developer.AssertNotNull(chunk, "Chunk: " + chunkPosition + " is null.");

        // Check the loading phase of the chunk. If the chunk is already complete, then we'll do nothing
        // If the chunk needs loading, that's okay, this is checked later as GetDependencies also returns
        // the chunk itself and we force those to load first.
        if (chunk.GetLoadingPhase() == Chunk.LoadingPhase.ReadyToDraw)
            return;
        
        // Add this chunk to the set, marking it as lighting
        chunksLighting.Add(chunkPosition);
        GD.Print("Lighting chunk: " + chunkPosition);

        // Check the dependencies are loading or loaded.
        IList<Vector2> dependencies = chunk.GetDependencies();
        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            bool neededLoading = BeginLoadingChunk(chunkToLoadDependency, loadedChunksConcurrent, lazy);
            if (neededLoading)
            {
                GD.Print("Loading chunk: " + chunkToLoadDependency + " for light: " + chunkPosition);
            }
        }

        // Now force load the chunks that need loading still. This avoids nasty race conditions
        // It's okay if chunks that aren't loading are in here
        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            FinishLoadingChunkForcefully(chunkToLoadDependency, loadedChunksConcurrent);
        }

        // Finally we can start the task that lights this chunk.
        Array<object> chunkData = new Array<object>{ chunkPosition, chunk };

        // If the thread above won't release lock (single threaded or no Unlock call) then unlock for us so the
        // thread can do it
        if (singleThreadedThreadPool)
        {
            loadedChunksConcurrent.Unlock();
            threadPool.SubmitTask(this, "ThreadedLightChunk", chunkData, 0, "lightingChunk", chunkPosition);
            loadedChunksConcurrent.Lock();
        }
        else
        {
            threadPool.SubmitTask(this, "ThreadedLightChunk", chunkData, 0, "lightingChunk", chunkPosition);
        }
    }

    public Chunk ThreadedLoadChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        GD.Print("ThreadPool(): Started loading chunk: " + chunkPosition);

        chunk.Create(chunkPosition, terrain.ChunkBlockCount, terrain.WorldBlocksImage, terrain.WorldWallsImage);
        chunksLoading.Remove(chunk.ChunkPosition);

        GD.Print("ThreadPool(): Finished loading chunk: " + chunkPosition);

        return chunk;
    }

    public Chunk ThreadedLightChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        GD.Print("ThreadPool(): Started lighting chunk: " + chunkPosition);

        chunk.ComputeLightingPass();

        try
        {
            chunk.Update();
            chunksLighting.Remove(chunk.ChunkPosition);
            GD.Print("ThreadPool(): Finished lighting chunk: " + chunkPosition);
        }
        catch (System.ObjectDisposedException)
        {
           GD.Print("ThreadPool(): Lighting chunk disposed: " + chunkPosition);
        }

        return chunk;
    }

    // Blocks until this chunk position isn't in the set anymore
    public void FinishLoadingChunkForcefully(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent)
    {
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "FinishLoadingChunkForcefully() requires the lock. ");
        loadedChunksConcurrent.Unlock();
        while (true)
        {
            if (!chunksLoading.Contains(chunkPosition))
                break;
            OS.DelayMsec(2);
        }
        loadedChunksConcurrent.Lock();
    }
    
    /* This function blocks while acquiring the lock yet lighting requires the lock! */
    public void FinishLightingChunkForcefully(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent)
    {
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "FinishLightingChunkForcefully() requires the lock. ");

        foreach (Vector2 dependency in Chunk.GetDependencies(chunkPosition, terrain.GetWorldSizeInChunks()))
        {
            FinishLoadingChunkForcefully(dependency, loadedChunksConcurrent);
        }
        loadedChunksConcurrent.Unlock();
        while (true)
        {
            if (!chunksLighting.Contains(chunkPosition))
                break;
            OS.DelayMsec(2);
        }
        loadedChunksConcurrent.Lock();
    }
}
