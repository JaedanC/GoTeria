using Godot;
using Godot.Collections;
using System.Collections.Generic;


public class LightingEngine : Node2D
{
    public struct LightBFSNode
    {
        public Vector2 WorldPosition;
        public Color Colour;

        public LightBFSNode(Vector2 worldPosition, Color colour)
        {
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
        public Vector2 WorldPosition;
        public Color Colour;

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

    private class LightImage
    {
        private Image worldLightImage;
        private System.Collections.Generic.Dictionary<Vector2, Color> colourChanges;
        private Mutex imageMutex;

        public LightImage(Image worldLightImage)
        {
            this.worldLightImage = worldLightImage;
            colourChanges = new System.Collections.Generic.Dictionary<Vector2, Color>();
            imageMutex = new Mutex();
        }

        public void SetPixelv(Vector2 worldPosition, Color colour)
        {
            imageMutex.Lock();
            colourChanges[worldPosition] = colour;
            imageMutex.Unlock();
        }

        public Color GetPixelv(Vector2 worldPosition)
        {
            imageMutex.Lock();
            Color colour;
            if (colourChanges.ContainsKey(worldPosition))
                colour = colourChanges[worldPosition];
            else
                colour = worldLightImage.GetPixelv(worldPosition);
            imageMutex.Unlock();
            return colour;
        }

        public void LockImage()
        {
            imageMutex.Lock();
        }

        public Image GetImage()
        {
            return worldLightImage;
        }

        public void UnlockImage()
        {
            imageMutex.Unlock();
        }

        public void CommitColourChanges()
        {
            imageMutex.Lock();
            foreach (Vector2 pixel in colourChanges.Keys)
            {
                worldLightImage.SetPixelv(pixel, colourChanges[pixel]);
            }
            colourChanges.Clear();
            imageMutex.Unlock();
        }
    }

    private InputLayering inputLayering;
    private Terrain terrain;
    private Player player;

    private Image screenLightLevels;
    private ImageTexture screenLightLevelsShaderTexture;
    private System.Threading.Thread lightingThread;
    private LightUpdateColourQueueSet lightUpdateAddQueue;
    private LightUpdateQueueSet lightUpdateRemoveQueue;
    private LightUpdateColourQueueSet lightUpdateRemoveToAddQueue;
    private Mutex lightUpdateMutex;
    private Vector2 nextScale;
    private Vector2 nextPosition;
    private bool keepLightThread = true;
    private bool updateShader = true;
    private LightImage worldLightImage;
    private Image worldLightSources;
    public Image WorldLightSources { get { return worldLightSources; } }


    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
        {
            keepLightThread = false;
            if (lightingThread.ThreadState == System.Threading.ThreadState.Running)
                lightingThread.Join();
        }
    }

    public override void _Ready()
    {
        inputLayering = GetNode<InputLayering>("/root/InputLayering");
        terrain = GetParent<Terrain>();
        player = GetNode<Player>("/root/WorldSpawn/Player");

        screenLightLevels = new Image();
        screenLightLevelsShaderTexture = new ImageTexture();
        lightingThread = new System.Threading.Thread(new System.Threading.ThreadStart(LightingThread));
        lightUpdateAddQueue = new LightUpdateColourQueueSet();
        lightUpdateRemoveQueue = new LightUpdateQueueSet();
        lightUpdateRemoveToAddQueue = new LightUpdateColourQueueSet();
        lightUpdateMutex = new Mutex();
    }

    public void Initialise()
    {
        Vector2 worldSize = terrain.GetWorldSize();

        Image worldLightLevels = new Image();
        worldLightLevels.Create((int)worldSize.x, (int)worldSize.y, false, Image.Format.Rgba8);
        worldLightLevels.Lock();

        worldLightSources = new Image();
        worldLightSources.Create((int)worldSize.x, (int)worldSize.y, false, Image.Format.Rgba8);
        worldLightSources.Fill(Colors.Red);
        worldLightSources.Lock();
        for (int i = 0; i < worldSize.x; i++)
        for (int j = 0; j < worldSize.y; j++)
        {
            Color blockColour = terrain.WorldBlocksImage.GetPixel(i, j);
            Color wallColour = terrain.WorldWallsImage.GetPixel(i, j);
            Color lightValue;
            if (Helper.IsLight(blockColour) && Helper.IsLight(wallColour))
                lightValue = Colors.White;
            else
                lightValue = Colors.Black;
            worldLightSources.SetPixel(i, j, lightValue);
        }

        worldLightLevels.BlitRect(worldLightSources, new Rect2(Vector2.Zero, terrain.GetWorldSize()), Vector2.Zero);
        worldLightImage = new LightImage(worldLightLevels);
    }

    public void AddLight(Vector2 worldBlockPosition, Color lightValue)
    {
        worldBlockPosition = worldBlockPosition.Floor();
        // Return if the position is out of bounds.
        // Return if the lightValue does not beat what's already present.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) ||
            worldLightImage.GetPixelv(worldBlockPosition).r >= lightValue.r)
            return;

        // Queue the light update to be computed by the worker thread.
        lightUpdateMutex.Lock();
        lightUpdateAddQueue.Enqueue(new LightUpdate(worldBlockPosition, lightValue));
        lightUpdateMutex.Unlock();
    }

    public void RemoveLight(Vector2 worldBlockPosition)
    {
        worldBlockPosition = worldBlockPosition.Floor();

        // Return if the position is out of bounds.
        // Return if the light at that block is already 0.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) ||
            worldLightImage.GetPixelv(worldBlockPosition).r == 0)
            return;

        // Set the Remove Light Update to contain the current light level. This is 
        // required for the remove light function to work. The function will handle
        // the setting of pixels.
        // Queue the light update to be computed by the worker thread.
        Color existingColour = worldLightImage.GetPixelv(worldBlockPosition);
        lightUpdateMutex.Lock();
        lightUpdateRemoveQueue.Enqueue(new LightUpdate(worldBlockPosition, existingColour));
        lightUpdateMutex.Unlock();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
            lightingThread.Start();

        if (inputLayering.PollAction("light_debug"))
        {
            GD.Print("Add: " + lightUpdateAddQueue.Count);
        }
        if (inputLayering.PollAction("remove_light_debug"))
        {
            GD.Print("Remove: " + lightUpdateRemoveQueue.Count);
        }
        if (inputLayering.PollAction("remove_light_add_debug"))
        {
            GD.Print("RemoveAdd: " + lightUpdateRemoveToAddQueue.Count);
        }

        // LightUpdatePass();
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

    private void DoAddLightUpdates(LightUpdate lightAddUpdate)
    {
        QueueSet<LightBFSNode> lightValuesFringe = new QueueSet<LightBFSNode>();
        lightValuesFringe.Enqueue(new LightBFSNode(lightAddUpdate));
        LightUpdateBFS(lightValuesFringe);
    }

    public void LightUpdateBFS(QueueSet<LightBFSNode> lightValuesFringe)
    {
        // This precomputation prevents a lot of neighbour exploration for a large fringe.
        foreach (LightBFSNode node in lightValuesFringe.ToList())
        {
            worldLightImage.SetPixelv(node.WorldPosition, node.Colour);
        }

        while (lightValuesFringe.Count > 0)
        {
            LightBFSNode node = lightValuesFringe.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = worldLightImage.GetPixelv(node.WorldPosition);
            if (node.Colour.r < existingColour.r)
            {
                continue;
            }

            // Set the colour then
            worldLightImage.SetPixelv(node.WorldPosition, node.Colour);

            IBlock topBlock = terrain.GetTopIBlockFromWorldBlockPosition(node.WorldPosition);
            if (topBlock == null)
            {
                continue;
            }

            float reduction = topBlock.GetTransparency();
            Color newColour = new Color(
                node.Colour.r - reduction,
                node.Colour.g - reduction,
                node.Colour.b - reduction,
                1
            );

            // Exit condition: The next colour would be too dark anyway
            if (newColour.r <= 0)
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
                    Color neighbourExistingColour = worldLightImage.GetPixelv(neighbourPosition);
                    if (neighbourExistingColour.r < node.Colour.r)
                    {
                        lightValuesFringe.Enqueue(new LightBFSNode(neighbourPosition, newColour));
                    }
                }
            }
        }
    }

    private void DoRemoveLightUpdates(LightUpdate lightRemoveUpdate)
    {
        QueueSet<LightBFSNode> fringe = new QueueSet<LightBFSNode>();
        fringe.Enqueue(new LightBFSNode(lightRemoveUpdate));

        List<LightUpdate> removeAddLightUpdateList = new List<LightUpdate>();

        while (fringe.Count > 0)
        {
            LightBFSNode node = fringe.Dequeue();

            // Remove this blocks light. It doesn't have any light sources near it.
            worldLightImage.SetPixelv(node.WorldPosition, Colors.Black);

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

                Color neighboursLevel = worldLightImage.GetPixelv(neighbourPosition);
                if (neighboursLevel.r != 0 && neighboursLevel.r < lightLevel.r)
                {
                    fringe.Enqueue(new LightBFSNode(neighbourPosition, neighboursLevel));
                }
                else if (neighboursLevel.r >= lightLevel.r)
                {
                    // These are recomputed as lights with the neighbours light level because this
                    // neighbour has recieved its light from another source. We then need to consider that
                    // this block which is having its light source removed may be filled in by another block
                    // instead. You can't simply add these to the Queue first, because a BFS may visit a node
                    // with a higher light level (hence thinking it's a source) but then is visited by another
                    // node and made dark. This can make light sources seemingly randomly appear when removing
                    // a light. Instead, we add the Update to a list and iterate through it at the end, culling
                    // all updates that have their pixel black (reduced by a neighbouring node).
                    removeAddLightUpdateList.Add(new LightUpdate(neighbourPosition, neighboursLevel));
                }
            }
        }

        // Cull the AddLight updates that will make false positive lights appear.
        foreach (LightUpdate update in removeAddLightUpdateList)
        {
            if (worldLightImage.GetPixelv(update.WorldPosition).r > 0)
            {
                lightUpdateRemoveToAddQueue.Enqueue(update);
            }
        }
    }

    private void LightingThread()
    {
        while (keepLightThread) // Used to kill the thread
        {
            // This thread running too quickly may become a problem because of the line
            // ImageTexture.CreateFromImage() in UpdateShaderTexture(). If you call this
            // function before it has an opportunity to render it will throw a "Can't resize pool
            // vector if locked" error silently-ish until you crash. In the past this was
            // synced to the physics loop and that worked fine. However, it seems to work
            // without it now :)
            LightUpdatePass();

            // This is delayed to give AddLight() and RemoveLight() time to acquire the lock.
            // TODO: This could use a copy system in the future so that the lock isn't required
            // whole time.
            OS.DelayMsec(2);
        }
    }

    private void LightUpdatePass()
    {
        worldLightImage.CommitColourChanges();

        if (lightUpdateRemoveQueue.Count == 0 && lightUpdateAddQueue.Count == 0)
            return;

        // Perform all light updates
        // https://www.seedofandromeda.com/blogs/29-fast-flood-fill-lighting-in-a-blocky-voxel-game-pt-1
        // Firstly compute all the light's that need to be removed. The queues are Mutexed so the main thread
        // won't race with the queues if they want a BlockUpdate to happen.
        lightUpdateMutex.Lock();
        while (lightUpdateRemoveQueue.Count > 0)
        {
            DoRemoveLightUpdates(lightUpdateRemoveQueue.Dequeue());

            // During the operation of removing lights we treat the edges as small light sources
            // They need to be computed first.
            while (lightUpdateRemoveToAddQueue.Count > 0)
                DoAddLightUpdates(lightUpdateRemoveToAddQueue.Dequeue());
        }
        // Now compute the lights that need to be added.
        while (lightUpdateAddQueue.Count > 0)
            DoAddLightUpdates(lightUpdateAddQueue.Dequeue());

        lightUpdateMutex.Unlock();

        worldLightImage.CommitColourChanges();

        updateShader = true;
    }

    public void SetUpdateShader(bool doUpdate)
    {
        updateShader = doUpdate;
    }

    private void CalculateNextShaderRectangle()
    {
        Array<Vector2> chunkPositionCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 chunkPositionTopLeftInPixels = chunkPositionCorners[0] * terrain.ChunkPixelDimensions;
        Vector2 chunkPositionBottomRightInPixels = (chunkPositionCorners[1] + Vector2.One) * terrain.ChunkPixelDimensions;

        nextPosition = chunkPositionTopLeftInPixels;
        nextScale = chunkPositionBottomRightInPixels - chunkPositionTopLeftInPixels;
    }

    private void UpdateShaderTexture()
    {
        CalculateNextShaderRectangle();

        bool positionSame = nextPosition == Position;
        bool scaleSame = nextScale == Scale;

        if (positionSame && scaleSame && !updateShader)
            return;

        updateShader = false;

        // Set our position and scale to be the new location before we render it to the screen so that
        // it is in the correct location.
        Position = nextPosition;
        Scale = nextScale;

        // Copy the image section from the terrain.WorldLightLevels that is visible to the player.
        Array<Vector2> chunkPositionCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 topLeftBlock = chunkPositionCorners[0] * terrain.ChunkBlockCount;
        Vector2 blocksOnScreen = Scale / terrain.BlockPixelSize;

        if (!scaleSame)
        {
            // "Can't resize pool vector if locked"
            screenLightLevels.Create((int)blocksOnScreen.x, (int)blocksOnScreen.y, false, Image.Format.Rgba8);
            screenLightLevelsShaderTexture.CreateFromImage(screenLightLevels, (uint)Texture.FlagsEnum.Filter);
        }

        worldLightImage.LockImage();
        screenLightLevels.BlitRect(worldLightImage.GetImage(), new Rect2(topLeftBlock, blocksOnScreen), Vector2.Zero);
        worldLightImage.UnlockImage();
        screenLightLevelsShaderTexture.SetData(screenLightLevels);
        (Material as ShaderMaterial).SetShaderParam("light_values_size", screenLightLevelsShaderTexture.GetSize());
        (Material as ShaderMaterial).SetShaderParam("light_values", screenLightLevelsShaderTexture);
    }
}
