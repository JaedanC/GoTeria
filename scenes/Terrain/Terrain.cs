using Godot;
using Godot.Collections;


public class Terrain : Node2D
{
    private const int loadMargin = 1;
    private const int lightingMargin = 1;

    private Player player;
    private InputLayering inputLayering;
    private LightingEngine lightingEngine;
    private ITerrainStack terrainStack;
    private WorldFile worldFile;
    private ObjectPool<Chunk> chunkPool;
    private ThreadPool threadPool;
    private ChunkLighting chunkLighting;

    private Vector2 blockPixelSize;
    private Vector2 chunkBlockCount;

    private Vector2 chunkPixelDimensions;
    // private Dictionary<Vector2, Chunk> loadedChunks;
    private LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent;
    private Dictionary<Vector2, Chunk> urgentChunks;
    private Dictionary<Vector2, Chunk> lightDrawChunks;
    private Dictionary<Vector2, Chunk> lightLoadingChunks;
    private MultithreadedChunkLoader chunkLoader;

    /* Returns the size of a block in pixels as a Vector2 */
    public Vector2 BlockPixelSize { get { return blockPixelSize; } }
    public Vector2 ChunkPixelDimensions { get { return chunkPixelDimensions; } }
    public Vector2 ChunkBlockCount { get { return chunkBlockCount; } }
    public Image WorldBlocksImage { get { return terrainStack.WorldBlocksImage; } }
    public Image WorldWallsImage { get { return terrainStack.WorldWallsImage; } }
    public LightingEngine LightingEngine { get { return lightingEngine; } }


    public override void _Ready()
    {
        Name = "Terrain";

        // Dependencies
        lightingEngine = GetNode<LightingEngine>("Lighting");

        // Local variables
        loadedChunksConcurrent = new LazyVolatileDictionary<Vector2, Chunk>();
        urgentChunks = new Dictionary<Vector2, Chunk>();
        lightDrawChunks = new Dictionary<Vector2, Chunk>();
        lightLoadingChunks = new Dictionary<Vector2, Chunk>();
    }

    public void Initialise(ThreadPool threadPool, InputLayering inputLayering, Player player, WorldFile worldFile,
                           Vector2 blockPixelSize, Vector2 chunkBlockCount, TeriaFile lightingCacheFile, TeriaFile lightingConfigFile, bool singleThreadedLightingEngine)
    {
        this.threadPool = threadPool;
        this.inputLayering = inputLayering;
        this.player = player;
        this.worldFile = worldFile;
        this.blockPixelSize = blockPixelSize;
        this.chunkBlockCount = chunkBlockCount;
        this.chunkPixelDimensions = BlockPixelSize * ChunkBlockCount;

        // Use instances
        this.terrainStack = worldFile.GetITerrainStack();
        this.chunkLoader = new MultithreadedChunkLoader(ChunkBlockCount, threadPool, WorldBlocksImage, WorldWallsImage);

        // Instance locals
        this.chunkPool = new ObjectPool<Chunk>(15, ChunkBlockCount);
        this.chunkLighting = new ChunkLighting(lightingEngine, lightingCacheFile, lightingConfigFile, GetWorldSizeInChunks());

        // Initialise further
        lightingEngine.Initialise(inputLayering, this, player, chunkLighting, singleThreadedLightingEngine);
    }

    public override void _Process(float _delta)
    {
        // Save the all chunks loaded in memory to a file.
        if (inputLayering.PopActionPressed("save_world"))
        {
            // TODO: Make this save to the current file with just worldFile.SaveWorld()
            TeriaFile blockFile = new TeriaFile(true, "saves/SavedWorld/worlds/blocks.png");
            TeriaFile wallFile = new TeriaFile(true, "saves/SavedWorld/worlds/walls.png");
            worldFile.SaveWorld(blockFile, wallFile, false);
            lightingEngine.SaveLight();
        }

        loadedChunksConcurrent.Lock();
        DeleteInvisibleChunks();
        LoadVisibleChunks();
        loadedChunksConcurrent.Unlock();
        CreateChunkStreamingRegions();
        ContinueStreamingRegions();

        // Draw the chunk borders
        Update();
    }

    /* This currently colours the chunks with a border donoting the kind of chunk it
    is and how it should be streamed in. Reducing the viewportRectangle in the
    Player.GetVisibilityPoints method will allow you to see this process in action. */
    public override void _Draw()
    {
        int thickness = 10;
        foreach (Vector2 point in lightLoadingChunks.Keys)
            DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Green, false, thickness, false);

        foreach (Vector2 point in lightDrawChunks.Keys)
            DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Orange, false, thickness, false);

        foreach (Vector2 point in urgentChunks.Keys)
            DrawRect(new Rect2(point * chunkPixelDimensions, chunkPixelDimensions), Colors.Red, false, thickness, false);

    }

    /* This method creates chunks (from the ObjectPool) that are visible to the player. These
    chunks are stored in the loadedChunks Dictionary. */
    private void LoadVisibleChunks()
    {
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "LoadVisibleChunks() Requires the lock to be on.");

        Array<Vector2> visibleChunkPositions = player.GetVisibilityChunkPositions(loadMargin + lightingMargin);
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
            GetOrInstanceChunkInto(chunkPosition, loadedChunksConcurrent);
            // if (!loadedChunksConcurrent.VolatileContainsKey(chunkPosition))
            // {
            // }
        }
    }

    /* Requires the loadedChunkMutex to be Locked(). */
    public Chunk GetOrInstanceChunkInto(Vector2 chunkPosition, LazyVolatileDictionary<Vector2, Chunk> loadedChunksConcurrent)
    {
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "GetOrInstanceChunkInto() Requires the lock to be on.");

        if (loadedChunksConcurrent.VolatileContainsKey(chunkPosition))
        {
            return loadedChunksConcurrent.Get(chunkPosition);
        }

        Chunk chunk = chunkPool.GetInstance(WorldBlocksImage, chunkPosition, BlockPixelSize,
                                            ChunkBlockCount, this, worldFile, chunkLighting);
        AddChild(chunk);
        loadedChunksConcurrent.VolatileAdd(chunkPosition, chunk);
        return chunk;
    }
    
    /* This method uses the visibility points to determine which chunks should be
    unloaded from memory. */
    private void DeleteInvisibleChunks()
    {
        Developer.AssertTrue(loadedChunksConcurrent.IsLocked, "DeleteInvisibleChunks() Requires the lock to be on.");

        // First we grab a set the worldPositions that should be loaded in the game 
        Array<Vector2> visibleChunkPositions = player.GetVisibilityChunkPositions(loadMargin + lightingMargin);

        // Create a temporary dictionary to store chunks that are already loaded
        // and should stay loaded
        // LazyVolatileDictionary<Vector2, Chunk> visibleChunks = new LazyVolatileDictionary<Vector2, Chunk>();

        // Loop through the loaded chunks and store the ones that we should keep in
        // visibleChunks while erasing them from the old loadedChunks dictionary. 
        System.Collections.Generic.IDictionary<Vector2, Chunk> deletedChunks = loadedChunksConcurrent.VolatileKeepOnly(visibleChunkPositions);
        // foreach (Vector2 visiblePoint in visibleChunkPositions)
        // {
        //     if (loadedChunksConcurrent.ContainsKey(visiblePoint))
        //     {
        //         visibleChunks.Add(visiblePoint, loadedChunksConcurrent[visiblePoint]);
        //         loadedChunksConcurrent.Remove(visiblePoint);
        //     }
        // }

        // Now the remaining chunks in loadedChunks are invisible to the player
        // and can be deleted from memory.
        foreach (Vector2 invisibleChunkPosition in deletedChunks.Keys)
        {
            Chunk chunk = deletedChunks[invisibleChunkPosition];
            RemoveChild(chunk);
            chunkPool.Die(chunk);
            // loadedChunksConcurrent.Remove(invisibleChunkPosition);
        }

        // Finally, our new visible chunks dictionary becomes the loaded chunks
        // dictionary
        // loadedChunksConcurrent = visibleChunks;

        // Also reset the region loading dictionaries. These will be repopulated
        // later.
        urgentChunks.Clear();
        lightDrawChunks.Clear();
        lightLoadingChunks.Clear();
    }

    private void CreateChunkStreamingRegions()
    {
        Array<Vector2> urgentVisibilityPoints = player.GetVisibilityChunkPositions();
        Array<Vector2> drawVisibilityPoints = player.GetVisibilityChunkPositions(lightingMargin, true);
        Array<Vector2> loadVisibilityPoints = player.GetVisibilityChunkPositions(loadMargin + lightingMargin, true, lightingMargin);

        // GD.Print("Urgent:", urgentVisibilityPoints);
        // GD.Print("Load:", loadVisibilityPoints);

        foreach (Vector2 point in urgentVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                urgentChunks[point] = chunk;
        }

        foreach (Vector2 point in drawVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                lightDrawChunks[point] = chunk;
        }

        foreach (Vector2 point in loadVisibilityPoints)
        {
            Chunk chunk = GetChunkFromChunkPosition(point);
            if (chunk != null)
                lightLoadingChunks[point] = chunk;
        }
    }

    /* This method continues to load in the chunks based on the three regions
    outlined at the top of this script. To tweak performance consider changing
    these variables:
        - drawMargin. This is the number of extra chunk layers outside the view of
        screen that will drawn (by streaming) such that a player moving into new
        chunks won't experience lag spikes
        - loadMargin. This is the number of extra chunk layers past the drawMargin
        that will only stream in blocks.
        - 'ChunkBlockCount'. This is the number of blocks in each chunk. This
        should be set to a reasonable value like (16, 16) or (32, 32). Experiment
        with others.

    The premise behind these dictionaries are to improve performance when streaming
    in chunks behind the scenes.
        - lightLoadingChunks. These are slowly loading in their blocks
        over a series of frames. These happen from a large distance away. Mediumly
        off the screen.
        - lightDrawChunks. These chunks are drawing their blocks.
        These happen from a short distance away, just off the screen
        - urgentChunks. These chunks are so close to the player that
        they need to have their blocks loaded in right away. The drawing can happen
        later though
        - loadedChunks. These chunks are fully drawn and visible to the player.

    Tweaking these values on different computers may result in better performance.
    TODO: Make them editable in a configuration file in the future. */
    private void ContinueStreamingRegions()
    {
        // Loading chunks
        loadedChunksConcurrent.Lock();
        Array<Vector2> chunksToLoad = new Array<Vector2>(urgentChunks.Keys) +
                                      new Array<Vector2>(lightDrawChunks.Keys) +
                                      new Array<Vector2>(lightLoadingChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToLoad)
        {
            Chunk chunk = loadedChunksConcurrent.Get(chunkPosition);
            chunkLoader.BeginLoadingChunk(chunk, chunkPosition);
        }
        loadedChunksConcurrent.Unlock();

        // Force load close chunks
        Array<Vector2> chunksToForceLoad = new Array<Vector2>(urgentChunks.Keys) +
                                           new Array<Vector2>(lightDrawChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToForceLoad)
        {
            chunkLoader.FinishLoadingChunkForcefully(chunkPosition);
        }

        // chunkLoader.GetFinishedLoadingChunks(loadedChunks);

        // Lighting chunks
        loadedChunksConcurrent.Lock();
        Array<Vector2> chunksToLight = new Array<Vector2>(urgentChunks.Keys) +
                                       new Array<Vector2>(lightDrawChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToLight)
        {
            Chunk chunk = loadedChunksConcurrent.Get(chunkPosition);
            chunkLoader.BeginLightingChunk(chunk, loadedChunksConcurrent);
        }
        loadedChunksConcurrent.Unlock();

        // Force light close chunks
        Array<Vector2> chunksToForceLight = new Array<Vector2>(urgentChunks.Keys);
        foreach (Vector2 chunkPosition in chunksToForceLight)
        {
            chunkLoader.FinishLightingChunkForcefully(chunkPosition);
        }
    }

    /* Returns the size of the world in blocks as a Vector2. TODO: Change to use the
    worldSizeInChunks variable when we create our own world. */
    public Vector2 GetWorldSize()
    {
        return WorldBlocksImage.GetSize();
    }

    public Vector2 GetWorldSizeInChunks()
    {
        return (GetWorldSize() / ChunkBlockCount).Floor();
    }

    /* Returns a Chunk if it exists at the given chunkPosition in the world.
    - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
    index in the world.

    Fastest function to get chunks.*/
    public Chunk GetChunkFromChunkPosition(Vector2 chunkPosition)
    {
        // if (CreateChunk(chunkPosition))
        loadedChunksConcurrent.Lock();
        try
        {
            return loadedChunksConcurrent.VolatileTryGet(chunkPosition);
        }
        finally
        {
            loadedChunksConcurrent.Unlock();
        }
    }

    /* Returns a Chunk if it exists at the given worldPosition in the world.
    World positions are locations represented by pixels. Entities in the world
    are stored using this value.

    Slowest function to get chunks.*/
    public Chunk GetChunkFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        return GetChunkFromChunkPosition(chunkPosition);
    }

    /* Returns a chunk position from the given worldPosition.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
    index in the world.

    Fastest function to get chunk positions.*/
    public Vector2 GetChunkPositionFromWorldPosition(Vector2 worldPosition)
    {
        return (worldPosition / chunkPixelDimensions).Floor();
    }

    /* Returns a block if it exists using the given chunkPosition and blockPosition
    values.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
        - Block positions are the position of the block relative to the chunk it is
        in. It cannot be larger than the chunk's blockSize.
    Blocks are Dictionaries containing a set of standard variables. See the Block
    documentation in the Chunk scene.

    Fastest function to get blocks.*/
    public Block GetBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return null;
        if (!chunk.LoadingDone)
            return null;
        return chunk.GetBlockFromBlockPosition(blockPosition);
    }

    public IBlock GetTopIBlockFromChunkPositionAndBlockPosition(Vector2 chunkPosition, Vector2 blockPosition)
    {
        Chunk chunk = GetChunkFromChunkPosition(chunkPosition);
        if (chunk == null)
            return null;
        if (!chunk.LoadingDone)
            return null;
        return chunk.GetTopIBlockFromBlockPosition(blockPosition);
    }

    public IBlock GetTopIBlockFromWorldBlockPosition(Vector2 worldBlockPosition)
    {
        Vector2 chunkPosition = (worldBlockPosition / chunkBlockCount).Floor();
        Vector2 blockPosition = worldBlockPosition - chunkPosition * chunkBlockCount;
        return GetTopIBlockFromChunkPositionAndBlockPosition(chunkPosition, blockPosition);
    }

    /* Returns a block if it exists using the given worldPosition.
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

    /* Returns a block position using the given worldPosition and chunkPosition
    values.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
    Block positions are the position of the block relative to the chunk it is in. It
    cannot be larger than the chunk's blockSize.
    Fastest function to get block positions.*/
    public Vector2 GetBlockPositionFromWorldPositionAndChunkPosition(Vector2 worldPosition, Vector2 chunkPosition)
    {
        Vector2 blockPosition = (worldPosition - chunkPosition * chunkPixelDimensions).Floor();
        return (blockPosition / BlockPixelSize).Floor();
    }

    /* Returns a block position using the given worldPosition.
        - World positions are locations represented by pixels. Entities in the world
        are stored using this value.
    Block positions are the position of the block relative to the chunk it is in. It
    cannot be larger than the chunk's blockSize.
    Slowest function to get block positions. */
    public Vector2 GetBlockPositionFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 chunkPosition = GetChunkPositionFromWorldPosition(worldPosition);
        return GetBlockPositionFromWorldPositionAndChunkPosition(worldPosition, chunkPosition);
    }

    /* Sets a block at the given chunkPosition and blockPosition to be the
    newBlock if it exists.
        - Chunk positions are Vectors like [0, 0] or [0, 1] that represent a chunk's
        index in the world.
        - Block positions are the position of the block relative to the chunk it is
        in. It cannot be larger than the chunk's blockSize.
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

    /* Sets a block at the given worldPosition to be the newBlock if it exists.
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

        CheckIfUpdateLighting(worldBlockPosition);
    }

    private void CheckIfUpdateLighting(Vector2 worldBlockPosition)
    {
        IBlock topBlock = GetTopIBlockFromWorldBlockPosition(worldBlockPosition);
        if (Helper.IsLight(topBlock.Colour))
            lightingEngine.AddLight(worldBlockPosition, Colors.White);
        else
            lightingEngine.RemoveLight(worldBlockPosition);
    }
}
