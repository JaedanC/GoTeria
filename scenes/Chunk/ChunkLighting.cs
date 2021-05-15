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

    private struct LightValue
    {   
        public Vector2 Position;
        public Color Colour;
        public LightValue(Vector2 position, Color colour) {
            Position = position;
            Colour = colour;
        }
    }

    public void ComputeLightingPass()
    {
        System.Collections.Generic.Queue<LightValue> lightQueue = new System.Collections.Generic.Queue<LightValue>();

        Vector2 blockCount = terrain.ChunkBlockCount;
        Vector2 topLeftPixel = chunk.ChunkPosition * blockCount;

        for (int i = (int)topLeftPixel.x; i < topLeftPixel.x + blockCount.x; i++)
        for (int j = (int)topLeftPixel.y; j < topLeftPixel.y + blockCount.y; j++)
        {
            Vector2 position = new Vector2(i, j);
            Color sourceColour = terrain.WorldLightSources.GetPixelv(position);
            if (sourceColour == Colors.White)
            {
                lightQueue.Enqueue(new LightValue(position, Colors.White));
            }
        }


        while (lightQueue.Count > 0)
        {
            LightValue currentNode = lightQueue.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = terrain.WorldLightLevels.GetPixelv(currentNode.Position);
            if (existingColour.r > currentNode.Colour.r)
            {
                continue;
            }

            // Set the colour then
            terrain.WorldLightLevels.SetPixelv(currentNode.Position, currentNode.Colour);

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
            neighbourPositions[0] = new Vector2(currentNode.Position.x - 1, currentNode.Position.y);
            neighbourPositions[1] = new Vector2(currentNode.Position.x + 1, currentNode.Position.y);
            neighbourPositions[2] = new Vector2(currentNode.Position.x, currentNode.Position.y - 1);
            neighbourPositions[3] = new Vector2(currentNode.Position.x, currentNode.Position.y + 1);

            foreach (Vector2 neighbourPosition in neighbourPositions)
            {
                if (Helper.InBounds(neighbourPosition, terrain.WorldLightLevels.GetSize()))
                {
                    lightQueue.Enqueue(new LightValue(neighbourPosition, newColour));
                }
            }

        }
    }
}