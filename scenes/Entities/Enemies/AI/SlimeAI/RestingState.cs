using Godot;
using System;

namespace SlimeAI
{
    public class RestingState : IState
    {
        public override void PhysicsProcess(FiniteStateMachine ai, Entity myself, Player player, int alive, float delta)
        {
            if (alive == 50)
            {
                ai.TransitionTo(new JumpState());
            }
        }
    }
}
