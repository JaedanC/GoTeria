using Godot;
using Godot.Collections;
using System;



public class LightingEngine : Node2D
{
    private class LightUpdate
    {
        public Vector2 WorldPosition { get; set; }
        public Color Colour { get; set; }

        public LightUpdate(Vector2 worldPosition)
        {
            this.WorldPosition = worldPosition;
        }

        public LightUpdate(Vector2 worldPosition, Color colour)
        {
            this.WorldPosition = worldPosition;
            this.Colour = colour;
        }
    }

    private InputLayering inputLayering;
    private Terrain terrain;
    private Player player;
	public Image localLightLevels;
    private ImageTexture localLightLevelsShaderTexture;

	private System.Threading.Thread lightingThread;
	private volatile bool doLighting = true;
    public Mutex lightUpdateMutex;
    private System.Collections.Generic.Queue<LightUpdate> lightAddQueue;
    private System.Collections.Generic.Queue<LightUpdate> lightRemoveQueue;
    private System.Collections.Generic.Queue<LightUpdate> lightRemoveReaddQueue;


    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
            doLighting = false;
    }

    public override void _Ready()
    {
        inputLayering = GetNode<InputLayering>("/root/InputLayering");
        terrain = GetParent<Terrain>();
        player = GetNode<Player>("/root/WorldSpawn/Player");

        localLightLevels = new Image();
        localLightLevelsShaderTexture = new ImageTexture();
		lightingThread = new System.Threading.Thread(new System.Threading.ThreadStart(SpawnThread));
        lightAddQueue = new System.Collections.Generic.Queue<LightUpdate>();
        lightRemoveQueue = new System.Collections.Generic.Queue<LightUpdate>();
        lightRemoveReaddQueue = new System.Collections.Generic.Queue<LightUpdate>();
        lightUpdateMutex = new Mutex();
    }

    public void AddLight(Vector2 worldBlockPosition, Color lightValue)
    {
        lightAddQueue.Enqueue(new LightUpdate(worldBlockPosition, lightValue));
    }

    public void RemoveLight(Vector2 worldBlockPosition)
    {
        // Set the Remove Light Update to contain the current light level.
        Color existingColour = terrain.WorldLightLevels.GetPixelv(worldBlockPosition);
        lightRemoveQueue.Enqueue(new LightUpdate(worldBlockPosition, existingColour));
    }

    private void LightUpdatePass()
    {

        Array<Vector2> chunkPointCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 chunkPointTopLeftInPixels = chunkPointCorners[0] * terrain.ChunkPixelDimensions;
        Vector2 chunkPointBottomRightInPixels = (chunkPointCorners[1] + Vector2.One) * terrain.ChunkPixelDimensions;
        
        // Calculate where to put the lighting
        Vector2 position = chunkPointTopLeftInPixels;
        Vector2 scale = chunkPointBottomRightInPixels - chunkPointTopLeftInPixels;

        lightUpdateMutex.Lock();
        // Perform all light updates
        // https://www.seedofandromeda.com/blogs/29-fast-flood-fill-lighting-in-a-blocky-voxel-game-pt-1
        // Firstly compute all the light's that need to be removed.
        while (lightRemoveQueue.Count > 0)
        {
            LightRemoveUpdateBFS(lightRemoveQueue.Dequeue());
            // During the operation of removing lights we treat the edges as small light sources
            // They need to be computed first.
            while (lightRemoveReaddQueue.Count > 0)
            {
                LightAddUpdateBFS(lightRemoveReaddQueue.Dequeue());
            }
        }
        // Now compute the lights that need to be added.
        while (lightAddQueue.Count > 0)
        {
            LightAddUpdateBFS(lightAddQueue.Dequeue());
        }
        lightUpdateMutex.Unlock();

        // Copy the world light values to our local image
        Vector2 topLeftBlock = chunkPointCorners[0] * terrain.ChunkBlockCount;
        Vector2 blocksOnScreen = scale / terrain.BlockPixelSize;
        localLightLevels = new Image();
        localLightLevels.Create((int)blocksOnScreen.x, (int)blocksOnScreen.y, false, Image.Format.Rgba8);
        localLightLevels.BlitRect(terrain.WorldLightLevels, new Rect2(topLeftBlock, blocksOnScreen), Vector2.Zero);

        // Set our position to be the new location before we render it to the screen so that
        // it is in the correct location.
        Position = position;
        Scale = scale;

        // localLightLevelsShaderTexture.CreateFromImage(localLightLevels, (uint)ImageTexture.FlagsEnum.Mipmaps);
        localLightLevelsShaderTexture.CreateFromImage(localLightLevels);
        (Material as ShaderMaterial).SetShaderParam("light_values_size", localLightLevelsShaderTexture.GetSize());
        (Material as ShaderMaterial).SetShaderParam("light_values", localLightLevelsShaderTexture);
    }

	public void SpawnThread()
	{
		while (doLighting)
		{
			LightUpdatePass();
		}
	}

    private struct LightBFSNode
    {   
        public Vector2 Position;
        public Color Colour;

        public LightBFSNode(Vector2 position, Color colour) {
            Position = position;
            Colour = colour;
        }

        public LightBFSNode(LightUpdate LightUpdate)
        {
            this.Position = LightUpdate.WorldPosition;
            this.Colour = LightUpdate.Colour;
        }

    }

    private void LightAddUpdateBFS(LightUpdate lightAddUpdate)
    {   
        System.Collections.Generic.Queue<LightBFSNode> lightValuesFringe = new System.Collections.Generic.Queue<LightBFSNode>();
        lightValuesFringe.Enqueue(new LightBFSNode(lightAddUpdate));

        while (lightValuesFringe.Count > 0)
        {
            LightBFSNode node = lightValuesFringe.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = terrain.WorldLightLevels.GetPixelv(node.Position);
            if (node.Colour.r < existingColour.r)
            {
                continue;
            }

            // Set the colour then
            terrain.WorldLightLevels.SetPixelv(node.Position, node.Colour);
            float multiplier = Terrain.LIGHT_MULTIPLIER;
            Color newColour = new Color(
                node.Colour.r * multiplier,
                node.Colour.g * multiplier,
                node.Colour.b * multiplier,
                1
            );

            // Exit condition: The next colour would be too dark anyway
            if (newColour.r < Terrain.LIGHT_CUTOFF)
            {
                continue;
            }

            Vector2[] neighbourPositions = new Vector2[4];
            neighbourPositions[0] = new Vector2(node.Position.x - 1, node.Position.y);
            neighbourPositions[1] = new Vector2(node.Position.x + 1, node.Position.y);
            neighbourPositions[2] = new Vector2(node.Position.x, node.Position.y - 1);
            neighbourPositions[3] = new Vector2(node.Position.x, node.Position.y + 1);

            foreach (Vector2 neighbourPosition in neighbourPositions)
            {   
                if (Helper.InBounds(neighbourPosition, terrain.GetWorldSize()))
                {
                    lightValuesFringe.Enqueue(new LightBFSNode(neighbourPosition, newColour));
                }
            }

        }
    }

    private void LightRemoveUpdateBFS(LightUpdate lightRemoveUpdate)
    {   
        
        System.Collections.Generic.Queue<LightBFSNode> fringe = new System.Collections.Generic.Queue<LightBFSNode>();
        fringe.Enqueue(new LightBFSNode(lightRemoveUpdate));

        while (fringe.Count > 0)
        {
            LightBFSNode node = fringe.Dequeue();

            // Remove this blocks light. It doesn't have any light sources near it.
            terrain.WorldLightLevels.SetPixelv(node.Position, Colors.Black);

            // To use the same terminolody as the blog post.
            Color lightLevel = node.Colour;

            // "Look at all neighbouring blocks to that node.
            // if their light level is nonzero and is less than the current node:
            //      - Add them to the queue
            //      - Set their light level to zero.
            // else if it is >= current node:
            //      - Add it to light propagation queue."
            // This is used to check the neighbours efficiently
            Vector2[] neighbourPositions = new Vector2[4];
            neighbourPositions[0] = new Vector2(node.Position.x - 1, node.Position.y);
            neighbourPositions[1] = new Vector2(node.Position.x + 1, node.Position.y);
            neighbourPositions[2] = new Vector2(node.Position.x, node.Position.y - 1);
            neighbourPositions[3] = new Vector2(node.Position.x, node.Position.y + 1);
            foreach (Vector2 neighbourPosition in neighbourPositions)
            {   
                // First check to make sure the position is not out-of-bounds
                if (Helper.OutOfBounds(neighbourPosition, terrain.GetWorldSize()))
                    continue;
                
                Color neighboursLevel = terrain.WorldLightLevels.GetPixelv(neighbourPosition);
                if (neighboursLevel.r != 0 && neighboursLevel.r < lightLevel.r)
                {
                    fringe.Enqueue(new LightBFSNode(neighbourPosition, neighboursLevel));
                }
                else if (neighboursLevel.r >= lightLevel.r)
                {
                    // These are recomputed as lights with the neighbours light level because this
                    // neighbour has recieved its light from another source. We then need to consider that
                    // this block which is having its light source removed may be filled in by another block
                    // instead.
                    lightRemoveReaddQueue.Enqueue(new LightUpdate(neighbourPosition, neighboursLevel));
                }
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
		// if (lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
		// 	lightingThread.Start();

        LightUpdatePass();
    }

    public override void _Process(float delta)
    {

        if (inputLayering.PopAction("place_light"))
		{
			Vector2 worldPosition = player.ScreenToWorldPosition(GetViewport().GetMousePosition());
            AddLight(worldPosition / terrain.BlockPixelSize, Colors.White);
		}

		if (inputLayering.PopAction("remove_light"))
		{
			Vector2 worldPosition = player.ScreenToWorldPosition(GetViewport().GetMousePosition());
            RemoveLight(worldPosition / terrain.BlockPixelSize);
		}

    }


}

/*

Lighting algorithm:

The terrain has two light images that are the same size as the world:
1. WorldLightSourcesImage = This contains information about light sources. 
2. WorldLightLevelsImage = This contains information about the light levels of the world. This image is computed using the
image above.

(They do not need to be the exact size of the world in blocks in theory it could be larger to create a more detailed light surface.)

Light channels:
Each block in the world can be effected by a seemingly infinite number of lights. But the light level they possess will still
come down to some level between 0 and 1. Because a block that is next to a super bright source can't get brighter than 1.
Each blocks has a light level counter for each channel it wishes to have with lighting. For White this is one. For RGB this
is three. This light level number works the same same for each individual channel. Let's consider how the white channel works.
If a light source is added, then when we compute the Recursive algorithm to determine the light levels around the block, add the
intensity value to the block. A block in the world would be defined at the following:

class LightBlock {
    int light_value_white
    # int light_value_red
    # int light_value_green
    # int light_value_blue
}

class LightSource {
    int intensity
}

If a LightSource is removed, we can reverse the recursive algorithm. Instead of adding ourselves to the blocks
around us, subtract. This way the light is reduced by the appropriate amount. Note: The recursive algorithm we choose may be
BFS or even a version of Dijkstra if you're keen.

Still the problem of recalculating light when placing or removing non-light sources needs to be addressed.

When a chunk is loaded, image two is generated if it has not already been generated and copied onto the light_levels image.
If a light update occurs inside the world, push a LightSourceCompute class instance to a worker thread so that it can locally
recompute the light levels and copy it back to the light_levels image.

Light values are then passed through a sigmoid function and the resulting value is the intensity of the colour channel.
This way, blocks are effected by more than one source and appear more natural and smooth especially if filtering is turned
on.

Finally a blur filter could be applied to the light_source image to smooth out the edges of the lighting further. 


*/