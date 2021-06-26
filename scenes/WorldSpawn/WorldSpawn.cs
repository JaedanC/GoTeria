using Godot;
using System;

public class WorldSpawn : Node
{
    public class Future<T>
    {
        public static Player Redeem(Future<Player> future)
        {
            return WorldSpawn.ActiveWorldSpawn.GetPlayer();
        }
    }

    public static WorldSpawn ActiveWorldSpawn;

    private const String title = "Teria";
    
    // Instance variables
    String saveFileName = "SavedWorld";

    // Singletons
    private ThreadPool threadPool;
    private InputLayering inputLayering;

    // Children
    private ActionMapping actionMapping;
    private Player player;
    private Terrain terrain;
    private CollisionSystem collisionSystem;

    // Local Instances
    private AnalogMapping analogMapping;
    private WorldFile worldFile;

    public WorldSpawn()
    {
        // Make this instance static so arbritray nodes can request the above
        WorldSpawn.ActiveWorldSpawn = this;

        // Only instance what has no dependencies and isn't a Node
        worldFile = new WorldFile(saveFileName);
        analogMapping = new AnalogMapping();
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
        threadPool.Initialise(false);
        inputLayering.Initialise(analogMapping);

        // Initialise children
        actionMapping.Initialise(analogMapping);
        player.Initialise(inputLayering, terrain, collisionSystem);
        terrain.Initialise(threadPool, inputLayering, player, worldFile);
        collisionSystem.Initialise(terrain);
    }

    public WorldFile GetWorldFile()
    {
        return worldFile;
    }

    public Player GetPlayer()
    {
        return player;
    }

    public override void _Process(float delta)
    {
        OS.SetWindowTitle(String.Format("{0} | FPS: {1}", title, Engine.GetFramesPerSecond()));

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
