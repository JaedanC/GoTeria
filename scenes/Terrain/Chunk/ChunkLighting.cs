using Godot;
using Godot.Collections;


public class ChunkLighting
{
    private enum ChunkLightingState
    {
        InBackground,
        NotCached,
        Cached
    }

    private readonly LightingEngine lightingEngine;
    private readonly TeriaFile imageCache;
    private readonly TeriaFile configFile;
    private readonly Dictionary<Vector2, ChunkLightingState> cache;
    private readonly Mutex cacheLock;


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

        // Load from disk what chunks have already been lit
        ReadCachedChunksConfig();
    }

    /* Light every chunk in the map so that it is cached. Makes for very smooth movement of the player as the light
    does not need to be recalculated. */
    public void BackgroundLightAChunk(Terrain terrain, ChunkLoader chunkLoader, LazyVolatileDictionary<Vector2, Chunk> loadedChunks)
    {
        Developer.AssertTrue(loadedChunks.IsLocked, "BackgroundLightAChunk() requires the lock.");

        // Light all the chunks
        Array<Vector2> lightingChunks = new Array<Vector2>();
        while (true)
        {
            Vector2? nextChunkPositionToLight = GetNextNotCachedChunkPosition();
            // No more chunks to light
            if (nextChunkPositionToLight == null)
                break;
            
            // Load these chunks first
            // TODO: Do this one at a time because loading all the chunks into the chunkPool is no better than loading
            // every block.
            Vector2 chunkPosition = (Vector2)nextChunkPositionToLight;
            foreach (Vector2 dependency in Chunk.GetDependencies(chunkPosition, terrain.GetWorldSizeInChunks()))
            {
                chunkLoader.BeginLoadingChunk(dependency, loadedChunks, true);
            }
            lightingChunks.Add(chunkPosition);
        }

        // Now begin lighting all the chunks
        foreach (Vector2 chunkPosition in lightingChunks)
        {
            chunkLoader.BeginLightingChunk(chunkPosition, loadedChunks, true);
        }

        // Finally, wait until they're all done
        foreach (Vector2 chunkPosition in lightingChunks)
        {
            chunkLoader.FinishLightingChunkForcefully(chunkPosition, loadedChunks);
        }
        
        // Clean up the chunks in the lazy section of the loadedChunks.
        // TODO: In future we may want this to be more specific to the chunks that this method instances.
        System.Collections.Generic.IDictionary<Vector2, Chunk> deletedChunks = loadedChunks.LazyClear();
        terrain.KillChunks(deletedChunks.Values);
    }

    /* Acts like a generator. Returns a chunk if it has not been cached. Marks the chunk as InBackground so that this
    method does not return the same chunk twice. */
    private Vector2? GetNextNotCachedChunkPosition()
    {
        try
        {
            cacheLock.Lock();
            foreach (Vector2 chunkPosition in cache.Keys)
            {
                // Ignore chunks that are cached or background loading already.
                if (cache[chunkPosition] != ChunkLightingState.NotCached)
                    continue;
                
                cache[chunkPosition] = ChunkLightingState.InBackground;
                return chunkPosition;
            }
            return null;
        }
        finally
        {
            cacheLock.Unlock();
        }
    }

    /* Reads from the lighting json file to know which chunks in the light.png have already been calculated. This
    means that a chunk does not need to recalculate its light between game runs. Chunks that that are in json file are
    marked as Cached. */
    private void ReadCachedChunksConfig()
    {
        File lightingConfigContents = configFile.ReadFile();
        if (lightingConfigContents == null)
        {
            // The file likely doesn't exist.
            // That's okay. No chunks are cached yet.
            return;
        }
        
        // Read the lighting.json file and marks chunks that are in here as Cached.
        JSONParseResult lightingConfig = JSON.Parse(lightingConfigContents.GetAsText());
        lightingConfigContents.Close(); // Don't forget to close the file so that we can use it later.
        Dictionary jsonResults = (Dictionary)lightingConfig.Result;
        Array cachedChunks = (Array)jsonResults["cache"];

        foreach (string vectorString in cachedChunks)
        {
            Vector2 cachedChunk = Helper.StringToVector2(vectorString);
            cache[cachedChunk] = ChunkLightingState.Cached;
        }
    }

    /* Returns the save lighting image likely called light.png. Can be used to initialise the LightingEngine's
    light image. */
    public Image GetSavedLighting()
    {
        return WorldFile.LoadImage(imageCache);
    }

    /* Assumes the parameter is the light image we wish to save. Also saves which chunks have had their chunk's light
     calculated. */
    public void SaveCacheToDisk(Image image)
    {
        // Save the image
        WorldFile.SaveImage(image, imageCache);
        GD.Print("ChunkLighting.SaveToDisk() Saved image to: " + imageCache.GetFinalFilePath());

        // Create a dictionary that will be turned into json. This contains the chunks that are loaded in the cache.
        Dictionary<string, Array<string>> toSaveDictionary = new Dictionary<string, Array<string>> {
            ["cache"] = new Array<string>()
        };
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
        
        // Save the dictionary to a json file.
        string json = JSON.Print(toSaveDictionary, "    ");
        File lightingFileContents = configFile.GetFile(File.ModeFlags.Write);
        lightingFileContents.StoreString(json);
        lightingFileContents.Close(); // Don't forget to close the file so that we can use it later.
        GD.Print("ChunkLighting.SaveToDisk() Saved config to: " + configFile.GetFinalFilePath());
    }

    /* Called in a thread. Continues to light the chunk if it isn't cached. Does nothing if it is cached. This means the
    light already exists inside the LightingEngine's light image. */
    public void CalculateLightOrUseCachedLight(Chunk chunk)
    {
        cacheLock.Lock();
        bool recalculate = !cache.ContainsKey(chunk.ChunkPosition) || cache[chunk.ChunkPosition] != ChunkLightingState.Cached;
        cacheLock.Unlock();

        if (!recalculate)
            return;
        
        // Don't lock this because this would be block multi-threaded lighting!
        // Start the lighting algorithm
        lightingEngine.LightChunk(chunk);

        cacheLock.Lock();
        cache[chunk.ChunkPosition] = ChunkLightingState.Cached;
        cacheLock.Unlock();
    }
}
