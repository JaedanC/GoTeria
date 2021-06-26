using Godot;
using Godot.Collections;
using System;

public class ChunkLighting
{
    private Terrain terrain;
    private LightingEngine lightingEngine;
    private Chunk chunk;
    private WorldFile worldFile;

    public ChunkLighting(Chunk chunk, Terrain terrain)
    {
        this.chunk = chunk;
        this.terrain = terrain;
        this.lightingEngine = terrain.LightingEngine;
        this.worldFile = new WorldFile("SavedWorld");
    }

    public void ComputeLightingPass()
    {
        // WorldChunkLighting.Instance.LightChunk(chunk);
        UsePrecalculatedIfExists();
        // ChunkLightBFS();
        lightingEngine.SetUpdateShader(true);
    }

    private void ChunkLightBFS()
    {
        QueueSet<LightingEngine.LightBFSNode> lightQueue = new QueueSet<LightingEngine.LightBFSNode>();

        Vector2 chunkBlockCount = terrain.ChunkBlockCount;
        Vector2 topLeftPixel = chunk.ChunkPosition * chunkBlockCount;

        // GD.Print("Chunk: ", topLeftPixel, chunkBlockCount);
        for (int i = (int)topLeftPixel.x; i < topLeftPixel.x + chunkBlockCount.x; i++)
        for (int j = (int)topLeftPixel.y; j < topLeftPixel.y + chunkBlockCount.y; j++)
        {
            Vector2 position = new Vector2(i, j);
            if (Helper.OutOfBounds(position, terrain.GetWorldSize()))
                continue;

            Color sourceColour = terrain.LightingEngine.WorldLightSources.GetPixelv(position);
            if (sourceColour == Colors.White)
            {
                lightQueue.Enqueue(new LightingEngine.LightBFSNode(position, Colors.White));
            }
        }

        lightingEngine.LightUpdateBFS(lightQueue);
    }

    private void UsePrecalculatedIfExists()
    {
        Image found = worldFile.LoadImage("SavedWorld", chunk.ChunkPosition + ".png");

        if (found == null)
        {
            ChunkLightBFS();
            SaveLight();
        }
        else
        {
            Rect2 chunkArea = new Rect2(Vector2.Zero, terrain.ChunkBlockCount);
            terrain.LightingEngine.WorldLightImage.GetImage().BlitRect(found, chunkArea, chunk.ChunkPosition * terrain.ChunkBlockCount);
        }
    }

    public void SaveLight()
    {
        Rect2 chunkArea = new Rect2(chunk.ChunkPosition * terrain.ChunkBlockCount, terrain.ChunkBlockCount);
        Image result = terrain.LightingEngine.WorldLightImage.GetImage().GetRect(chunkArea);
        worldFile.SaveImage(result, "SavedWorld", chunk.ChunkPosition + ".png");
    }
}
