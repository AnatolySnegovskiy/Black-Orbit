using UnityEditor;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    public class Hand : MonoBehaviour
    {
        public enum Type
        {
            Left,
            Right
        }
        
       [SerializeField] private Type handType;
        
        public Type HandType { get => handType; set => handType = value; }
        
        private void OnDrawGizmos()
        {
            // Цвет сферы в зависимости от руки
            Gizmos.color = handType == Type.Left ? Color.cyan : Color.magenta;

            // Основная сфера в позиции руки
            Gizmos.DrawSphere(transform.position, 0.04f);

            // Показываем направление взгляда (forward)
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 0.3f);

            // Показываем "вверх" руки (обычно ладонь вверх)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 0.2f);

            // Показываем "вправо" от руки (ось локтя)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.right * 0.2f);

#if UNITY_EDITOR
            // Надпись с типом руки
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            Handles.Label(transform.position + Vector3.up * 0.05f, $"{handType} Hand", style);
#endif
        }
    }
}
