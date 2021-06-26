using Godot;
using System;


public abstract class Entity : Node2D, ICollidable
{
    protected Vector2 velocity;
    protected KinematicBody2D rigidBody;
    protected CollisionShape2D hitbox;
    protected Smoothing smoothing;
    public Vector2 SmoothPosition
    {
        get
        {
            return smoothing.Position;
        }
    }
    public new Vector2 Position {
        get
        {
            return rigidBody.Position;
        }
        set
        {
            rigidBody.Position = value;
        }
    }
    public new float Rotation {
        get
        {
            return (float)smoothing.Get("rotation");
        }
        set
        {
            rigidBody.Rotation = value;
        }
    }

    public override void _Ready()
    {
        rigidBody = GetNode<KinematicBody2D>("RigidBody");
        hitbox = rigidBody.GetNode<CollisionShape2D>("Hitbox");
        smoothing = GetNode<Smoothing>("Smoothing");
    }

    public void Teleport()
    {
        smoothing.Teleport();
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
    }

    public KinematicBody2D GetRigidBody()
    {
        return rigidBody;
    }

    public CollisionShape2D GetHitbox()
    {
        return hitbox;
    }
}
