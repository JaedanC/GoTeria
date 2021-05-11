using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;

public class Terrain : Node2D
{
    // [Export]
    private Vector2 blockPixelSize = new Vector2(16, 16);
    // [Export]
    private Vector2 chunkBlockCount = new Vector2(256, 256);
    private int loadMargin = 1;
    private int drawMargin = 0;

    private ThreadPool threadPool;
    private Player player;
    private Godot.Object lighting;
    private InputLayering inputLayering;

    private Vector2 chunkPixelDimensions;
    private Dictionary<Vector2, Chunk> loadedChunks;
    private Dictionary<Vector2, Chunk> urgentlyLoadingBlocksChunks;
    private Dictionary<Vector2, Chunk> lightlyLoadingDrawingChunks;
    private Dictionary<Vector2, Chunk> lightlyLoadingBlocksChunks;
    private Image worldImage;
    private Image worldImageLuminance;
    // private Image worldImageLightSources;
    private ObjectPool<Chunk> chunkPool;


    public override void _Ready()
    {
        Name = "Terrain";
        chunkPixelDimensions = blockPixelSize * chunkBlockCount;

        Debug.Assert(blockPixelSize.x > 0, "blockPixelSize.x is 0");
        Debug.Assert(blockPixelSize.y > 0, "blockPixelSize.y is 0");
        Debug.Assert(chunkBlockCount.y > 0, "chunkBlockCount.y is 0");
        Debug.Assert(chunkBlockCount.y > 0, "chunkBlockCount.y is 0");

        loadedChunks = new Dictionary<Vector2, Chunk>();
        urgentlyLoadingBlocksChunks = new Dictionary<Vector2, Chunk>();
        lightlyLoadingDrawingChunks = new Dictionary<Vector2, Chunk>();
        lightlyLoadingBlocksChunks = new Dictionary<Vector2, Chunk>();

        threadPool =    GetNode<ThreadPool>("/root/ThreadPool");
        player =        GetNode<Player>("/root/WorldSpawn/Player");
        lighting =      GetNode<Godot.Object>("Lighting");
        inputLayering = GetNode<InputLayering>("/root/InputLayering");

        // TODO: dynamically load the world
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/save_image.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/blocks.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorld.png");
        Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorldAlpha.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/small.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/medium.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/hd.png");
        worldImage = worldTexture.GetData();
        worldImage.Lock();
        worldImageLuminance = new Image();
        worldImageLuminance.Create(worldImage.GetWidth(), worldImage.GetHeight(), false, Image.Format.Rgba8);
        worldImageLuminance.Fill(Colors.Red);
        worldImageLuminance.Lock();
        for (int i = 0; i < worldImageLuminance.GetWidth(); i++)
        for (int j = 0; j < worldImageLuminance.GetHeight(); j++)
        {
            Color colour = worldImage.GetPixel(i, j);
            if (colour.a == 0)
                worldImageLuminance.SetPixel(i, j, Colors.White);
            else
                worldImageLuminance.SetPixel(i, j, Colors.Black);
        }

        // TODO: initialise with a better number
        chunkPool = new ObjectPool<Chunk>(10);

        // GenerateWorld()
    }

    public override void _Process(float _delta)
    {
        // Save the all chunks loaded in memory to a file.
        // TODO: This won't work
        if (inputLayering.PopAction("save_world"))
        {
            foreach (Chunk chunk in GetChildren())
            {
                chunk.SaveChunk();
            }
            GD.Print("Finished Saving to file");
        }
        
        DeleteInvisibleChunks();
        LoadVisibleChunks();
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
	    foreach (Vector2 point in lightlyLoadingBlocksChunks.Keys)
        {
            DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Green, false, thickness, false);
        }

        foreach (Vector2 point in lightlyLoadingDrawingChunks.Keys)
        {
            DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Orange, false, thickness, false);
        }
			
        foreach (Vector2 point in urgentlyLoadingBlocksChunks.Keys)
        {
		    DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Red, false, thickness, false);
        }
    }

    /* This method is run on the threadPool, and it generates all the data a chunk requires
    for it to work. This function can only take one paramater, thus we take in an away of objects.
    We return the chunkPosition so that we can retrieve the Chunk from the threadPool forcefully
    if it is taking too much time, and the player is getting too close. */
    public Vector2 GetChunkData(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        int numberOfBlocks = (int)(chunkBlockCount.x * chunkBlockCount.y);

        // TODO: Use an Array instead
        Dictionary<String, object>[] blocks = new Dictionary<String, object>[numberOfBlocks];
        // Array<Dictionary<String, object>> blocks = new Array<Dictionary<String, object>>();

        // This is to check if the next thread that could have been waiting is
        // accidentally about to do work a second time.
        Image chunkImage;
        // Create a local image so multiple threads don't race to write to the
	    // instance variable chunk_image.
        chunkImage = new Image();
        chunkImage.Create((int)chunkBlockCount.x, (int)chunkBlockCount.y, false, Image.Format.Rgba8);
        chunkImage.BlitRect(worldImage, new Rect2(chunkPosition * chunkBlockCount, chunkBlockCount), Vector2.Zero);
        for (int j = 0; j < chunkBlockCount.y; j++)
        for (int i = 0; i < chunkBlockCount.x; i++)
        {
            Vector2 blockPosition = new Vector2(i, j);
            Vector2 worldBlockPosition = chunkPosition * chunkBlockCount + blockPosition;

            // Grab the colour for the pixel from the world image. If the pixel
            // goes out of bounds then just draw Red. This happens when the image is
            // not a multiple of the chunk size.
            Color pixel;
            if (worldBlockPosition.x < 0 || worldBlockPosition.y < 0 || 
                worldBlockPosition.x >= worldImage.GetWidth() ||
                worldBlockPosition.y >= worldImage.GetHeight())
            {
                pixel = Colors.Red;
            }
            else
            {
                pixel = worldImage.GetPixelv(worldBlockPosition);
            }

			Dictionary<String, object> block = new Dictionary<String, object>();
            if (pixel.a == 0)
                block.Add("id", 0);
            else
                block.Add("id", 1);

            // Now using chunkImage
            // block.Add("colour", pixel);

            int blockIndex = chunk.BlockPositionToBlockIndex(blockPosition);
			blocks[blockIndex] = block;
        }
	    
        chunk.ObtainChunkData(blocks, chunkImage);
	    return chunkPosition;
    }

    /* This method creates chunks (from the ObjectPool) that are visible to the player. These
    chunks are stored in the loadedChunks Dictionary. */
    private void LoadVisibleChunks()
    {
        Array<Vector2> visibleChunkPositions = player.GetVisibilityChunkPositions(loadMargin + drawMargin);
        foreach (Vector2 chunkPosition in visibleChunkPositions)
        {
            Vector2 worldImageInChunks = worldImage.GetSize() / chunkBlockCount;
            if (chunkPosition.x < 0 || chunkPosition.y < 0 ||
                chunkPosition.x >= worldImageInChunks.x ||
                chunkPosition.y >= worldImageInChunks.y)
            {
                continue;
            }
            
            // Only create chunks that have not already been loaded in
            if (!loadedChunks.ContainsKey(chunkPosition))
            {
                Chunk chunk = chunkPool.GetInstance(worldImage, chunkPosition, chunkBlockCount, blockPixelSize);
                AddChild(chunk);
                loadedChunks[chunkPosition] = chunk;
            }
        }
    }

    /* This method uses the visibility points to determine which chunks should be
    unloaded from memory. */
    private void DeleteInvisibleChunks()
    {
        // First we grab a set the world_positions that should be loaded in the game 
        Array<Vector2> visibleChunkPositions = player.GetVisibilityChunkPositions(loadMargin + drawMargin);

        // Create a temporary dictionary to store chunks that are already loaded
        // and should stay loaded
        Dictionary<Vector2, Chunk> visibleChunks = new Dictionary<Vector2, Chunk>();

        // Loop through the loaded chunks and store the ones that we should keep in
        // visible_chunks while erasing them from the old loaded_chunks dictionary. 
        foreach (Vector2 visiblePoint in visibleChunkPositions)
        {
            if (loadedChunks.ContainsKey(visiblePoint))
            {
                visibleChunks.Add(visiblePoint, loadedChunks[visiblePoint]);
                loadedChunks.Remove(visiblePoint);
            }
        }

        // Now the remaining chunks in loaded_chunks are invisible to the player
        // and can be deleted from memory.
        foreach (Vector2 invisibleChunkPosition in loadedChunks.Keys)
        {
            Chunk chunk = loadedChunks[invisibleChunkPosition];
            RemoveChild(chunk);
            chunkPool.Die(chunk);
            // FreeChunk(chunk);
            loadedChunks.Remove(invisibleChunkPosition);
        }

        // Finally, our new visible chunks dictionary becomes the loaded chunks
        // dictionary
        loadedChunks = visibleChunks;

        // Also reset the region loading dictionaries. These will be repopulated
        // later.
        urgentlyLoadingBlocksChunks.Clear();
        lightlyLoadingDrawingChunks.Clear();
        lightlyLoadingBlocksChunks.Clear();
    }
	
    private void CreateChunkStreamingRegions()
    {
        Array<Vector2> urgentVisibilityPoints = player.GetVisibilityChunkPositions();
        Array<Vector2> drawVisibilityPoints = player.GetVisibilityChunkPositions(drawMargin, true);
        Array<Vector2> loadVisibilityPoints = player.GetVisibilityChunkPositions(loadMargin + drawMargin, true, drawMargin);

        // GD.Print("Urgent:", urgentVisibilityPoints);
        // GD.Print("Load:", loadVisibilityPoints);

        foreach (Vector2 point in urgentVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                urgentlyLoadingBlocksChunks.Add(point, chunk);
        }
        
        foreach (Vector2 point in drawVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                lightlyLoadingDrawingChunks.Add(point, chunk);
        }

        foreach (Vector2 point in loadVisibilityPoints)
        {
        Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                lightlyLoadingBlocksChunks.Add(point, chunk);
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
	    Array<Vector2> forceLoad = new Array<Vector2>();
        
        // First we check if the chunks need to be forced to load their blocks
        Array<Vector2> chunkPoints = new Array<Vector2>(urgentlyLoadingBlocksChunks.Keys) +
                                    new Array<Vector2>(lightlyLoadingDrawingChunks.Keys);
        foreach (Vector2 chunkPoint in chunkPoints)
        {
            Chunk chunk = loadedChunks[chunkPoint];
            if (!chunk.IsLoaded())
            {
                forceLoad.Add(chunkPoint);
            }
        }

        // Next we check the draw conditions
        int chunksToDraw = 1;
        foreach (Chunk chunk in urgentlyLoadingBlocksChunks.Values)
        {
            if (!chunk.IsDrawn())
            {
                chunk.Update();
                chunksToDraw -= 1;
            }
        }
        foreach (Chunk chunk in lightlyLoadingDrawingChunks.Values)
        {
            if (chunksToDraw <= 0)
                break;
            if (!chunk.IsDrawn())
            {
                chunk.Update();
                chunksToDraw -= 1;
            }
        }

        // Finally, start the thread pool with the next batch as long as they haven't
        // been locked already. Don't start the chunks again that were found to have
        // finished in the thread pool.
        Array<Vector2> chunksToLoad = new Array<Vector2>(lightlyLoadingBlocksChunks.Keys) + forceLoad;

        foreach (Vector2 chunkPoint in chunksToLoad)
        {
            Chunk chunk = loadedChunks[chunkPoint];
            if (chunk.IsLocked() || chunk.IsLoaded())
                continue;

            chunk.Lock();
            Array<object> chunkData = new Array<object>{
                chunkPoint,
                chunk
            };
            threadPool.SubmitTask(this, "GetChunkData", chunkData, "chunk", chunkPoint);
        }

        // Next, we obtain the completed blocks that have loaded and start the
        // next batch of block loads. During this process, wait until the force_load
        // blocks have been done as they are required to be completed by now.
        foreach (Vector2 chunkPosition in forceLoad)
        {
            threadPool.WaitForTaskSpecific(chunkPosition);
            // Now all tasks that need to be done should be ready
        }
        
        // Obtain the tasks completed that has the chunks that are being
        // loaded inside the thread pool. If they have completed, then we can send
        // this data to the chunk to automatically mark it as completed.
        Godot.Collections.Array completedChunkTasks = (Godot.Collections.Array)threadPool.FetchFinishedTasksByTag("chunk");
        foreach (Task completedChunkTask in completedChunkTasks)
        {
            Vector2 chunkPoint = (Vector2)completedChunkTask.GetResult();
            // In the case that the chunk has already been freed, don't assign the
            // chunk any data.
            if (!loadedChunks.ContainsKey(chunkPoint))
                continue;

            // Give the chunk it's data
            Chunk chunk = loadedChunks[chunkPoint];
            chunk.Update();
        }
    }

    public Vector2 GetChunkPixelDimensions()
    {
	    return chunkPixelDimensions;
    }

    /* Returns the size of the world in blocks as a Vector2. TODO: Change to use the
    worldSizeInChunks variable when we create our own world. */
    public Vector2 GetWorldSize()
    {
	    return worldImage.GetSize();
        // return worldSizeInChunks;
    }
	
    /* Returns the size of a chunk in blocks as a Vector2 */
	public Vector2 GetChunkBlockCount()
    {
	    return chunkBlockCount;
    }

    /* Returns the size of a block in pixels as a Vector2 */
	public Vector2 GetBlockPixelSize()
    {
	    return blockPixelSize;
    }

    /* Returns a Chunk if it exists at the given chunk_position in the world.
	- Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
	index in the world.

    Fastest function to get chunks.*/
    public Chunk GetChunkFromChunkPosition(Vector2 chunkPosition)
    {
        // if (CreateChunk(chunkPosition))
        if (loadedChunks.ContainsKey(chunkPosition))
        {
            return loadedChunks[chunkPosition];
        }
        return null;
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
	    return (worldPosition / GetChunkPixelDimensions()).Floor();
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
    public Dictionary<String, object> GetBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return null;
        if (!chunk.IsLoaded())
            return null;
        return chunk.GetBlockFromBlockPosition(blockPosition);
    }
		
    /* Returns a block if it exists using the given world_position.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Blocks are Dictionaries containing a set of standard variables. See the Block
    documentation in the Chunk scene.

    Slowest function to get blocks.*/
    public Dictionary<String, object> GetBlockFromWorldPosition(Vector2 worldPosition)
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
        Vector2 blockPosition = (worldPosition - chunkPosition * GetChunkPixelDimensions()).Floor();
        return (blockPosition / blockPixelSize).Floor();
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
    public void SetBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition, Dictionary<String, object> newBlock)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return;
        chunk.SetBlockFromBlockPosition(blockPosition, newBlock);
        Vector2 worldBlockPosition = chunkPosition * chunkBlockCount + blockPosition;
        SetWorldImage(worldBlockPosition, (Color)newBlock["colour"]);
    }

	/* Sets a block at the given world_position to be the new_block if it exists.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
        - Blocks are Dictionaries containing a set of standard variables. See the
        Block documentation in the Chunk scene.

    Slowest function to set blocks. */
    public void SetBlockAtWorldPosition(Vector2 worldPosition, Dictionary<String, object> newBlock)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        Vector2 blockPosition = GetBlockPositionFromWorldPositionAndChunkPosition(worldPosition, chunkPosition);
        SetBlockFromChunkPositionAndBlockPosition(chunkPosition, blockPosition, newBlock);
    }

    public Image GetWorldImageLuminance()
    {
        return worldImageLuminance;
    }

    private void SetWorldImage(Vector2 worldBlockPosition, Color colour)
    {
        if (worldBlockPosition.x < 0 || worldBlockPosition.y < 0 ||
                worldBlockPosition.x >= worldImage.GetWidth() ||
                worldBlockPosition.y >= worldImage.GetHeight())
            return;
        
        worldImage.SetPixelv(worldBlockPosition, colour);
        if (colour.a == 0)
            worldImageLuminance.SetPixelv(worldBlockPosition, Colors.White);
        else
            worldImageLuminance.SetPixelv(worldBlockPosition, Colors.Black);
    }
}
