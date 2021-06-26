using Godot;
using Godot.Collections;
using System.Collections.Generic;


class MultithreadedChunkLoader
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

    private HashSet<Vector2> chunksLoading;
    private HashSet<Vector2> chunksLighting;

    public MultithreadedChunkLoader(Vector2 chunkBlockCount, ThreadPool threadPool, Image worldBlocksImage, Image worldWallsImage)
    {
        this.threadPool = threadPool;
        this.chunkBlockCount = chunkBlockCount;
        this.worldBlocksImage = worldBlocksImage;
        this.worldWallsImage = worldWallsImage;

        chunksLoading = new HashSet<Vector2>();
        chunksLighting = new HashSet<Vector2>();
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

        threadPool.SubmitTask(this, "ThreadedLoadChunk", chunkData, "loadingChunk", chunkPosition);
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

        threadPool.SubmitTask(this, "ThreadedLightChunk", chunkData, "lightingChunk", chunk.ChunkPosition);
    }

    public Vector2 ThreadedLoadChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        chunk.Create(chunkPosition, chunkBlockCount, worldBlocksImage, worldWallsImage);

        return chunkPosition;
    }

    public Vector2 ThreadedLightChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        chunk.ComputeLightingPass();

        return chunkPosition;
    }

    public void FinishLoadingChunkForcefully(Vector2 chunkPosition)
    {
        if (!chunksLoading.Contains(chunkPosition))
            return;

        threadPool.WaitForTaskSpecific(chunkPosition);
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

        threadPool.WaitForTaskSpecific(chunkPosition);
    }

    public Array<Chunk> GetFinishedLoadingChunks(Godot.Collections.Dictionary<Vector2, Chunk> loadedChunks)
    {
        Array<Chunk> finishedChunks = new Array<Chunk>();
        Array completedChunkTasks = (Godot.Collections.Array)threadPool.FetchFinishedTasksByTag("loadingChunk");
        foreach (Task completedChunkTask in completedChunkTasks)
        {
            Vector2 completedChunkPosition = (Vector2)completedChunkTask.GetResult();
            chunksLoading.Remove(completedChunkPosition);

            if (!loadedChunks.ContainsKey(completedChunkPosition))
                continue;

            Chunk completedChunk = loadedChunks[completedChunkPosition];
            completedChunk.LoadingDone = true;
            finishedChunks.Add(completedChunk);
        }

        return finishedChunks;
    }

    public Array<Chunk> GetFinishedLightingChunks(Godot.Collections.Dictionary<Vector2, Chunk> loadedChunks)
    {
        Array<Chunk> finishedChunks = new Array<Chunk>();
        Array completedChunkTasks = (Godot.Collections.Array)threadPool.FetchFinishedTasksByTag("lightingChunk");
        foreach (Task completedChunkTask in completedChunkTasks)
        {
            Vector2 completedChunkPosition = (Vector2)completedChunkTask.GetResult();
            chunksLighting.Remove(completedChunkPosition);

            if (!loadedChunks.ContainsKey(completedChunkPosition))
                continue;

            Chunk completedChunk = loadedChunks[completedChunkPosition];
            completedChunk.LightingDone = true;
            finishedChunks.Add(completedChunk);
        }

        return finishedChunks;
    }
}
