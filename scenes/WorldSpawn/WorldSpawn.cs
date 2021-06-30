using Godot;

public class WorldSpawn : Node
{
    public class Future<T>
    {
        public static Player Redeem(Future<Player> future)
        {
            return WorldSpawn.activeWorldSpawn.GetPlayer();
        }
    }

    [Export]
    private string windowTitle = "Teria";

    [Export]
    private Vector2 blockPixelSize = new Vector2(16, 16);

    [Export]
    // private Vector2 chunkBlockCount = new Vector2(420, 400);
    private readonly Vector2 chunkBlockCount = new Vector2(210, 200);
    private readonly bool singleThreadedThreadPool;
    private readonly bool singleThreadedLightingEngine;
    private readonly int threadPoolThreads;

    // Static reference to the current active WorldSpawn
    private static WorldSpawn activeWorldSpawn;

    // Instance variables
    private readonly string saveFileName;
    private readonly TeriaFile configFile;
    private readonly TeriaFile blockFile;
    private readonly TeriaFile wallFile;
    private readonly TeriaFile lightingConfigFile;
    private readonly TeriaFile lightingCacheFile;

    // Singletons
    private ThreadPool threadPool;
    private InputLayering inputLayering;

    // Children
    private ActionMapping actionMapping;
    private Player player;
    private Terrain terrain;
    private CollisionSystem collisionSystem;

    // Local Instances
    private readonly AnalogMapping analogMapping;
    private readonly WorldFile worldFile;
    private readonly ConfigFile config;

    public WorldSpawn()
    {
        // Make this instance static so arbitrary nodes can request data
        activeWorldSpawn = this;

        // Constants
        singleThreadedThreadPool = false;
        singleThreadedLightingEngine = false;
        saveFileName = "SavedWorld";
        threadPoolThreads = Mathf.Max(1, OS.GetProcessorCount() - 2);
        // threadPoolThreads = OS.GetProcessorCount();
        // threadPoolThreads = 1;

        // Only instance what has no dependencies and isn't a Node
        configFile = new TeriaFile(true, "saves/" + saveFileName + "/bindings.ini");
        blockFile = new TeriaFile(true, "saves/" + saveFileName + "/worlds/blocks.png");
        wallFile = new TeriaFile(true, "saves/" + saveFileName + "/worlds/walls.png");
        lightingCacheFile = new TeriaFile(true, "saves/" + saveFileName + "/worlds/light.png");
        lightingConfigFile = new TeriaFile(true, "saves/" + saveFileName + "/worlds/lighting.json");

        analogMapping = new AnalogMapping();
        worldFile = new WorldFile(blockFile, wallFile);
        config = new ConfigFile();

        // Value checks
        Developer.AssertGreaterThan(blockPixelSize.x, 0, "BlockPixelSize.x is 0");
        Developer.AssertGreaterThan(blockPixelSize.y, 0, "BlockPixelSize.y is 0");
        Developer.AssertGreaterThan(chunkBlockCount.x, 0, "ChunkBlockCount.x is 0");
        Developer.AssertGreaterThan(chunkBlockCount.y, 0, "ChunkBlockCount.y is 0");
    }

    public override void _Ready()
    {
        // Get Singletons
        threadPool = GetNode<ThreadPool>("/root/ThreadPool");
        inputLayering = GetNode<InputLayering>("/root/InputLayering");

        // Get Children
        actionMapping = GetNode<ActionMapping>("ActionMapping");
        player = GetNode<Player>("Player");
        terrain = GetNode<Terrain>("Terrain");
        collisionSystem = GetNode<CollisionSystem>("CollisionSystem");

        // Initialise Singletons
        threadPool.Initialise(singleThreadedThreadPool, true, threadPoolThreads);
        inputLayering.Initialise(analogMapping);

        // Initialise children
        actionMapping.Initialise(analogMapping, config, configFile);
        player.Initialise(inputLayering, terrain, collisionSystem);
        terrain.Initialise(threadPool, inputLayering, player, worldFile, blockPixelSize, chunkBlockCount,
                           lightingCacheFile, lightingConfigFile, singleThreadedLightingEngine, singleThreadedThreadPool);
        collisionSystem.Initialise(terrain);
    }

    public WorldFile GetWorldFile()
    {
        return worldFile;
    }

    private Player GetPlayer()
    {
        return player;
    }

    public override void _Process(float delta)
    {
        OS.SetWindowTitle($"{windowTitle} | FPS: {Engine.GetFramesPerSecond()}");

        if (inputLayering.PopActionPressed("toggle_fullscreen"))
        {
            OS.WindowFullscreen = !OS.WindowFullscreen;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            InputEventKey keyEvent = (InputEventKey)@event;
            if (keyEvent.Alt && keyEvent.Scancode == (uint)KeyList.Enter && keyEvent.IsPressed())
            {
                OS.WindowFullscreen = !OS.WindowFullscreen;
            }
        }
    }
}
