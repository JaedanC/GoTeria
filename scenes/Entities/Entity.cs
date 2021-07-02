using Godot;


public abstract class Entity : Node2D, ICollidable
{
    protected Terrain terrain;
    protected CollisionSystem collisionSystem;
    protected CollisionComponent collisionComponent;
    protected Smoothing smoothing;
    protected KinematicBody2D rigidBody;
    protected CollisionShape2D hitbox;
    protected Vector2 velocity;
    public Vector2 SmoothPosition => smoothing.Position;

    /* Hide the Entity's position with the rigidbody one. If you require the smooth location
    use SmoothPosition. */
    public new Vector2 Position {
        get => rigidBody.Position;
        set => rigidBody.Position = value;
    }
    public new float Rotation {
        get => (float)smoothing.Get("rotation");
        set => rigidBody.Rotation = value;
    }

    public override void _Ready()
    {
        // Dependencies
        collisionComponent = GetNodeOrNull<CollisionComponent>("CollisionComponent");
        smoothing = GetNode<Smoothing>("Smoothing");
        rigidBody = GetNode<KinematicBody2D>("RigidBody");
        hitbox = rigidBody.GetNode<CollisionShape2D>("Hitbox");
    }

    public virtual void Initialise(Terrain terrain, CollisionSystem collisionSystem)
    {
        this.terrain = terrain;
        this.collisionSystem = collisionSystem;

        // if (collisionComponent != null)
        //     collisionComponent.Initialise(terrain, collisionSystem);
        collisionComponent?.Initialise(collisionSystem);
    }

    public void Teleport()
    {
        smoothing.Teleport();
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
    }

    public KinematicBody2D GetRigidBody()
    {
        return rigidBody;
    }

    public CollisionShape2D GetHitbox()
    {
        return hitbox;
    }
}
