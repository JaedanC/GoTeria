using Godot;
using System;

public class TeriaRayCastCollision
{
    private PhysicsBody2D collider;
    private Vector2? collisionPosition;
    private Vector2? collisionNormal;

    public TeriaRayCastCollision(Godot.Collections.Dictionary intersectRayReturn)
    {
        // The value in the dictionary could be null but c# lets you cast
        // nulls to other types. Thankyou c# devs. Vector2 is a struct so it needs to
        // be marked as Nullable.
        this.collider = (PhysicsBody2D)GetIfExists(intersectRayReturn, "collider");
        this.collisionPosition = (Vector2?)GetIfExists(intersectRayReturn, "position");
        this.collisionNormal = (Vector2?)GetIfExists(intersectRayReturn, "normal");
    }

    private object GetIfExists(Godot.Collections.Dictionary intersectRayReturn, String key)
    {
        if (intersectRayReturn.Contains(key))
        {
            return intersectRayReturn[key];
        }
        return null;
    }

    public PhysicsBody2D GetCollider()
    {
        return collider;
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