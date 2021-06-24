using Godot;
using System;

namespace SlimeAI
{
    public class SlimeAIMachine : FiniteStateMachine
    {
        public SlimeAIMachine()
        {
            currentState = new RestingState();
        }
    }
}
