using Godot;
using System;


// Template class
public abstract class FiniteStateMachine : Node
{
    private WorldSpawn.Future<Player> futurePlayer;

    protected IState currentState;
    private Entity myself;
    private Player player;
    private int alive;

    public override void _Ready()
    {
        this.futurePlayer = new WorldSpawn.Future<Player>();
        // this.player = WorldSpawn.ActiveWorldSpawn.GetPlayer();
        this.myself = GetParent<Entity>();
        alive = -1;
    }

    public override void _PhysicsProcess(float delta)
    {
        this.player = WorldSpawn.Future<Player>.Redeem(futurePlayer);
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
