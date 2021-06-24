using Godot;
using System;

namespace SlimeAI
{
    public class JumpState : IState
    {
        private Vector2 jumpVelocity;

        public override void PhysicsProcess(FiniteStateMachine ai, Entity myself, Player player, int alive, float delta)
        {
            if (alive == 0)
            {
                Vector2 jumpDirection;
                int jumpHeight = 900;
                // GD.Print("Slime: Me[" + myself.Position + "] Player[" + player.Position + "]");
                if (player.Position.x < myself.Position.x)
                {
                    jumpDirection = Vector2.Up.Rotated(Mathf.Deg2Rad(-30));
                }
                else
                {
                    jumpDirection = Vector2.Up.Rotated(Mathf.Deg2Rad(30));
                }

                jumpVelocity = jumpDirection * jumpHeight;
                myself.SetVelocity(jumpVelocity);
            }

            if (!myself.GetRigidBody().IsOnFloor())
            {
                myself.SetVelocity(new Vector2(jumpVelocity.x, myself.GetVelocity().y));
            }
            else
            {
                myself.SetVelocity(new Vector2(0, myself.GetVelocity().y));
            }

            if (alive == 20)
            {
                ai.TransitionTo(new RestingState());
            }
        }
    }
}
