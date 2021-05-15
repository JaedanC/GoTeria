using Godot;
using Godot.Collections;
using System;

public class ChunkLighting
{
    private Terrain terrain;
    private Chunk chunk;

    public ChunkLighting(Chunk chunk, Terrain terrain)
    {
        this.chunk = chunk;
        this.terrain = terrain;
    }

    public void ComputeLightingPass()
    {
        System.Collections.Generic.Queue<LightingEngine.LightBFSNode> lightQueue = new System.Collections.Generic.Queue<LightingEngine.LightBFSNode>();

        Vector2 chunkBlockCount = terrain.ChunkBlockCount;
        Vector2 topLeftPixel = chunk.ChunkPosition * chunkBlockCount;

        // GD.Print("Chunk: ", topLeftPixel, chunkBlockCount);
        for (int i = (int)topLeftPixel.x; i < topLeftPixel.x + chunkBlockCount.x; i++)
        for (int j = (int)topLeftPixel.y; j < topLeftPixel.y + chunkBlockCount.y; j++)
        {
            Vector2 position = new Vector2(i, j);
            if (Helper.OutOfBounds(position, terrain.GetWorldSize()))
                continue;

            Color sourceColour = terrain.WorldLightSources.GetPixelv(position);
            if (sourceColour == Colors.White)
            {
                lightQueue.Enqueue(new LightingEngine.LightBFSNode(position, Colors.White));
            }
        }


        while (lightQueue.Count > 0)
        {
            LightingEngine.LightBFSNode currentNode = lightQueue.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = terrain.WorldLightLevels.GetPixelv(currentNode.WorldPosition);
            if (existingColour.r > currentNode.Colour.r)
            {
                continue;
            }

            // Set the colour then
            terrain.WorldLightLevels.SetPixelv(currentNode.WorldPosition, currentNode.Colour);

            float multiplier = Terrain.LIGHT_MULTIPLIER;
            Color newColour = new Color(
                currentNode.Colour.r * multiplier,
                currentNode.Colour.g * multiplier,
                currentNode.Colour.b * multiplier,
                1
            );

            // Exit condition: The next colour would be too dark anyway
            if (newColour.r < Terrain.LIGHT_CUTOFF)
            {
                continue;
            }

            Vector2[] neighbourPositions = new Vector2[4];
            neighbourPositions[0] = new Vector2(currentNode.WorldPosition.x - 1, currentNode.WorldPosition.y);
            neighbourPositions[1] = new Vector2(currentNode.WorldPosition.x + 1, currentNode.WorldPosition.y);
            neighbourPositions[2] = new Vector2(currentNode.WorldPosition.x, currentNode.WorldPosition.y - 1);
            neighbourPositions[3] = new Vector2(currentNode.WorldPosition.x, currentNode.WorldPosition.y + 1);

            foreach (Vector2 neighbourPosition in neighbourPositions)
            {
                if (Helper.InBounds(neighbourPosition, terrain.WorldLightLevels.GetSize()))
                {
                    lightQueue.Enqueue(new LightingEngine.LightBFSNode(neighbourPosition, newColour));
                }
            }

        }
    }
}