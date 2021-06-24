using Godot;


public class Bullet : Projectile
{
    public override void Init(Entity shooter, Vector2 position, Vector2 direction, float speed)
    {
        base.Init(shooter, position, direction, speed);
    }

    public override void AI(int alive, TeriaRayCastCollision collision)
    {
        if (collision != null && collision.GetCollider() != null)
        {
            // GD.Print("Collided with: " + collision.GetCollider() + " @ " + collision.GetCollisionPosition());
            QueueFree();
        }
    }
}
