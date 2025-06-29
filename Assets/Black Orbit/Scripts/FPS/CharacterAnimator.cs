using UnityEngine;

namespace Black_Orbit.Scripts.FPS
{
    public class CharacterAnimator
    {
        private Animator _animator;

        private static readonly int YVelocity = Animator.StringToHash("Y_Velocity");
        private static readonly int XVelocity = Animator.StringToHash("X_Velocity");
        private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
        
        
        public CharacterAnimator(Animator animator)
        {
            _animator = animator;
        } 
        
        public void Move(Vector3 velocity)
        {
            _animator.SetFloat(YVelocity, velocity.z);
            _animator.SetFloat(XVelocity, velocity.x);
        }
    }
}
