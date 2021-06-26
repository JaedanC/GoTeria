using Godot;


public class ChunkLighting
{
    private Terrain terrain;
    private LightingEngine lightingEngine;
    private Chunk chunk;
    private WorldFile worldFile;

    public ChunkLighting(Chunk chunk, Terrain terrain, WorldFile worldFile)
    {
        this.chunk = chunk;
        this.terrain = terrain;
        this.lightingEngine = terrain.LightingEngine;
        this.worldFile = WorldSpawn.ActiveWorldSpawn.GetWorldFile();
    }

    public void ComputeLightingPass()
    {
        // UsePrecalculatedIfExists();
        CalculateLight();
    }

    private void CalculateLight()
    {
        lightingEngine.LightChunk(chunk);
    }

    // private void UsePrecalculatedIfExists()
    // {
    //     Image found = worldFile.LoadImage("SavedWorld", chunk.ChunkPosition + ".png");

    //     if (found == null)
    //     {
    //         CalculateLight();
    //         SaveLight();
    //     }
    //     else
    //     {
    //         Rect2 chunkArea = new Rect2(Vector2.Zero, terrain.ChunkBlockCount);
    //         terrain.LightingEngine.WorldLightImage.GetImage().BlitRect(found, chunkArea, chunk.ChunkPosition * terrain.ChunkBlockCount);
    //     }
    // }

    // public void SaveLight()
    // {
    //     Rect2 chunkArea = new Rect2(chunk.ChunkPosition * terrain.ChunkBlockCount, terrain.ChunkBlockCount);
    //     Image result = terrain.LightingEngine.WorldLightImage.GetImage().GetRect(chunkArea);
    //     worldFile.SaveImage(result, "SavedWorld", chunk.ChunkPosition + ".png");
    // }
}
