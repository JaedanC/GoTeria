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
        if (collision != null && collision.GetCollisionPosition() != null)
        {
            // GD.Print("Collided at: " + collision.GetCollisionPosition() + " Normal: " + collision.GetCollisionNormal());
            QueueFree();
        }
    }
}
