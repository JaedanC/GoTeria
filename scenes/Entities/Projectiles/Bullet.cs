using Godot;


public class Bullet : Projectile
{
    protected override void AI(int alive, TeriaFastRayCastCollision collision, float delta)
    {
        // The player is actually doing the right thing...
        if (collision != null && collision.GetCollisionPosition() != null)
        {
            // GD.Print("Collided at: " + collision.GetCollisionPosition() + " Normal: " + collision.GetCollisionNormal());
            QueueFree();
        }
    }
}
