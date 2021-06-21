
using Godot;

public interface ICollidable
{
    Vector2 Velocity { get; set; }
    KinematicBody2D GetRigidBody();
    CollisionShape2D GetHitbox();
}