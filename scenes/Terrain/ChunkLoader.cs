using Godot;
using Godot.Collections;
using System.Collections.Generic;


public class ChunkLoader
{
    private readonly Terrain terrain;
    private readonly ThreadPool threadPool;

    private readonly ConcurrentSet<Vector2> chunksLoading;
    private readonly ConcurrentSet<Vector2> chunksLighting;

    public ChunkLoader(Terrain terrain, ThreadPool threadPool)
    {
        this.terrain = terrain;
        this.threadPool = threadPool;

        chunksLoading = new ConcurrentSet<Vector2>();
        chunksLighting = new ConcurrentSet<Vector2>();
    }

    /* Begins loading a chunk by passing the chunk onto the thread pool. The chunk is ignored if it is already loaded
    or is currently loading. This function requires the loadedChunks LazyVolatileDictionary to be locked and will throw
    an error if not. Returns true if the chunk was placed in the thread pool. */
    public bool BeginLoadingChunk(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunks, bool lazy)
    {
        Developer.AssertTrue(Helper.InBounds(chunkPosition, terrain.GetWorldSizeInChunks()), "BeginLoadingChunk(): " + chunkPosition + " was out of bounds.");
        Developer.AssertTrue(loadedChunks.IsLocked, "BeginLoadingChunk() requires the lock.");
        
        // The chunk is already loading
        if (chunksLoading.Contains(chunkPosition))
            return false;
        
        // Retrieve the chunk
        Chunk chunk = terrain.GetOrInstanceChunkInto(chunkPosition, loadedChunks, lazy);
        Developer.AssertNotNull(chunk, "Chunk: " + chunkPosition + " is null.");
        
        // The chunk has already loaded
        if (chunk.GetLoadingPhase() != Chunk.LoadingPhase.NeedsLoading)
            return false;
        
        // Let's begin loading the chunk
        chunksLoading.Add(chunkPosition);
        Array<object> chunkData = new Array<object>{ chunkPosition, chunk };
        threadPool.SubmitTask(this, "ThreadedLoadChunk", chunkData, 0, "loadingChunk", chunkPosition);
        return true;
    }

    /* Begins lighting a chunk by passing the chunk onto the thread pool. The chunk is ignored if it is already lit
    or is currently lighting. This function requires the loadedChunks LazyVolatileDictionary to be locked and will throw
    an error if not. */
    public void BeginLightingChunk(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunks, bool lazy)
    {
        Developer.AssertTrue(Helper.InBounds(chunkPosition, terrain.GetWorldSizeInChunks()), "BeginLightingChunk(): " + chunkPosition + " was out of bounds.");
        Developer.AssertTrue(loadedChunks.IsLocked, "BeginLightingChunk() requires the lock.");

        // The chunk is already lighting
        if (chunksLighting.Contains(chunkPosition))
            return;
        
        // Retrieve the chunk
        Chunk chunk = terrain.GetOrInstanceChunkInto(chunkPosition, loadedChunks, lazy);
        Developer.AssertNotNull(chunk, "Chunk: " + chunkPosition + " is null.");

        // Check the loading phase of the chunk. If the chunk needs to be loaded, that's okay, this is checked later as
        // GetDependencies also returns the chunkPosition of the chunk itself and we force those to load first.
        if (chunk.GetLoadingPhase() == Chunk.LoadingPhase.ReadyToDraw)
            return;
        
        // Let's begin lighting the chunk
        chunksLighting.Add(chunkPosition);

        // Check that the dependencies are loading or loaded.
        IList<Vector2> dependencies = chunk.GetDependencies();
        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            BeginLoadingChunk(chunkToLoadDependency, loadedChunks, lazy);
            // bool neededLoading = BeginLoadingChunk(chunkToLoadDependency, loadedChunksConcurrent, lazy);
            // if (neededLoading)
            // {
            //     GD.Print("Loading chunk: " + chunkToLoadDependency + " for light: " + chunkPosition);
            // }
        }

        // Now force load the chunks that are around us. This avoids nasty race conditions when chunks light and don't
        // have the ability to read the blocks around them because they aren't loaded yet. Yes, loading chunks is
        // typically faster than lighting, but we cannot guarantee this order of execution. It's okay if chunks that
        // aren't loading are forced to load.
        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            FinishLoadingChunkForcefully(chunkToLoadDependency, loadedChunks);
        }

        // Finally we can start the task that lights this chunk. We release the lock for this call in the event that the
        // thread pool is in single threaded mode. Lighting requires the lock so we release it for that reason.
        Array<object> chunkData = new Array<object>{ chunkPosition, chunk };
        loadedChunks.Unlock();
        threadPool.SubmitTask(this, "ThreadedLightChunk", chunkData, 0, "lightingChunk", chunkPosition);
        loadedChunks.Lock();
    }
    
    /* Called by the thread pool. Loads a chunk with its block information. */
    public Chunk ThreadedLoadChunk(Array<object> data)
    {
        // Retrieve the real parameters
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        // GD.Print("ThreadPool(): Started loading chunk: " + chunkPosition);
        chunk.Create(chunkPosition, terrain.ChunkBlockCount, terrain.WorldBlocksImage, terrain.WorldWallsImage);
        chunksLoading.Remove(chunk.ChunkPosition);
        // GD.Print("ThreadPool(): Finished loading chunk: " + chunkPosition);
        return chunk;
    }
    
    /* Called by the thread pool. Propagates light into the chunk. */
    public Chunk ThreadedLightChunk(Array<object> data)
    {
        // Retrieve the real parameters
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        // GD.Print("ThreadPool(): Started lighting chunk: " + chunkPosition);
        chunk.ComputeLightingPass();
        // Catches a warning exception when the game closes during the lighting execution on a thread.
        try
        {
            chunk.Update();
        }
        catch (System.ObjectDisposedException)
        {
           GD.Print("ThreadPool(): Lighting chunk disposed: " + chunkPosition);
        }
        chunksLighting.Remove(chunkPosition);
        // GD.Print("ThreadPool(): Finished lighting chunk: " + chunkPosition);
        return chunk;
    }

    /* Finishes loading the chunk at the chunk position if it is in thread pool. Blocks until the chunk is done loading.
    Does nothing otherwise. This function requires the loadedChunks LazyVolatileDictionary to be locked and will throw
    an error if not. */
    public void FinishLoadingChunkForcefully(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunks)
    {
        Developer.AssertTrue(loadedChunks.IsLocked, "FinishLoadingChunkForcefully() requires the lock.");
        
        // The chunk wasn't loading
        if (!chunksLoading.Contains(chunkPosition))
            return;

        // Blocks until the chunk is done. We unlock so that things on the thread pool can continue in the meantime.
        // GD.Print("Force loading chunk: " + chunkPosition);
        loadedChunks.Unlock();
        while (true)
        {
            if (!chunksLoading.Contains(chunkPosition))
                break;
            OS.DelayMsec(2);
        }
        loadedChunks.Lock();
    }
    
    /* Finishes lighting the chunk at the chunk position if it is in thread pool. Blocks until the chunk is done
    lighting. Does nothing otherwise. This function requires the loadedChunks LazyVolatileDictionary to be locked and
    will throw an error if not. */
    public void FinishLightingChunkForcefully(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunks)
    {
        Developer.AssertTrue(loadedChunks.IsLocked, "FinishLightingChunkForcefully() requires the lock.");
        
        // The chunk wasn't lighting
        if (!chunksLighting.Contains(chunkPosition))
            return;
        
        // We consider this chunk done lighting when the dependencies are done loading also.
        foreach (Vector2 dependency in Chunk.GetDependencies(chunkPosition, terrain.GetWorldSizeInChunks()))
        {
            FinishLoadingChunkForcefully(dependency, loadedChunks);
        }
        // Blocks until the chunk is done. We unlock so that things on the thread pool can continue in the meantime.
        // GD.Print("Force lighting chunk: " + chunkPosition);
        loadedChunks.Unlock();
        while (true)
        {
            if (!chunksLighting.Contains(chunkPosition))
                break;
            OS.DelayMsec(2);
        }
        loadedChunks.Lock();
    }
}
