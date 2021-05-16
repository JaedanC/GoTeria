using Godot;
using Godot.Collections;
using System;



public class LightingEngine : Node2D
{
    public struct LightBFSNode
    {   
        public Vector2 WorldPosition;
        public Color Colour;

        public LightBFSNode(Vector2 worldPosition, Color colour) {
            this.WorldPosition = worldPosition;
            this.Colour = colour;
        }

        public LightBFSNode(LightUpdate LightUpdate)
        {
            this.WorldPosition = LightUpdate.WorldPosition;
            this.Colour = LightUpdate.Colour;
        }

    }
    
    public class LightUpdate
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
	private Image localLightLevels;
    private ImageTexture localLightLevelsShaderTexture;

	private System.Threading.Thread lightingThread;
	private volatile bool doLighting = true;
    private System.Collections.Generic.Queue<LightUpdate> lightAddQueue;
    private System.Collections.Generic.Queue<LightUpdate> lightRemoveQueue;
    private System.Collections.Generic.Queue<LightUpdate> lightRemoveReaddQueue;
    private Mutex lightUpdateQueueMutex;
    private Vector2 temporaryScale;
    private Vector2 temporaryPosition;
    private volatile bool inPhysicsLoop;
    private Vector2 previousBlocksOnScreen;
    public bool UpdateShader = true;

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
        {
            doLighting = false;
            if (lightingThread.ThreadState == System.Threading.ThreadState.Running)
                lightingThread.Join();
        }
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
        lightUpdateQueueMutex = new Mutex();
    }

    public void AddLight(Vector2 worldBlockPosition, Color lightValue)
    {
        // Return if the position is out of bounds.
        // Return if the lightValue does not beat what's already present.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) || 
            terrain.WorldLightLevels.GetPixelv(worldBlockPosition).r >= lightValue.r)
            return;

        // Queue the light update to be computed by the worker thread.
        EnqueueAddLightUpdate(new LightUpdate(worldBlockPosition, lightValue));
    }

    public void RemoveLight(Vector2 worldBlockPosition)
    {
        // Return if the position is out of bounds.
        // Return if the light at that block is already 0.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) ||
            terrain.WorldLightLevels.GetPixelv(worldBlockPosition).r == 0)
            return;
        
        // Set the Remove Light Update to contain the current light level. This is 
        // required for the remove light function to work. The function will handle
        // the setting of pixels.
        Color existingColour = terrain.WorldLightLevels.GetPixelv(worldBlockPosition);
        EnqueueRemoveLightUpdate(new LightUpdate(worldBlockPosition, existingColour));
    }

    public override void _PhysicsProcess(float delta)
    {
		// if (lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
		// 	lightingThread.Start();
        inPhysicsLoop = true;

        LightUpdatePass();
        UpdateShaderTexture();
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

    private void EnqueueAddLightUpdate(LightUpdate lightUpdate)
    {
        lightUpdateQueueMutex.Lock();
        lightAddQueue.Enqueue(lightUpdate);
        lightUpdateQueueMutex.Unlock();
    }

    private void EnqueueRemoveLightUpdate(LightUpdate lightUpdate)
    {
        lightUpdateQueueMutex.Lock();
        lightRemoveQueue.Enqueue(lightUpdate);
        lightUpdateQueueMutex.Unlock();
    }

    

    private void LightAddUpdateBFS(LightUpdate lightAddUpdate)
    {   
        System.Collections.Generic.Queue<LightBFSNode> lightValuesFringe = new System.Collections.Generic.Queue<LightBFSNode>();
        lightValuesFringe.Enqueue(new LightBFSNode(lightAddUpdate));

        while (lightValuesFringe.Count > 0)
        {
            LightBFSNode node = lightValuesFringe.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = terrain.WorldLightLevels.GetPixelv(node.WorldPosition);
            if (node.Colour.r < existingColour.r)
            {
                continue;
            }

            // Set the colour then
            terrain.WorldLightLevels.SetPixelv(node.WorldPosition, node.Colour);
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
            neighbourPositions[0] = new Vector2(node.WorldPosition.x - 1, node.WorldPosition.y);
            neighbourPositions[1] = new Vector2(node.WorldPosition.x + 1, node.WorldPosition.y);
            neighbourPositions[2] = new Vector2(node.WorldPosition.x, node.WorldPosition.y - 1);
            neighbourPositions[3] = new Vector2(node.WorldPosition.x, node.WorldPosition.y + 1);

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
            terrain.WorldLightLevels.SetPixelv(node.WorldPosition, Colors.Black);

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
            neighbourPositions[0] = new Vector2(node.WorldPosition.x - 1, node.WorldPosition.y);
            neighbourPositions[1] = new Vector2(node.WorldPosition.x + 1, node.WorldPosition.y);
            neighbourPositions[2] = new Vector2(node.WorldPosition.x, node.WorldPosition.y - 1);
            neighbourPositions[3] = new Vector2(node.WorldPosition.x, node.WorldPosition.y + 1);
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

	private void SpawnThread()
	{
        while (doLighting) // Used to kill the thread
        {
            // This is run whenever the physics loop hits on the main thread so that this thread
            // is not running too quickly. Yes! This thread running too quickly is a problem because
            // of the line ImageTexture.CreateFromImage() in UpdateShaderTexture(). If you call
            // this function before it has an opportunity to render it will throw a "Can't resize pool
            // vector if locked" error silently-ish until you crash.
            if (inPhysicsLoop)
            {
                LightUpdatePass();
                // UpdateShaderTexture();
                inPhysicsLoop = false;
                OS.DelayMsec(1);
            }
        }
	}

    private bool DoRecomputeShader()
    {
        return temporaryPosition != Position || temporaryScale != Scale || UpdateShader;
    }

    private bool CanSkipLightPass()
    {
        // If nothing has changed
        return lightRemoveQueue.Count == 0 && lightAddQueue.Count == 0;
    }

    private void LightUpdatePass()
    {
        if (CanSkipLightPass())
        {
            // GD.Print("Skipping");
            return;
        }


        // Perform all light updates
        // https://www.seedofandromeda.com/blogs/29-fast-flood-fill-lighting-in-a-blocky-voxel-game-pt-1
        // Firstly compute all the light's that need to be removed. The queues are Mutexed so the main thread
        // won't race with the queues if they want a BlockUpdate to happen.
        // lightUpdateQueueMutex.Lock();
        while (lightRemoveQueue.Count > 0)
        {
            LightRemoveUpdateBFS(lightRemoveQueue.Dequeue());
            // During the operation of removing lights we treat the edges as small light sources
            // They need to be computed first.
            while (lightRemoveReaddQueue.Count > 0)
                LightAddUpdateBFS(lightRemoveReaddQueue.Dequeue());
        }
        // Now compute the lights that need to be added.
        while (lightAddQueue.Count > 0)
            LightAddUpdateBFS(lightAddQueue.Dequeue());
        // lightUpdateQueueMutex.Unlock();

        
        UpdateShader = true;
    }

    private void UpdateShaderTexture()
    {
        
        Array<Vector2> chunkPositionCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 chunkPositionTopLeftInPixels = chunkPositionCorners[0] * terrain.ChunkPixelDimensions;
        Vector2 chunkPositionBottomRightInPixels = (chunkPositionCorners[1] + Vector2.One) * terrain.ChunkPixelDimensions;

        temporaryPosition = chunkPositionTopLeftInPixels;
        temporaryScale = chunkPositionBottomRightInPixels - chunkPositionTopLeftInPixels;

        if (!DoRecomputeShader())
            return;
        UpdateShader = false;

        // Set our position and scale to be the new location before we render it to the screen so that
        // it is in the correct location.
        Position = temporaryPosition;
        Scale = temporaryScale;

        // Copy the image section from the terrain.WorldLightLevels that is visible to the player.
        Vector2 topLeftBlock = chunkPositionCorners[0] * terrain.ChunkBlockCount;
        Vector2 blocksOnScreen = Scale / terrain.BlockPixelSize;

        if (blocksOnScreen != previousBlocksOnScreen)
        {
            // "Can't resize pool vector if locked"
            localLightLevels.Create((int)blocksOnScreen.x, (int)blocksOnScreen.y, false, Image.Format.Rgba8);
            localLightLevelsShaderTexture.CreateFromImage(localLightLevels, 0);
        }
        previousBlocksOnScreen = blocksOnScreen;

        localLightLevels.BlitRect(terrain.WorldLightLevels, new Rect2(topLeftBlock, blocksOnScreen), Vector2.Zero);
        localLightLevelsShaderTexture.SetData(localLightLevels);
        (Material as ShaderMaterial).SetShaderParam("light_values_size", localLightLevelsShaderTexture.GetSize());
        (Material as ShaderMaterial).SetShaderParam("light_values", localLightLevelsShaderTexture);
    }

}
