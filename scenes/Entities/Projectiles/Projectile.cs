using Godot;
using System;


public abstract class Projectile : Entity
{
    protected Entity shooter;
    protected Vector2 direction;
    protected int alive;
    protected float speed;
    protected bool rayCasted;
    protected bool done;

    public virtual void Initialise(Terrain terrain, CollisionSystem collisionSystem, Entity shooter, Vector2 position, Vector2 direction, float speed)
    {
        base.Initialise(terrain, collisionSystem);
        shooter.AddChild(this);
        this.terrain = terrain;
        this.collisionSystem = collisionSystem;
        this.shooter = shooter;
        this.Position = position;
        this.direction = direction.Normalized();
        this.speed = Mathf.Max(speed, 0);
        this.rayCasted = false;
        alive = 0;
        Teleport();

        // Disable the hitbox as it is no longer required
        GetHitbox().SetDeferred("disabled", true);
    }

    // Use raycasting
    public void Initialise(Terrain terrain, CollisionSystem collisionSystem, Entity shooter, Vector2 position, Vector2 direction)
    {
        Initialise(terrain, collisionSystem, shooter, position, direction, 0f);
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
