public abstract class IState
{
    public abstract void PhysicsProcess(FiniteStateMachine ai, Entity myself, Player player, int alive, float delta);
}
