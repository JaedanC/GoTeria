using Godot;
using Godot.Collections;
using System;

public class Terrain : Node2D
{
    // [Export]
    private Vector2 blockPixelSize;

    // [Export]
    private Vector2 chunkBlockCount;

    private ThreadPool threadPool;
    private Player player;
    private Godot.Object lighting;
    private Godot.Object inputLayering;

    private Vector2 chunkPixelDimensions;
    private Dictionary<Vector2, Chunk> loadedChunks;
    private Dictionary<Vector2, Chunk> urgentlyLoadingBlocksChunks;
    private Dictionary<Vector2, Chunk> lightlyLoadingDrawingChunks;
    private Dictionary<Vector2, Chunk> lightlyLoadingBlocksChunks;
    private Array<Chunk> deactivatedAndReusableChunks;

    private Image worldImage;
    private Image worldImageLuminance;
    // private Image worldImageLightSources;
    /* Returns the size of a chunk in pixels as a Vector2. */

    private ObjectPool<Chunk> chunkPool;

    private int loadMargin = 1;
    private int drawMargin = 0;


    public override void _Ready()
    {
        Name = "Terrain";
        blockPixelSize = new Vector2(16, 16);
        chunkBlockCount = new Vector2(256, 256);

        if (blockPixelSize.x == 0 || blockPixelSize.y == 0)
        {
            throw new Exception("blockPixelSize is 0.");
        }

        loadedChunks = new Dictionary<Vector2, Chunk>();
        urgentlyLoadingBlocksChunks = new Dictionary<Vector2, Chunk>();
        lightlyLoadingDrawingChunks = new Dictionary<Vector2, Chunk>();
        lightlyLoadingBlocksChunks = new Dictionary<Vector2, Chunk>();
        deactivatedAndReusableChunks = new Array<Chunk>();

        threadPool = GetNode<ThreadPool>("/root/WorldSpawn/ThreadPool");
        player = GetNode<Player>("/root/WorldSpawn/Player");
        lighting = GetNode<Godot.Object>("Lighting");
        inputLayering = GetTree().Root.GetNode("InputLayering");

        // TODO: dynamically load the world
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/save_image.png");
        Texture worldTexture = (Texture)GD.Load("res://saves/worlds/blocks.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorld.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/LargeWorldAlpha.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/small.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/medium.png");
        // Texture worldTexture = (Texture)GD.Load("res://saves/worlds/hd.png");
        worldImage = worldTexture.GetData();
        worldImage.Lock();

        chunkPixelDimensions = blockPixelSize * chunkBlockCount;

        worldImageLuminance = new Image();
        worldImageLuminance.Create(worldImage.GetWidth(), worldImage.GetHeight(), false, Image.Format.Rgba8);
        worldImageLuminance.Fill(Colors.Red);
        worldImageLuminance.Lock();
        for (int i = 0; i < worldImageLuminance.GetWidth(); i++)
        for (int j = 0; j < worldImageLuminance.GetHeight(); j++)
        {
            Color colour = worldImage.GetPixel(i, j);
            if (colour.a == 0)
            {
                worldImageLuminance.SetPixel(i, j, Colors.White);
            }
            else
            {
                worldImageLuminance.SetPixel(i, j, Colors.Black);
            }

        }

        // TODO: initialise with a better number
        chunkPool = new ObjectPool<Chunk>(5);

        GD.Print("Hello");
        // GenerateWorld()
    }

    public override void _Process(float _delta)
    {
        // Save the all chunks loaded in memory to a file.
        // TODO: This won't work
        if ((bool)inputLayering.Call("pop_action", "save_world"))
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

    /* This method frees a chunk and removes it from this node as a child. The Chunk
    is not freed unless the amount of chunks that are queued to be reset exceeds the
    value maximum_deactivated_and_reusable_chunks. Instead, it is reset and stored
    such that when a new chunk is created one of these chunks can be used instead
    to decrease the number of memory allocations. This technique provides a large
    speed up in performance. */
    // private void FreeChunk(Chunk chunk)
    // {
	//     RemoveChild(chunk);
    //     deactivatedAndReusableChunks.Add(chunk);
    // }

    public Vector2 GetChunkData(Array<object> data)
    {
        Vector2 chunkPosition = (Vector2)data[0];
        Chunk chunk = (Chunk)data[1];

        int numberOfBlocks = (int)(chunkBlockCount.x * chunkBlockCount.y);

        // TODO: Use an Array instead
        Array<Dictionary<String, object>> blocks = new Array<Dictionary<String, object>>();

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

            // TODO: Use an Array instead
            blocks.Add(block);
			// blocks[blockIndex] = block;
        }
	    
        chunk.ObtainChunkData(blocks, chunkImage);

	    return chunkPosition;
    }
			
	// private Chunk CreateChunk(Vector2 chunkPosition)
    // {
    //     Chunk chunk;
    //     if (deactivatedAndReusableChunks.Count != 0)
    //     {
    //         // GD.Print("Reusing: ", chunkPosition);
    //         chunk = deactivatedAndReusableChunks[0];
    //         deactivatedAndReusableChunks.RemoveAt(0);

    //         // Reset the chunk
    //         chunk.Reset(new object[]{chunkPosition});
    //     }
    //     else
    //     {
            
            // Reset the chunk
        //     chunk.Reset(chunkPosition);
        // }
        // else
        // {
        //     chunk = new Chunk(worldImage, chunkPosition, chunkBlockCount, blockPixelSize);
        // }
        // Don't forget to add this chunk as a child so it has access to
        // the root of the project.
        // AddChild(chunk);

    //     return chunk;
    // }

    /* This method creates and initialises all the chunks the player can see such
    that they are ready to have their blocks streamed in. */
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
                // Chunk chunk = CreateChunk(chunkPosition);
                loadedChunks.Add(chunkPosition, chunk);
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
        // and can be deleted from memory. The delete_chunk method will handle this
        // for us.
        // TODO: .Keys() not possible
        foreach (Vector2 invisibleChunkPosition in loadedChunks.Keys)
        {
            Chunk chunk = loadedChunks[invisibleChunkPosition];
            chunkPool.ReturnToPool(chunk);
            RemoveChild(chunk);
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
            // Vector2 chunkPosition = getChunkData(point);
            // chunk.obtainChunkData(chunkPosition);
            Array<object> chunkData = new Array<object>{};
            chunkData.Add(chunkPoint);
            chunkData.Add(chunk);
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
