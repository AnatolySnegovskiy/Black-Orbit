using System;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    public class WeaponHandler : MonoBehaviour
    {
        [SerializeField] private Transform muzzleHolder;
        [SerializeField] private Transform leftHandHolder;
        [SerializeField] private Transform rightHandHolder;

        public Transform MuzzleHolder => muzzleHolder;
        public Transform LeftHandHolder => leftHandHolder;
        public Transform RightHandHolder => rightHandHolder; 
        void Awake()
        {
            if (muzzleHolder && !muzzleHolder.GetComponent<Muzzle>())
            {
                muzzleHolder.gameObject.AddComponent<Muzzle>();
            }
            if (leftHandHolder && !leftHandHolder.GetComponent<Hand>())
            {
                Hand hand = leftHandHolder.gameObject.AddComponent<Hand>();
                hand.HandType = Hand.Type.Left;
            }
            if (rightHandHolder && !rightHandHolder.GetComponent<Hand>())
            {
                Hand hand = rightHandHolder.gameObject.AddComponent<Hand>();
                hand.HandType = Hand.Type.Right;
            }
        }
    }
}
