using Godot;

public class TeriaFastRayCastCollision
{
    private readonly Vector2? collisionPosition;
    private readonly Vector2? collisionNormal;

    public TeriaFastRayCastCollision(Vector2? position, Vector2? normal)
    {
        // The value in the dictionary could be null but c# lets you cast
        // nulls to other types. Thank you c# devs. Vector2 is a struct so it needs to
        // be marked as Nullable.
        collisionPosition = position;
        collisionNormal = normal;
    }

    public Vector2? GetCollisionPosition()
    {
        return collisionPosition;
    }

    public Vector2? GetCollisionNormal()
    {
        return collisionNormal;
    }
}
