using Godot;
using Godot.Collections;
using System;

public class ChunkLighting
{
    private Terrain terrain;
    private LightingEngine lightingEngine;
    private Chunk chunk;

    public ChunkLighting(Chunk chunk, Terrain terrain)
    {
        this.chunk = chunk;
        this.terrain = terrain;
        this.lightingEngine = terrain.LightingEngine;
    }

    public void ComputeLightingPass()
    {   
        ChunkLightBFS();
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
}