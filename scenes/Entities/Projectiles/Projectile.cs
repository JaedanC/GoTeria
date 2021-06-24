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
        this.shooter = shooter;
        Teleport(position);
        this.direction = direction.Normalized();
        this.speed = Mathf.Max(speed, 0);
        this.rayCasted = false;
        alive = 0;

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
        if (alive == 200)
        {
            QueueFree();
            return;
        }

        if (!done)
            Behaviour(delta);

        alive += 1;
    }

    protected void Behaviour(float delta)
    {
        // Rotate with the direction
        Rotation = direction.Angle();

        TeriaRayCast rayCast;
        TeriaRayCastCollision collision;
        if (rayCasted)
        {
            rayCast = TeriaRayCast.FromDirection(terrain, collisionSystem, GetWorld2d().DirectSpaceState, shooter, Position, direction, 1000);
            collision = rayCast.Cast();

            if (collision.GetCollider() != null)
            {
                this.Position = (Vector2)collision.GetCollisionPosition();
            }
            done = true;
        }
        else
        {
            rayCast = TeriaRayCast.FromDirection(terrain, collisionSystem, GetWorld2d().DirectSpaceState, shooter, Position, direction, speed * delta); // Half the FPS dies here
            collision = rayCast.Cast(); // The other half here.

            if (collision.GetCollider() != null)
            {
                this.Position = (Vector2)collision.GetCollisionPosition();
            }
            else
            {
                this.Position += direction * speed * delta;
            }
        }
        // rayCast.Free();
        AI(alive, null);
    }

    public Entity GetShooter()
    {
        return shooter;
    }

    public virtual void AI(int alive, TeriaRayCastCollision collision) {}
}
