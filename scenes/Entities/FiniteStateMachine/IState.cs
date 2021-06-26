using Godot;
using System;


public abstract class IState
{
    public virtual void FirstFrame(FiniteStateMachine ai, Entity myself, Player player, float delta) { }

    public abstract void PhysicsProcess(FiniteStateMachine ai, Entity myself, Player player, int alive, float delta);
}