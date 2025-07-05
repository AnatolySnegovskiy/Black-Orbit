using System;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    public class Muzzle : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
        }
    }
}
