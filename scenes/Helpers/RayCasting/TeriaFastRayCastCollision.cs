using Godot;
using System;

public class TeriaFastRayCastCollision
{
    private Vector2? collisionPosition;
    private Vector2? collisionNormal;

    public TeriaFastRayCastCollision(Vector2? position, Vector2? normal)
    {
        // The value in the dictionary could be null but c# lets you cast
        // nulls to other types. Thankyou c# devs. Vector2 is a struct so it needs to
        // be marked as Nullable.
        this.collisionPosition = position;
        this.collisionNormal = normal;
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
