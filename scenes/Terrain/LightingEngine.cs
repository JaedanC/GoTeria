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

    public const float LIGHT_MULTIPLIER = 0.5f;
    public const float LIGHT_CUTOFF = 0.1f;

    private InputLayering inputLayering;
    private Terrain terrain;
    private Player player;

	private Image _screenLightLevels;
    private ImageTexture _screenLightLevelsShaderTexture;
	private System.Threading.Thread _lightingThread;
    private Queue<LightUpdate> _lightUpdateAddQueue;
    private Queue<LightUpdate> _lightUpdateRemoveQueue;
    private Queue<LightUpdate> _lightUpdateRemoveToAddQueue;
    private Godot.Mutex _lightUpdateQueueMutex;
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
        _lightUpdateAddQueue = new Queue<LightUpdate>();
        _lightUpdateRemoveQueue = new Queue<LightUpdate>();
        _lightUpdateRemoveToAddQueue = new Queue<LightUpdate>();
        _lightUpdateQueueMutex = new Godot.Mutex();
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
        for (int i = 0; i < _worldLightSources.GetWidth(); i++)
        for (int j = 0; j < _worldLightSources.GetHeight(); j++)
        {
            Color colour = terrain.WorldImage.GetPixel(i, j);
            if (Helper.IsLight(colour))
                _worldLightSources.SetPixel(i, j, Colors.White);
            else
                _worldLightSources.SetPixel(i, j, Colors.Black);
        }
    }

    public void AddLight(Vector2 worldBlockPosition, Color lightValue)
    {
        // Return if the position is out of bounds.
        // Return if the lightValue does not beat what's already present.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) || 
            WorldLightLevels.GetPixelv(worldBlockPosition).r >= lightValue.r)
            return;

        // Queue the light update to be computed by the worker thread.
        EnqueueAddLightUpdate(new LightUpdate(worldBlockPosition, lightValue));
    }

    public void RemoveLight(Vector2 worldBlockPosition)
    {
        // Return if the position is out of bounds.
        // Return if the light at that block is already 0.
        if (Helper.OutOfBounds(worldBlockPosition, terrain.GetWorldSize()) ||
            WorldLightLevels.GetPixelv(worldBlockPosition).r == 0)
            return;
        
        // Set the Remove Light Update to contain the current light level. This is 
        // required for the remove light function to work. The function will handle
        // the setting of pixels.
        Color existingColour = WorldLightLevels.GetPixelv(worldBlockPosition);
        EnqueueRemoveLightUpdate(new LightUpdate(worldBlockPosition, existingColour));
    }

    public override void _PhysicsProcess(float delta)
    {
		if (_lightingThread.ThreadState == System.Threading.ThreadState.Unstarted)
			_lightingThread.Start();
        _inPhysicsLoop = true;

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

    private void EnqueueAddLightUpdate(LightUpdate lightUpdate)
    {
        _lightUpdateQueueMutex.Lock();
        _lightUpdateAddQueue.Enqueue(lightUpdate);
        _lightUpdateQueueMutex.Unlock();
    }

    private void EnqueueRemoveLightUpdate(LightUpdate lightUpdate)
    {
        _lightUpdateQueueMutex.Lock();
        _lightUpdateRemoveQueue.Enqueue(lightUpdate);
        _lightUpdateQueueMutex.Unlock();
    }

    private void DoAddLightUpdates(LightUpdate lightAddUpdate)
    {   
        Queue<LightBFSNode> lightValuesFringe = new Queue<LightBFSNode>();
        lightValuesFringe.Enqueue(new LightBFSNode(lightAddUpdate));
        LightUpdateBFS(lightValuesFringe);
    }   

    public void LightUpdateBFS(Queue<LightBFSNode> lightValuesFringe)
    {
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
            float multiplier = LIGHT_MULTIPLIER;
            Color newColour = new Color(
                node.Colour.r * multiplier,
                node.Colour.g * multiplier,
                node.Colour.b * multiplier,
                1
            );

            // Exit condition: The next colour would be too dark anyway
            if (newColour.r < LIGHT_CUTOFF)
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

    private void DoRemoveLightUpdates(LightUpdate lightRemoveUpdate)
    {   
        
        System.Collections.Generic.Queue<LightBFSNode> fringe = new System.Collections.Generic.Queue<LightBFSNode>();
        fringe.Enqueue(new LightBFSNode(lightRemoveUpdate));

        while (fringe.Count > 0)
        {
            LightBFSNode node = fringe.Dequeue();

            // Remove this blocks light. It doesn't have any light sources near it.
            WorldLightLevels.SetPixelv(node.WorldPosition, Colors.Black);

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
                    _lightUpdateRemoveToAddQueue.Enqueue(new LightUpdate(neighbourPosition, neighboursLevel));
                }
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
        // lightUpdateQueueMutex.Lock();
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
        // lightUpdateQueueMutex.Unlock();

        
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
