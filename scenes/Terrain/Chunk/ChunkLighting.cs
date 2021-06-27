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


    public ChunkLighting(LightingEngine lightingEngine, TeriaFile imageCache, TeriaFile configFile)
    {
        this.lightingEngine = lightingEngine;
        this.imageCache = imageCache;
        this.configFile = configFile;

        cache = new Dictionary<Vector2, ChunkLightingState>();
        ReadCachedChunksConfig();
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

        foreach (Vector2 chunkPosition in cache.Keys)
        {
            ChunkLightingState state = cache[chunkPosition];

            if (state == ChunkLightingState.Cached)
            {
                toSaveDictionary["cache"].Add("" + chunkPosition);
            }
        }

        String json = JSON.Print(toSaveDictionary, "    ");

        File lightingFileContents = configFile.GetFile(File.ModeFlags.Write);
        lightingFileContents.StoreString(json);
        lightingFileContents.Close();
        GD.Print("ChunkLighting.SaveToDisk() Saved config to: " + configFile.GetFinalFilePath());
    }

    public void CalculateLightOrUseCachedLight(Chunk chunk)
    {
        if (!cache.ContainsKey(chunk.ChunkPosition) ||
            cache[chunk.ChunkPosition] == ChunkLightingState.NotCached)
        {
            lightingEngine.LightChunk(chunk);
            cache[chunk.ChunkPosition] = ChunkLightingState.Cached;
        }
    }
}
