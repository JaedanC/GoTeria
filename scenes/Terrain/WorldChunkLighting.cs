using Godot;
using Godot.Collections;
using System;


public class WorldChunkLighting
{
    private WorldChunkLighting() { }
    private static WorldChunkLighting instance;
    public static WorldChunkLighting Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new WorldChunkLighting();
            }
            return instance;
        }
    }

    private bool initialised = false;
    private WorldFile worldFile;
    private Terrain terrain;
    private MultithreadedChunkLoader chunkLoader;
    private ObjectPool<Chunk> chunkPool;
    private String saveName;

    public void Initialise(Terrain terrain, WorldFile worldFile, ObjectPool<Chunk> chunkPool, String saveName)
    {
        this.worldFile = worldFile;
        this.terrain = terrain;
        this.chunkLoader = new MultithreadedChunkLoader(
            terrain.ChunkBlockCount,
            new ThreadPool(),
            terrain.WorldBlocksImage,
            terrain.WorldWallsImage
        );
        this.initialised = true;
        this.chunkPool = chunkPool;
        this.saveName = saveName;
    }

    public Image GetWorldLightImage()
    {
        Image worldLightImage = worldFile.LoadImage(saveName, "light.png");
        if (worldLightImage != null)
        {
            return worldLightImage;
        }
        worldLightImage = ComputeWorldLightImage();
        worldFile.SaveImage(worldLightImage, saveName, "light.png");
        return worldLightImage;
    }

    private Image ComputeWorldLightImage()
    {
        Dictionary<Vector2, Chunk> loadedChunks = new Dictionary<Vector2, Chunk>();

        Vector2 worldSizeInChunks = terrain.GetWorldSize() / terrain.ChunkBlockCount;

        for (int i = 0; i < worldSizeInChunks.x; i++)
        for (int j = 0; j < worldSizeInChunks.y; j++)
        {
            Vector2 chunkPosition = new Vector2(i, j);
            Chunk chunk = chunkPool.GetInstance(chunkPosition, terrain.BlockPixelSize, terrain.ChunkBlockCount);
            loadedChunks[chunkPosition] = chunk;
        }

        foreach (Vector2 chunkPosition in loadedChunks.Keys)
        {
            chunkLoader.BeginLightingChunk(loadedChunks[chunkPosition], loadedChunks);
        }

        chunkLoader.GetFinishedLoadingChunks(loadedChunks);
        chunkLoader.GetFinishedLightingChunks(loadedChunks);

        foreach (Vector2 chunkPosition in loadedChunks.Keys)
        {
            chunkLoader.FinishLightingChunkForcefully(chunkPosition);
        }

        // We now should have all the chunks loaded and their light calculated.
        terrain.LightingEngine.WorldLightImage.CommitColourChanges();
        return terrain.LightingEngine.WorldLightImage.GetImage();
    }

    public void LightChunk(Chunk chunk)
    {
        Developer.AssertTrue(initialised, "WorldChunkLighting must be initialised first.");
        LightChunkBFS(chunk);
    }

    private void LightChunkBFS(Chunk chunk)
    {
        QueueSet<LightingEngine.LightBFSNode> lightQueue = new QueueSet<LightingEngine.LightBFSNode>();

        Vector2 chunkBlockCount = terrain.ChunkBlockCount;
        Vector2 topLeftPixel = chunk.ChunkPosition * chunkBlockCount;

        // GD.Print("Chunk: ", topLeftPixel, chunkBlockCount);
        for (int i = (int)topLeftPixel.x; i < topLeftPixel.x + chunkBlockCount.x; i++)
        for (int j = (int)topLeftPixel.y; j < topLeftPixel.y + chunkBlockCount.y; j++)
        {
            Vector2 position = new Vector2(i, j);
            if (Helper.OutOfBounds(position, terrain.GetWorldSize()))
                continue;

            Color sourceColour = terrain.LightingEngine.WorldLightSources.GetPixelv(position);
            if (sourceColour == Colors.White)
            {
                lightQueue.Enqueue(new LightingEngine.LightBFSNode(position, Colors.White));
            }
        }

        terrain.LightingEngine.LightUpdateBFS(lightQueue);
        // lightingEngine.LightUpdateBFS(lightQueue);
    }
}