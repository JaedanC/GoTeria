using Godot;
using Godot.Collections;
using System.Collections.Generic;


public class MultithreadedChunkLoader
{
    public enum LoadingPhase
    {
        NeedsLoading,
        NeedsLighting,
        ReadyToDraw
    }

    private ThreadPool threadPool;
    private Vector2 chunkBlockCount;
    private Image worldBlocksImage;
    private Image worldWallsImage;

    private ConcurrentSet<Vector2> chunksLoading;
    private ConcurrentSet<Vector2> chunksLighting;

    public MultithreadedChunkLoader(Vector2 chunkBlockCount, ThreadPool threadPool, Image worldBlocksImage, Image worldWallsImage)
    {
        this.threadPool = threadPool;
        this.chunkBlockCount = chunkBlockCount;
        this.worldBlocksImage = worldBlocksImage;
        this.worldWallsImage = worldWallsImage;

        chunksLoading = new ConcurrentSet<Vector2>();
        chunksLighting = new ConcurrentSet<Vector2>();
    }

    public void LightAllChunks(Terrain terrain, Vector2 worldSizeInChunks)
    {
        for (int i = 0; i < worldSizeInChunks.x; i++)
        for (int j = 0; j < worldSizeInChunks.y; j++)
        {
            // BeginLightingChunk(new Vector2(i, j));
        }

        for (int i = 0; i < worldSizeInChunks.x; i++)
        for (int j = 0; j < worldSizeInChunks.y; j++)
        {
            FinishLightingChunkForcefully(new Vector2(i, j));
        }
    }

    public LoadingPhase GetChunkPhase(Chunk chunk)
    {
        if (!chunk.LoadingDone)
            return LoadingPhase.NeedsLoading;

        if (!chunk.LightingDone)
            return LoadingPhase.NeedsLighting;

        return LoadingPhase.ReadyToDraw;
    }

    public void BeginLoadingChunk(Chunk chunk, Vector2 chunkPosition)
    {
        if (chunksLoading.Contains(chunkPosition) || GetChunkPhase(chunk) != LoadingPhase.NeedsLoading)
            return;

        // GD.Print("Loading: " + chunkPosition);

        chunksLoading.Add(chunkPosition);

        Array<object> chunkData = new Array<object>{
            chunkPosition,
            chunk
        };

        threadPool.SubmitTask(this, "ThreadedLoadChunk", "ThreadedLoadChunkCallback", chunkData, 0, "loadingChunk", chunkPosition);
    }

    public void BeginLightingChunk(Chunk chunk, Godot.Collections.Dictionary<Vector2, Chunk> loadedChunks)
    {
        if (chunksLighting.Contains(chunk.ChunkPosition) || GetChunkPhase(chunk) != LoadingPhase.NeedsLighting)
            return;
        chunksLighting.Add(chunk.ChunkPosition);

        // Check the dependencies are loading or loaded.
        Array<Vector2> dependencies = new Array<Vector2>() {
            new Vector2(chunk.ChunkPosition.x,     chunk.ChunkPosition.y),
            new Vector2(chunk.ChunkPosition.x - 1, chunk.ChunkPosition.y),
            new Vector2(chunk.ChunkPosition.x - 1, chunk.ChunkPosition.y - 1),
            new Vector2(chunk.ChunkPosition.x,     chunk.ChunkPosition.y - 1),
            new Vector2(chunk.ChunkPosition.x + 1, chunk.ChunkPosition.y - 1),
            new Vector2(chunk.ChunkPosition.x + 1, chunk.ChunkPosition.y),
            new Vector2(chunk.ChunkPosition.x + 1, chunk.ChunkPosition.y + 1),
            new Vector2(chunk.ChunkPosition.x    , chunk.ChunkPosition.y + 1),
            new Vector2(chunk.ChunkPosition.x - 1, chunk.ChunkPosition.y + 1),
        };

        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            // Ignore dependencies out of bounds
            Vector2 worldImageInChunks = worldBlocksImage.GetSize() / chunkBlockCount;
            if (Helper.OutOfBounds(chunkToLoadDependency, worldImageInChunks))
            {
                continue;
            }

            Developer.AssertTrue(loadedChunks.ContainsKey(chunkToLoadDependency));

            Chunk dependencyChunk = loadedChunks[chunkToLoadDependency];
            if (!chunksLoading.Contains(chunkToLoadDependency) && GetChunkPhase(chunk) == LoadingPhase.NeedsLoading)
            {
                BeginLoadingChunk(loadedChunks[chunkToLoadDependency], chunkToLoadDependency);
            }
        }

        Array<object> chunkData = new Array<object>{
            chunk.ChunkPosition,
            chunk
        };

        threadPool.SubmitTask(this, "ThreadedLightChunk", "ThreadedLightChunkCallback", chunkData, 0, "lightingChunk", chunk.ChunkPosition);
    }

    public Chunk ThreadedLoadChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        // OS.DelayMsec(2000);

        chunk.Create(chunkPosition, chunkBlockCount, worldBlocksImage, worldWallsImage);

        return chunk;
    }

    public void ThreadedLoadChunkCallback(Chunk chunk)
    {
        chunksLoading.Remove(chunk.ChunkPosition);
    }

    public Chunk ThreadedLightChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        chunk.ComputeLightingPass();

        return chunk;
    }

    public void ThreadedLightChunkCallback(Chunk chunk)
    {
        chunksLighting.Remove(chunk.ChunkPosition);
        chunk.Update();
        GD.Print("Finished Lighting chunk: " + chunk.ChunkPosition);
    }

    public void FinishLoadingChunkForcefully(Vector2 chunkPosition)
    {
        // if (!chunksLoading.Contains(chunkPosition))
            // return;

        // threadPool.WaitForTaskSpecific(chunkPosition);
        WaitForLoadingTask(chunkPosition);
    }

    public void FinishLightingChunkForcefully(Vector2 chunkPosition)
    {
        if (!chunksLighting.Contains(chunkPosition))
            return;

        Array<Vector2> dependencies = new Array<Vector2>() {
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

        foreach (Vector2 chunkToLoadDependency in dependencies)
        {
            if (chunksLoading.Contains(chunkToLoadDependency))
            {
                FinishLoadingChunkForcefully(chunkToLoadDependency);
            }
        }

        WaitForLightingTask(chunkPosition);
        // threadPool.WaitForTaskSpecific(chunkPosition);
    }

    public void WaitForLoadingTask(Vector2 chunkPosition)
    {
        while (true)
        {
            if (!chunksLoading.Contains(chunkPosition))
                return;
            OS.DelayMsec(2);
        }
    }

    public void WaitForLightingTask(Vector2 chunkPosition)
    {
        while (true)
        {
            if (!chunksLighting.Contains(chunkPosition))
                return;
            OS.DelayMsec(2);
        }
    }

    // public Array<Chunk> GetFinishedLoadingChunks(Godot.Collections.Dictionary<Vector2, Chunk> loadedChunks) //TODO: Check if remove parameter
    // {
    //     Array<Chunk> finishedChunks = new Array<Chunk>();
    //     List<Task> completedChunkTasks = threadPool.FetchFinishedTasksByTag("loadingChunk");
    //     foreach (Task completedChunkTask in completedChunkTasks)
    //     {
    //         Chunk completedChunk = (Chunk)completedChunkTask.GetResult();
    //         completedChunk.LoadingDone = true;
    //         chunksLoading.Remove(completedChunk.ChunkPosition);
    //         finishedChunks.Add(completedChunk);

    //         // Don't know if break. Shouldn't be required as the reference hasn't changed.
    //         // if (!loadedChunks.ContainsKey(completedChunkPosition))
    //         //     continue;
    //     }

    //     return finishedChunks;
    // }

    // public Array<Chunk> GetFinishedLightingChunks(Godot.Collections.Dictionary<Vector2, Chunk> loadedChunks) //TODO: Check if remove parameter
    // {
    //     Array<Chunk> finishedChunks = new Array<Chunk>();
    //     List<Task> completedChunkTasks = threadPool.FetchFinishedTasksByTag("lightingChunk");
    //     foreach (Task completedChunkTask in completedChunkTasks)
    //     {
    //         Chunk completedChunk = (Chunk)completedChunkTask.GetResult();
    //         completedChunk.LightingDone = true;
    //         finishedChunks.Add(completedChunk);
    //         chunksLighting.Remove(completedChunk.ChunkPosition);

    //         // Don't know if break. Shouldn't be required as the reference hasn't changed.
    //         // if (loadedChunks.ContainsKey(completedChunk.Position))
    //             // loadedChunks[completedChunk.Position] = completedChunk;
    //     }

    //     return finishedChunks;
    // }
}
