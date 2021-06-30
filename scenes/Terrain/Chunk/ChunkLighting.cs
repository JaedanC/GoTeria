using Godot;
using Godot.Collections;
using System;


public class ChunkLighting
{
    private enum ChunkLightingState
    {
        InBackground,
        NotCached,
        Cached
    }

    private LightingEngine lightingEngine;
    private TeriaFile imageCache;
    private TeriaFile configFile;
    private Dictionary<Vector2, ChunkLightingState> cache;

    private Mutex cacheLock;


    public ChunkLighting(LightingEngine lightingEngine, TeriaFile imageCache, TeriaFile configFile, Vector2 worldSizeInChunks)
    {
        this.lightingEngine = lightingEngine;
        this.imageCache = imageCache;
        this.configFile = configFile;

        cache = new Dictionary<Vector2, ChunkLightingState>();
        cacheLock = new Mutex();

        // Initialise all the chunks to not be cached.
        for (int i = 0; i < worldSizeInChunks.x; i += 1)
        for (int j = 0; j < worldSizeInChunks.y; j += 1)
        {
            cache[new Vector2(i, j)] = ChunkLightingState.NotCached;
        }

        // Change that fact ^ here
        ReadCachedChunksConfig();
    }

    public void BackgroundLightAChunk(Terrain terrain, MultithreadedChunkLoader chunkLoader, LazyVolatileDictionary<Vector2, Chunk> loadedChunks, ObjectPool<Chunk> chunkPool)
    {
        Developer.AssertTrue(loadedChunks.IsLocked, "BackgroundLightAChunk() requires the lock. ");

        Godot.Collections.Array<Vector2> lightingChunks = new Godot.Collections.Array<Vector2>();

        // Light all the chunks
        while (true)
        {
            Vector2? nextChunkPositionToLight = GetNextNotCachedChunkPosition();
            if (nextChunkPositionToLight == null)
            {
                break;
            }
            Vector2 chunkPosition = (Vector2)nextChunkPositionToLight;
            foreach (Vector2 dependency in Chunk.GetDependencies(chunkPosition, terrain.GetWorldSizeInChunks()))
            {
                chunkLoader.BeginLoadingChunk(dependency, loadedChunks, true);
            }
            lightingChunks.Add(chunkPosition);
        }

        // Now wait until they're all done
        foreach (Vector2 chunkPosition in lightingChunks)
        {
            chunkLoader.BeginLightingChunk(chunkPosition, loadedChunks, true);
            chunkLoader.FinishLightingChunkForcefully(chunkPosition, loadedChunks);
        }


        System.Collections.Generic.IDictionary<Vector2, Chunk> deletedChunks = loadedChunks.LazyClear();
        foreach (Chunk chunk in deletedChunks.Values)
        {
            chunkPool.Die(chunk);
            terrain.RemoveChild(chunk);
        }
    }

    private Vector2? GetNextNotCachedChunkPosition()
    {
        try
        {
            cacheLock.Lock();
            foreach (Vector2 chunkPosition in cache.Keys)
            {
                if (cache[chunkPosition] == ChunkLightingState.NotCached)
                {
                    cache[chunkPosition] = ChunkLightingState.InBackground;
                    return chunkPosition;
                }
            }
            return null;
        }
        finally
        {
            cacheLock.Unlock();
        }
    }

    private void ReadCachedChunksConfig()
    {
        // Find out what chunk's lighting were cached.
        File lightingConfigContents = configFile.ReadFile();
        if (lightingConfigContents == null)
        {
            // The file likely doesn't exist yet
            return;
        }
        JSONParseResult lightingConfig = JSON.Parse(lightingConfigContents.GetAsText());
        lightingConfigContents.Close();
        Dictionary jsonResults = (Dictionary)lightingConfig.Result;
        Godot.Collections.Array cachedChunks = (Godot.Collections.Array)jsonResults["cache"];

        foreach (String vectorString in cachedChunks)
        {
            Vector2 cachedChunk = Helper.StringToVector2(vectorString);
            cache[cachedChunk] = ChunkLightingState.Cached;
        }
    }

    public Image GetSavedLighting()
    {
        return WorldFile.LoadImage(imageCache);
    }

    public void SaveToDisk(Image image)
    {
        // Save the image
        WorldFile.SaveImage(image, imageCache);
        GD.Print("ChunkLighting.SaveToDisk() Saved image to: " + imageCache.GetFinalFilePath());

        // Save the chunks that are loaded in the cache
        var toSaveDictionary = new Dictionary<String, Godot.Collections.Array<String>>();
        toSaveDictionary["cache"] = new Godot.Collections.Array<String>();

        cacheLock.Lock();
        foreach (Vector2 chunkPosition in cache.Keys)
        {
            ChunkLightingState state = cache[chunkPosition];
            if (state == ChunkLightingState.Cached)
            {
                toSaveDictionary["cache"].Add("" + chunkPosition);
            }
        }
        cacheLock.Unlock();

        String json = JSON.Print(toSaveDictionary, "    ");
        File lightingFileContents = configFile.GetFile(File.ModeFlags.Write);
        lightingFileContents.StoreString(json);
        lightingFileContents.Close();
        GD.Print("ChunkLighting.SaveToDisk() Saved config to: " + configFile.GetFinalFilePath());
    }

    public void CalculateLightOrUseCachedLight(Chunk chunk)
    {
        cacheLock.Lock();
        bool recalculate = !cache.ContainsKey(chunk.ChunkPosition) || cache[chunk.ChunkPosition] == ChunkLightingState.NotCached;
        cacheLock.Unlock();

        if (recalculate)
        {
            // Don't lock this because this would be block multithreaded lighting!
            lightingEngine.LightChunk(chunk);

            cacheLock.Lock();
            cache[chunk.ChunkPosition] = ChunkLightingState.Cached;
            cacheLock.Unlock();
        }
    }
}
