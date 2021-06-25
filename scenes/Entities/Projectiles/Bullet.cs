using Godot;


public class Bullet : Projectile
{
    public override void Init(Entity shooter, Vector2 position, Vector2 direction, float speed)
    {
        base.Init(shooter, position, direction, speed);
    }

    public override void AI(int alive, TeriaFastRayCastCollision collision, float delta)
    {
        // The player is actually doing the right thing...
        // GD.Print("Bu: " + Position + " " + Engine.GetPhysicsFrames() + " " + delta);
        if (collision != null && collision.GetCollisionPosition() != null)
        {
            // GD.Print("Collided at: " + collision.GetCollisionPosition() + " Normal: " + collision.GetCollisionNormal());
            // QueueFree();
        }
    }
}

/*
Pl: (12533.51, 3135.992) 519 0.01666667
Bu: (12286.79, 3099.293) 519 0.01666667
Pl: (12606.01, 3135.992) 520 0.01666667
Bu: (12369.54, 3098.449) 520 0.01666667
Computed Lighting for chunk: (5, 0)
Computed Lighting for chunk: (5, 1)
Computed Lighting for chunk: (5, 2)
Pl: (12642.26, 3135.992) 521 0.01666667
Bu: (12410.92, 3098.027) 521 0.01666667
Pl: (12642.26, 3135.992) 522 0.01666667
Bu: (12410.92, 3098.027) 522 0.01666667
Pl: (12642.26, 3135.992) 523 0.01666667
Bu: (12410.92, 3098.027) 523 0.01666667
Pl: (12642.26, 3135.992) 524 0.01666667
Bu: (12410.92, 3098.027) 524 0.01666667
Pl: (12642.26, 3135.992) 525 0.01666667
Bu: (12410.92, 3098.027) 525 0.01666667
Pl: (12642.26, 3135.992) 526 0.01666667
Bu: (12410.92, 3098.027) 526 0.01666667
Pl: (12642.26, 3135.992) 527 0.01666667
Bu: (12410.92, 3098.027) 527 0.01666667
Pl: (12642.26, 3135.992) 528 0.01666667
Bu: (12410.92, 3098.027) 528 0.01666667
Pl: (13259.01, 3135.992) 529 0.01666667
Bu: (12494.25, 3097.176) 529 0.01666667
Pl: (13259.01, 3135.992) 530 0.01666667
Bu: (12494.25, 3097.176) 530 0.01666667
Pl: (13259.01, 3135.992) 531 0.01666667
Bu: (12494.25, 3097.176) 531 0.01666667
Pl: (13469.29, 3135.992) 532 0.01666667
Bu: (12577.57, 3096.326) 532 0.01666667
Pl: (13543.52, 3135.992) 533 0.01666667
Bu: (12654.59, 3095.54) 533 0.01666667
Pl: (13616.02, 3135.992) 534 0.01666667
*/
