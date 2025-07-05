using Unity.Mathematics;
using UnityEngine;

namespace Black_Orbit.Scripts.FPS
{
    public class CharacterAnimator
    {
        private Animator _animator;

        private static readonly int YVelocity = Animator.StringToHash("Y_Velocity");
        private static readonly int XVelocity = Animator.StringToHash("X_Velocity");
        private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int IsRunning = Animator.StringToHash("IsRunning"); 
        
        
        public CharacterAnimator(Animator animator)
        {
            _animator = animator;
        } 
        
        public void Move(Vector2 velocity)
        {
            _animator.SetFloat(YVelocity, math.lerp(_animator.GetFloat(YVelocity),  velocity.y, 0.5f));
            _animator.SetFloat(XVelocity, math.lerp(_animator.GetFloat(XVelocity),  velocity.x, 0.5f));
        }

        public void Crouch(bool isCrouching)
        {
            _animator.SetBool(IsCrouching, isCrouching);
        }

        public void Run(bool isRunning)
        {
            _animator.SetBool(IsRunning, isRunning);
        }
    }
}
