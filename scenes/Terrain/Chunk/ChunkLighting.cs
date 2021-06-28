using Godot;
using Godot.Collections;
using System;


public class ChunkLighting
{
    private enum ChunkLightingState
    {
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

    public void BackgroundLightAChunk(Terrain terrain, MultithreadedChunkLoader chunkLoader)
    {
        Vector2? nextChunkPositionToLight = GetNextNotCachedChunkPosition();
        if (nextChunkPositionToLight == null)
        {
            return;
        }
        Vector2 chunkPosition = (Vector2)nextChunkPositionToLight;

        // This will act as out loadedChunks Dictionary
        var backgroundloadedChunks = new Dictionary<Vector2, Chunk>();

        // Add the chunk we want to give light to
        Chunk chunkToLight = terrain.InstanceChunk(chunkPosition);
        backgroundloadedChunks[chunkPosition] = chunkToLight;

        // Add the chunk's dependencies
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

        foreach (Vector2 dependencyChunkPosition in dependencies)
        {
            backgroundloadedChunks[dependencyChunkPosition] = terrain.InstanceChunk(dependencyChunkPosition);
        }

        // Requires fresh eyes because chunks that can't be seen by the player are unloaded. This will
        // need to be changed in the future because I'll need a way to support bullets and other things
        // flying around! They will need to keep chunks loaded. A new way to keep chunks from being
        // unloaded needs to be devised.
        chunkLoader.BeginLightingChunk(chunkToLight, backgroundloadedChunks);
    }

    private Vector2? GetNextNotCachedChunkPosition()
    {
        foreach (Vector2 chunkPosition in cache.Keys)
        {
            if (cache[chunkPosition] == ChunkLightingState.NotCached)
            {
                return chunkPosition;
            }
        }
        return null;
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

        // Save the chunks that are loaded or in the
        var toSaveDictionary = new Dictionary<String, Array<String>>();
        toSaveDictionary["cache"] = new Array<String>();

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
            // Can't Resize Pool Vector if locked! Mutexing this does not fix the issue, but
            // making the ThreadPool single threaded does.
            lightingEngine.LightChunk(chunk);

            cacheLock.Lock();
            cache[chunk.ChunkPosition] = ChunkLightingState.Cached;
            cacheLock.Unlock();
        }
    }
}
