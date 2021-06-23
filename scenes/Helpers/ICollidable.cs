
using Godot;

public interface ICollidable
{
    Vector2 GetVelocity();
    void SetVelocity(Vector2 newVelocity);
    KinematicBody2D GetRigidBody();
    CollisionShape2D GetHitbox();
}