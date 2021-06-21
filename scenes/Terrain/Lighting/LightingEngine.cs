using Godot;
using Godot.Collections;
using System.Collections.Generic;


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

    private InputLayering inputLayering;
    private Terrain terrain;
    private Player player;

	private Image _screenLightLevels;
    private ImageTexture _screenLightLevelsShaderTexture;
	private System.Threading.Thread _lightingThread;
    private LightUpdateColourQueue _lightUpdateAddQueue;
    private LightUpdateQueueSet _lightUpdateRemoveQueue;
    private LightUpdateColourQueue _lightUpdateRemoveToAddQueue;
    private Mutex _lightUpdateMutex;
    private Vector2 _nextScale;
    private Vector2 _nextPosition;
    private volatile bool _inPhysicsLoop;
	private bool _doLighting = true;
    private bool _updateShader = true;
    private Image _worldLightSources;
    private Image _worldLightLevels;
    public Image WorldLightSources { get { return _worldLightSources; } }
    public Image WorldLightLevels { get { return _worldLightLevels; } }


    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
        {
            _doLighting = false;
            if (_lightingThread.ThreadState == System.Threading.ThreadState.Running)
                _lightingThread.Join();
        }
    }

    public override void _Ready()
    {
        inputLayering = GetNode<InputLayering>("/root/InputLayering");
        terrain = GetParent<Terrain>();
        player = GetNode<Player>("/root/WorldSpawn/Player");

        _screenLightLevels = new Image();
        _screenLightLevelsShaderTexture = new ImageTexture();
		_lightingThread = new System.Threading.Thread(new System.Threading.ThreadStart(SpawnThread));
        _lightUpdateAddQueue = new LightUpdateColourQueue();
        _lightUpdateRemoveQueue = new LightUpdateQueueSet();
        _lightUpdateRemoveToAddQueue =  new LightUpdateColourQueue();
        _lightUpdateMutex = new Mutex();
    }

    public void Initialise()
    {
        Vector2 worldSize = terrain.GetWorldSize();

        _worldLightLevels = new Image();
        _worldLightLevels.Create((int)worldSize.x, (int)worldSize.y, false, Image.Format.Rgba8);
        _worldLightLevels.Lock();

        _worldLightSources = new Image();
        _worldLightSources.Create((int)worldSize.x, (int)worldSize.y, false, Image.Format.Rgba8);
        _worldLightSources.Fill(Colors.Red);
        _worldLightSources.Lock();
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
            _worldLightSources.SetPixel(i, j, lightValue);
        }

        _worldLightLevels.BlitRect(_worldLightSources, new Rect2(Vector2.Zero, terrain.GetWorldSize()), Vector2.Zero);
    }

    public void AddLight(Vector2 worldBlockPosition, Color lightValue)
    {
        worldBlockPosition = worldBlockPosition.Floor();
        // Return if the position is out of bounds.
        // Return if the lightValue does not beat what's already present.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) || 
            WorldLightLevels.GetPixelv(worldBlockPosition).r >= lightValue.r)
            return;

        // Queue the light update to be computed by the worker thread.
        _lightUpdateMutex.Lock();
        _lightUpdateAddQueue.Enqueue(new LightUpdate(worldBlockPosition, lightValue));
        _lightUpdateMutex.Unlock();
    }

    public void RemoveLight(Vector2 worldBlockPosition)
    {
        worldBlockPosition = worldBlockPosition.Floor();

        // Return if the position is out of bounds.
        // Return if the light at that block is already 0.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) ||
            WorldLightLevels.GetPixelv(worldBlockPosition).r == 0)
            return;

        // TODO: Fix flickering remove lights.
        // if (WorldLightSources.GetPixelv(worldBlockPosition).r == 0)
        //     return;
        
        // Set the Remove Light Update to contain the current light level. This is 
        // required for the remove light function to work. The function will handle
        // the setting of pixels.
        // Queue the light update to be computed by the worker thread.
        Color existingColour = WorldLightLevels.GetPixelv(worldBlockPosition);
        _lightUpdateMutex.Lock();
        _lightUpdateRemoveQueue.Enqueue(new LightUpdate(worldBlockPosition, existingColour));
        _lightUpdateMutex.Unlock();
    }

    public override void _PhysicsProcess(float delta)
    {
		if (_lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
			_lightingThread.Start();
        _inPhysicsLoop = true;

        if (inputLayering.PollAction("light_debug"))
        {
            GD.Print("Add: " + _lightUpdateAddQueue.Count);
        }
        if (inputLayering.PollAction("remove_light_debug"))
        {
            GD.Print("Remove: " + _lightUpdateRemoveQueue.Count);
        }
        if (inputLayering.PollAction("remove_light_add_debug"))
        {
            GD.Print("RemoveAdd: " + _lightUpdateRemoveToAddQueue.Count);
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
            WorldLightLevels.SetPixelv(node.WorldPosition, node.Colour);
        }

        while (lightValuesFringe.Count > 0)
        {
            LightBFSNode node = lightValuesFringe.Dequeue();

            // Exit condition: The current nodes color is brighter than us already.
            Color existingColour = WorldLightLevels.GetPixelv(node.WorldPosition);
            if (node.Colour.r < existingColour.r)
            {
                continue;
            }

            // Set the colour then
            WorldLightLevels.SetPixelv(node.WorldPosition, node.Colour);

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
                    Color neighbourExistingColour = WorldLightLevels.GetPixelv(neighbourPosition);
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
            // if (inputLayering.PollAction("remove_light_debug"))
            // {
            //     GD.Print("Fringe: " + fringe.Count);
            // }
            LightBFSNode node = fringe.Dequeue();

            // Remove this blocks light. It doesn't have any light sources near it.
            WorldLightLevels.SetPixelv(node.WorldPosition, Colors.Black);
            WorldLightSources.SetPixelv(node.WorldPosition, Colors.Black);

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
                
                Color neighboursLevel = WorldLightLevels.GetPixelv(neighbourPosition);
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
                    // This is causing lighting issues because these are being added to the fringe when they clearly are not lights.
                    // _lightUpdateRemoveToAddQueue.Enqueue(new LightUpdate(neighbourPosition, neighboursLevel));
                    removeAddLightUpdateList.Add(new LightUpdate(neighbourPosition, neighboursLevel));
                }
            }
        }

        foreach (LightUpdate update in removeAddLightUpdateList)
        {
            if (WorldLightLevels.GetPixelv(update.WorldPosition).r > 0)
            {
                 _lightUpdateRemoveToAddQueue.Enqueue(update);
            }
        }
    }

	private void SpawnThread()
	{
        while (_doLighting) // Used to kill the thread
        {
            // This is run whenever the physics loop hits on the main thread so that this thread
            // is not running too quickly. Yes! This thread running too quickly is a problem because
            // of the line ImageTexture.CreateFromImage() in UpdateShaderTexture(). If you call
            // this function before it has an opportunity to render it will throw a "Can't resize pool
            // vector if locked" error silently-ish until you crash.
            if (_inPhysicsLoop)
            {
                LightUpdatePass();
                // UpdateShaderTexture();
                _inPhysicsLoop = false;
                OS.DelayMsec(1);
            }
        }
	}

    private void LightUpdatePass()
    {
        if (_lightUpdateRemoveQueue.Count == 0 && _lightUpdateAddQueue.Count == 0)
            return;

        // Perform all light updates
        // https://www.seedofandromeda.com/blogs/29-fast-flood-fill-lighting-in-a-blocky-voxel-game-pt-1
        // Firstly compute all the light's that need to be removed. The queues are Mutexed so the main thread
        // won't race with the queues if they want a BlockUpdate to happen.
        _lightUpdateMutex.Lock();
        while (_lightUpdateRemoveQueue.Count > 0)
        {
            DoRemoveLightUpdates(_lightUpdateRemoveQueue.Dequeue());

            // During the operation of removing lights we treat the edges as small light sources
            // They need to be computed first.
            while (_lightUpdateRemoveToAddQueue.Count > 0)
                DoAddLightUpdates(_lightUpdateRemoveToAddQueue.Dequeue());
        }
        // Now compute the lights that need to be added.
        while (_lightUpdateAddQueue.Count > 0)
            DoAddLightUpdates(_lightUpdateAddQueue.Dequeue());
        
        _lightUpdateMutex.Unlock();
        
        _updateShader = true;
    }

    public void SetUpdateShader(bool doUpdate)
    {
        _updateShader = doUpdate;
    }

    private void CalculateNextShaderRectangle()
    {
        Array<Vector2> chunkPositionCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 chunkPositionTopLeftInPixels = chunkPositionCorners[0] * terrain.ChunkPixelDimensions;
        Vector2 chunkPositionBottomRightInPixels = (chunkPositionCorners[1] + Vector2.One) * terrain.ChunkPixelDimensions;

        _nextPosition = chunkPositionTopLeftInPixels;
        _nextScale = chunkPositionBottomRightInPixels - chunkPositionTopLeftInPixels;
    }

    private void UpdateShaderTexture()
    {
        CalculateNextShaderRectangle();

        bool positionSame = _nextPosition == Position;
        bool scaleSame = _nextScale == Scale;

        if (positionSame && scaleSame && !_updateShader)
            return;

        _updateShader = false;


        // Set our position and scale to be the new location before we render it to the screen so that
        // it is in the correct location.
        Position = _nextPosition;
        Scale = _nextScale;

        // Copy the image section from the terrain.WorldLightLevels that is visible to the player.
        Array<Vector2> chunkPositionCorners = player.GetVisibilityChunkPositionCorners();
        Vector2 topLeftBlock = chunkPositionCorners[0] * terrain.ChunkBlockCount;
        Vector2 blocksOnScreen = Scale / terrain.BlockPixelSize;

        if (!scaleSame)
        {
            // "Can't resize pool vector if locked"
            _screenLightLevels.Create((int)blocksOnScreen.x, (int)blocksOnScreen.y, false, Image.Format.Rgba8);
            _screenLightLevelsShaderTexture.CreateFromImage(_screenLightLevels, 0);
        }

        _screenLightLevels.BlitRect(WorldLightLevels, new Rect2(topLeftBlock, blocksOnScreen), Vector2.Zero);
        _screenLightLevelsShaderTexture.SetData(_screenLightLevels);
        (Material as ShaderMaterial).SetShaderParam("light_values_size", _screenLightLevelsShaderTexture.GetSize());
        (Material as ShaderMaterial).SetShaderParam("light_values", _screenLightLevelsShaderTexture);
    }
}
