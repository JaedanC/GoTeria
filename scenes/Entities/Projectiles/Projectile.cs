using Godot;
using System;


public abstract class Projectile : Entity
{
    protected CollisionSystem collisionSystem;
    protected Terrain terrain;
    protected Entity shooter;
    protected Vector2 direction;
    protected int alive;
    protected float speed;
    protected bool rayCasted;
    protected bool done;

    public virtual void Init(Entity shooter, Vector2 position, Vector2 direction, float speed)
    {
        shooter.AddChild(this);
        this.Position = position;
        this.shooter = shooter;
        this.direction = direction.Normalized();
        this.speed = Mathf.Max(speed, 0);
        this.rayCasted = false;
        alive = 0;
        Teleport();

        collisionSystem = GetNode<CollisionSystem>("/root/WorldSpawn/CollisionSystem");
        terrain = GetNode<Terrain>("/root/WorldSpawn/Terrain");

        // Disable the hitbox as it is no longer required
        GetHitbox().SetDeferred("disabled", true);
    }

    // Use raycasting
    public void Init(Entity shooter, Vector2 position, Vector2 direction)
    {
        Init(shooter, position, direction, 0f);
        this.rayCasted = true;
    }

    public override void _Ready()
    {
        Name = "Projectile";
        base._Ready();
    }

    public override void _PhysicsProcess(float delta)
    {
        // Die after a certain amount of time
        if (alive == 2000)
        {
            QueueFree();
            return;
        }

        if (!done)
            Behaviour(delta);

        alive += 1;
    }

    private TeriaFastRayCastCollision FastCast(float delta)
    {
        TeriaFastRayCast rayCast;
        TeriaFastRayCastCollision collision;
        if (rayCasted)
        {
            rayCast = TeriaFastRayCast.FromDirection(terrain, shooter, GetWorld2d().DirectSpaceState, Position, direction, 1000);
            collision = rayCast.Cast();

            Vector2? collisionPosition = collision.GetCollisionPosition();
            if (collisionPosition != null)
            {
                this.Position = (Vector2)collisionPosition;
            }
            done = true;
        }
        else
        {
            rayCast = TeriaFastRayCast.FromDirection(terrain, shooter, GetWorld2d().DirectSpaceState, Position, direction, speed * delta);
            collision = rayCast.Cast();

            Vector2? collisionPosition = collision.GetCollisionPosition();
            if (collisionPosition != null)
            {
                this.Position = (Vector2)collisionPosition;
                done = true;
            }
            else
            {
                this.Position += direction * speed * delta;
            }
        }
        return collision;
    }

    private void Behaviour(float delta)
    {
        // Rotate with the direction
        Rotation = direction.Angle();
        Teleport();

        TeriaFastRayCastCollision collision = FastCast(delta);

        AI(alive, collision, delta);
    }

    public Entity GetShooter()
    {
        return shooter;
    }

    public virtual void AI(int alive, TeriaFastRayCastCollision collision, float delta) {}
}
