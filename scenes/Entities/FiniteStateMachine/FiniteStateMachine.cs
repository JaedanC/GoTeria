using Godot;
using System;


// Template class
public abstract class FiniteStateMachine : Node
{
    protected IState currentState;
    private Entity myself;
    private Player player;
    private int alive;

    public override void _Ready()
    {
        player = GetNode<Player>("/root/WorldSpawn/Player");
        myself = GetParent<Entity>();
        alive = -1;
    }

    public override void _PhysicsProcess(float delta)
    {
        alive += 1;
        currentState.PhysicsProcess(this, myself, player, alive, delta);
    }

    public void TransitionTo(IState newState)
    {
        currentState = newState;
        alive = -1;
    }

    public IState GetCurrentState()
    {
        return currentState;
    }
}
