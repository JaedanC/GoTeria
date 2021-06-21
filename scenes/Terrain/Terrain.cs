using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Terrain : Node2D
{
    [Export]
    private readonly Vector2 _blockPixelSize = new Vector2(16, 16);
    [Export]
    // private readonly Vector2 _chunkBlockCount = new Vector2(420, 400);
    private readonly Vector2 _chunkBlockCount = new Vector2(168, 160);
    private const int _loadMargin = 2;
    private const int _drawMargin = 1;

    private ThreadPool _threadPool;
    private Player _player;
    private InputLayering _inputLayering;
    private LightingEngine _lightingEngine;
    private TerrainStack _terrainStack;

    private Mutex _loadedChunksMutex;
    private Vector2 _chunkPixelDimensions;
    private Dictionary<Vector2, Chunk> _loadedChunks;
    private Dictionary<Vector2, Chunk> _urgentChunks;
    private Dictionary<Vector2, Chunk> _lightDrawChunks;
    private Dictionary<Vector2, Chunk> _lightLoadingChunks;
    private ObjectPool<Chunk> chunkPool;

    /* Returns the size of a block in pixels as a Vector2 */
    public Vector2 BlockPixelSize { get { return _blockPixelSize; } }
    public Vector2 ChunkPixelDimensions { get { return _chunkPixelDimensions; } }
    public Vector2 ChunkBlockCount { get { return _chunkBlockCount; } }
    public Image WorldBlocksImage { get { return _terrainStack.GetWorldBlocksImage(); } }
    public Image WorldWallsImage { get { return _terrainStack.GetWorldWallsImage(); } }
    public LightingEngine LightingEngine { get { return _lightingEngine; } }
    
    private MultithreadedChunkLoader chunkLoader;

    public override void _Ready()
    {
        Name = "Terrain";
        _chunkPixelDimensions = BlockPixelSize * ChunkBlockCount;

        Debug.Assert(BlockPixelSize.x > 0, "BlockPixelSize.x is 0");
        Debug.Assert(BlockPixelSize.y > 0, "BlockPixelSize.y is 0");
        Debug.Assert(ChunkBlockCount.y > 0, "ChunkBlockCount.y is 0");
        Debug.Assert(ChunkBlockCount.y > 0, "ChunkBlockCount.y is 0");

        _loadedChunksMutex = new Mutex();
        _loadedChunks = new Dictionary<Vector2, Chunk>();
        _urgentChunks = new Dictionary<Vector2, Chunk>();
        _lightDrawChunks = new Dictionary<Vector2, Chunk>();
        _lightLoadingChunks = new Dictionary<Vector2, Chunk>();

        _threadPool = GetNode<ThreadPool>("/root/ThreadPool");
        _player = GetNode<Player>("/root/WorldSpawn/Player");
        _inputLayering = GetNode<InputLayering>("/root/InputLayering");
        _lightingEngine = GetNode<LightingEngine>("Lighting");

        // TODO: dynamically load the world
        String worldName = "default";
        // String worldName = "light_test";
        _terrainStack = new TerrainStack(
            "res://saves/worlds/" + worldName + "/blocks.png",
            "res://saves/worlds/" + worldName + "/walls.png"
        );
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/save_image.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/blocks.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorld.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorldAlpha.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/small.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/medium.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/hd.png");

        _lightingEngine.Initialise();
        chunkPool = new ObjectPool<Chunk>(15, ChunkBlockCount);
        chunkLoader = new MultithreadedChunkLoader(ChunkBlockCount, _threadPool, WorldBlocksImage, WorldWallsImage);
        
        // WorldBlocksImageLuminance.Unlock();
    }

    public override void _Process(float _delta)
    {
        // Save the all chunks loaded in memory to a file.
        // TODO: This won't work
        if (_inputLayering.PopAction("save_world"))
        {
            foreach (Chunk chunk in GetChildren())
            {
                chunk.SaveChunk();
            }
            GD.Print("Finished Saving to file");
        }
        
        _loadedChunksMutex.Lock();
        DeleteInvisibleChunks();
        LoadVisibleChunks();
        _loadedChunksMutex.Unlock();
        CreateChunkStreamingRegions();
        ContinueStreamingRegions();
        
        // Draw the chunk borders
        Update();
    }

    // public override void _Notification(int what)
    // {
    //     if (what == MainLoop.NotificationPredelete)
    //     {
    //         worldImage.SavePng("res://save_image.png");
    //     }
    // }

    /* This currently colours the chunks with a border donoting the kind of chunk it
    is and how it should be streamed in. Reducing the viewport_rectangle in the
    Player.get_visibility_points method will allow you to see this process in action. */
    public override void _Draw()
    {
        int thickness = 10;
	    foreach (Vector2 point in _lightLoadingChunks.Keys)
            DrawRect(new Rect2(point * _chunkPixelDimensions, _chunkPixelDimensions), Colors.Green, false, thickness, false);

        foreach (Vector2 point in _lightDrawChunks.Keys)
            DrawRect(new Rect2(point * _chunkPixelDimensions, _chunkPixelDimensions), Colors.Orange, false, thickness, false);
			
        foreach (Vector2 point in _urgentChunks.Keys)
		    DrawRect(new Rect2(point * _chunkPixelDimensions, _chunkPixelDimensions), Colors.Red, false, thickness, false);

    }

    /* This method is run on the threadPool, and it generates all the data a chunk requires
    for it to work. This function can only take one paramater, thus we take in an away of objects.
    We return the chunkPosition so that we can retrieve the Chunk from the threadPool forcefully
    if it is taking too much time, and the player is getting too close. */
    public Vector2 CreateChunk(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        chunk.Create(chunkPosition, _chunkBlockCount, WorldBlocksImage, WorldWallsImage);
        
	    return chunkPosition;
    }

    /* This method is run on the threadPool, and it generates all the data a chunk requires
    for it to work. This function can only take one paramater, thus we take in an away of objects.
    We return the chunkPosition so that we can retrieve the Chunk from the threadPool forcefully
    if it is taking too much time, and the player is getting too close. */
    public Vector2 ComputeChunkLighting(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        chunk.ComputeLightingPass();
        
	    return chunkPosition;
    }

    /* This method creates chunks (from the ObjectPool) that are visible to the player. These
    chunks are stored in the loadedChunks Dictionary. */
    private void LoadVisibleChunks()
    {
        Array<Vector2> visibleChunkPositions = _player.GetVisibilityChunkPositions(_loadMargin + _drawMargin);
        foreach (Vector2 chunkPosition in visibleChunkPositions)
        {
            Vector2 worldImageInChunks = WorldBlocksImage.GetSize() / ChunkBlockCount;
            if (chunkPosition.x < 0 || chunkPosition.y < 0 ||
                chunkPosition.x >= worldImageInChunks.x ||
                chunkPosition.y >= worldImageInChunks.y)
            {
                continue;
            }
            
            // Only create chunks that have not already been loaded in
            if (!_loadedChunks.ContainsKey(chunkPosition))
            {
                Chunk chunk = chunkPool.GetInstance(WorldBlocksImage, chunkPosition, BlockPixelSize, ChunkBlockCount);
                AddChild(chunk);
                _loadedChunks[chunkPosition] = chunk;
            }
        }
    }

    /* This method uses the visibility points to determine which chunks should be
    unloaded from memory. */
    private void DeleteInvisibleChunks()
    {
        // First we grab a set the world_positions that should be loaded in the game 
        Array<Vector2> visibleChunkPositions = _player.GetVisibilityChunkPositions(_loadMargin + _drawMargin);

        // Create a temporary dictionary to store chunks that are already loaded
        // and should stay loaded
        Dictionary<Vector2, Chunk> visibleChunks = new Dictionary<Vector2, Chunk>();

        // Loop through the loaded chunks and store the ones that we should keep in
        // visible_chunks while erasing them from the old loaded_chunks dictionary. 
        foreach (Vector2 visiblePoint in visibleChunkPositions)
        {
            if (_loadedChunks.ContainsKey(visiblePoint))
            {
                visibleChunks.Add(visiblePoint, _loadedChunks[visiblePoint]);
                _loadedChunks.Remove(visiblePoint);
            }
        }

        // Now the remaining chunks in loaded_chunks are invisible to the player
        // and can be deleted from memory.
        foreach (Vector2 invisibleChunkPosition in _loadedChunks.Keys)
        {
            Chunk chunk = _loadedChunks[invisibleChunkPosition];
            RemoveChild(chunk);
            chunkPool.Die(chunk);
            _loadedChunks.Remove(invisibleChunkPosition);
        }

        // Finally, our new visible chunks dictionary becomes the loaded chunks
        // dictionary
        _loadedChunks = visibleChunks;

        // Also reset the region loading dictionaries. These will be repopulated
        // later.
        _urgentChunks.Clear();
        _lightDrawChunks.Clear();
        _lightLoadingChunks.Clear();
    }
	
    private void CreateChunkStreamingRegions()
    {
        Array<Vector2> urgentVisibilityPoints = _player.GetVisibilityChunkPositions();
        Array<Vector2> drawVisibilityPoints = _player.GetVisibilityChunkPositions(_drawMargin, true);
        Array<Vector2> loadVisibilityPoints = _player.GetVisibilityChunkPositions(_loadMargin + _drawMargin, true, _drawMargin);

        // GD.Print("Urgent:", urgentVisibilityPoints);
        // GD.Print("Load:", loadVisibilityPoints);

        foreach (Vector2 point in urgentVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                _urgentChunks.Add(point, chunk);
        }
        
        foreach (Vector2 point in drawVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                _lightDrawChunks.Add(point, chunk);
        }

        foreach (Vector2 point in loadVisibilityPoints)
        {
        Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                _lightLoadingChunks.Add(point, chunk);
        }
    }

    /* This method continues to load in the chunks based on the three regions
    outlined at the top of this script. To tweak performance consider changing
    these variables:
        - blocks_to_load. Blocks to stream in every frame. Should be ~around the
        number of blocks in a chunk.
        - chunks_to_draw. The number of chunks to draw every frame. Set to 0 and no
        chunks will be streamed. This should be set 1 but experiment with more
        values.
        - draw_margin. This is the number of extra chunk layers outside the view of
        screen that will drawn (by streaming) such that a player moving into new
        chunks won't experience lag spikes
        - load_margin. This is the number of extra chunk layers past the draw_margin
        that will only stream in blocks.
        - 'Chunk Block Count'. This is the number of blocks in each chunk. This
        should be set to a reasonable value like (16, 16) or (32, 32). Experiment
        with others.

    The premise behind these dictionaries are to improve performance when streaming
    in chunks behind the scenes.
        - lightly_loading_blocks_chunks. These are slowly loading in their blocks
        over a series of frames. These happen from a large distance away. Mediumly
        off the screen.
        - lightly_loading_drawing_chunks. These chunks are drawing their blocks.
        These happen from a short distance away, just off the screen
        - urgently_loading_chunks. These chunks are so close to the player that
        they need to have their blocks loaded in right away. The drawing can happen
        later though
        - loaded_chunks. These chunks are fully drawn and visible to the player.

    Tweaking these values on different computers may result in better performance.
    TODO: Make them editable in a configuration file in the future. */
    private void ContinueStreamingRegions()
    {
        // Loading chunks
        Array<Vector2> chunksToLoad = new Array<Vector2>(_urgentChunks.Keys) +
                                      new Array<Vector2>(_lightDrawChunks.Keys) + 
                                      new Array<Vector2>(_lightLoadingChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToLoad)
        {
            Chunk chunk = _loadedChunks[chunkPosition];

            if (!chunk.Loaded)
                chunkLoader.beginLoadingChunk(chunk, chunkPosition);
        }

        // Force load close chunks
        Array<Vector2> chunksToForceLoad = new Array<Vector2>(_urgentChunks.Keys) +
                                           new Array<Vector2>(_lightDrawChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToForceLoad)
        {
            chunkLoader.FinishLoadingChunkForcefully(chunkPosition);
        }

        chunkLoader.GetFinishedLoadingChunks(_loadedChunks);

        // Lighting chunks
        Array<Vector2> chunksToLight = new Array<Vector2>(_urgentChunks.Keys) +
                                       new Array<Vector2>(_lightDrawChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToLight)
        {
            Chunk chunk = _loadedChunks[chunkPosition];

            if (chunk.Loaded && !chunk.LightingDone)
                chunkLoader.beginLightingChunk(chunk, _loadedChunks);
        }
        
        // Force light close chunks
        Array<Vector2> chunksToForceLight = new Array<Vector2>(_urgentChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToForceLight)
        {
            chunkLoader.FinishLightingChunkForcefully(chunkPosition);
        }

        // Draw ready chunks
        foreach (Chunk chunk in chunkLoader.GetFinishedLightingChunks(_loadedChunks))
        {
            Debug.Assert(chunkLoader.GetChunkPhase(chunk) == MultithreadedChunkLoader.LoadingPhase.ReadyToDraw);
            chunk.Update();
        }
    }

    /* Returns the size of the world in blocks as a Vector2. TODO: Change to use the
    worldSizeInChunks variable when we create our own world. */
    public Vector2 GetWorldSize()
    {
	    return WorldBlocksImage.GetSize();
        // return worldSizeInChunks;
    }

    /* Returns a Chunk if it exists at the given chunk_position in the world.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.

    Fastest function to get chunks.*/
    public Chunk GetChunkFromChunkPosition(Vector2 chunkPosition)
    {
        // if (CreateChunk(chunkPosition))
        Chunk chunk = null;

        _loadedChunksMutex.Lock();
        if (_loadedChunks.ContainsKey(chunkPosition))
            chunk = _loadedChunks[chunkPosition];
        _loadedChunksMutex.Unlock();

        return chunk;
    }
	
	/* Returns a Chunk if it exists at the given world_position in the world.
    World positions are locations represented by pixels. Entities in the world
    are stored using this value.

    Slowest function to get chunks.*/
    public Chunk GetChunkFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        return GetChunkFromChunkPosition(chunkPosition);
    }
	
	/* Returns a chunk position from the given world_position.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
    index in the world.

    Fastest function to get chunk positions.*/
    public Vector2 GetChunkPositionFromWorldPosition(Vector2 worldPosition)
    {
	    return (worldPosition / _chunkPixelDimensions).Floor();
    }

    /* Returns a block if it exists using the given chunk_position and block_position
    values.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
        - Block positions are the position of the block relative to the chunk it is
        in. It cannot be larger than the chunk's block_size.
    Blocks are Dictionaries containing a set of standard variables. See the Block
    documentation in the Chunk scene.

    Fastest function to get blocks.*/
    public Block GetBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return null;
        if (!chunk.Loaded)
            return null;
        return chunk.GetBlockFromBlockPosition(blockPosition);
    }

    public IBlock GetTopIBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return null;
        if (!chunk.Loaded)
            return null;
        return chunk.GetTopIBlockFromBlockPosition(blockPosition);
    }

    public IBlock GetTopIBlockFromWorldBlockPosition(Vector2 worldBlockPosition)
    {
        Vector2 chunkPosition = (worldBlockPosition / _chunkBlockCount).Floor();
        Vector2 blockPosition = worldBlockPosition - chunkPosition * _chunkBlockCount;
        return GetTopIBlockFromChunkPositionAndBlockPosition(chunkPosition, blockPosition);
    }
		
    /* Returns a block if it exists using the given world_position.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Blocks are Dictionaries containing a set of standard variables. See the Block
    documentation in the Chunk scene.

    Slowest function to get blocks.*/
    public Block GetBlockFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        Vector2 blockPosition = GetBlockPositionFromWorldPositionAndChunkPosition(worldPosition, chunkPosition);
        return GetBlockFromChunkPositionAndBlockPosition(chunkPosition, blockPosition);
    }
	
	/* Returns a block position using the given world_position and chunk_position
    values.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
    Block positions are the position of the block relative to the chunk it is in. It
    cannot be larger than the chunk's block_size.

    Fastest function to get block positions.*/
    public Vector2 GetBlockPositionFromWorldPositionAndChunkPosition(Vector2 worldPosition, Vector2 chunkPosition)
    {
        Vector2 blockPosition = (worldPosition - chunkPosition * _chunkPixelDimensions).Floor();
        return (blockPosition / BlockPixelSize).Floor();
    }
	
	/* Returns a block position using the given world_position.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Block positions are the position of the block relative to the chunk it is in. It
    cannot be larger than the chunk's block_size.

    Slowest function to get block positions. */
    public Vector2 GetBlockPositionFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        return GetBlockPositionFromWorldPositionAndChunkPosition(worldPosition, chunkPosition);
    }

    /* Sets a block at the given chunk_position and block_position to be the
    new_block if it exists.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
        - Block positions are the position of the block relative to the chunk it is
        in. It cannot be larger than the chunk's block_size.
        - Blocks are Dictionaries containing a set of standard variables. See the
        Block documentation in the Chunk scene.

    Fastest function to set blocks. */
    public void SetBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition, Block newBlock)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return;
        chunk.SetBlockFromBlockPosition(blockPosition, newBlock);
        Vector2 worldBlockPosition = chunkPosition * ChunkBlockCount + blockPosition;
        SetWorldImage(worldBlockPosition, newBlock.Colour);
    }

	/* Sets a block at the given world_position to be the new_block if it exists.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
        - Blocks are Dictionaries containing a set of standard variables. See the
        Block documentation in the Chunk scene.

    Slowest function to set blocks. */
    public void SetBlockAtWorldPosition(Vector2 worldPosition, Block newBlock)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        Vector2 blockPosition = GetBlockPositionFromWorldPositionAndChunkPosition(worldPosition, chunkPosition);
        SetBlockFromChunkPositionAndBlockPosition(chunkPosition, blockPosition, newBlock);
    }

    private void SetWorldImage(Vector2 worldBlockPosition, Color colour)
    {
        if (worldBlockPosition.x < 0 || worldBlockPosition.y < 0 ||
                worldBlockPosition.x >= WorldBlocksImage.GetWidth() ||
                worldBlockPosition.y >= WorldBlocksImage.GetHeight())
            return;
        WorldBlocksImage.SetPixelv(worldBlockPosition, colour);

        CheckIfUpdateLighting(worldBlockPosition, colour);
    }

    private void CheckIfUpdateLighting(Vector2 worldBlockPosition, Color colour)
    {
        Color coloursLightValue;
        if (Helper.IsLight(colour))
            coloursLightValue = Colors.White;
        else
            coloursLightValue = Colors.Black;

        Color existingLightValue = _lightingEngine.WorldLightSources.GetPixelv(worldBlockPosition);
        
        // Just remove and re-add the light.
        if (existingLightValue != colour){
            _lightingEngine.RemoveLight(worldBlockPosition);
        }
    }
}
