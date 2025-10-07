using UnityEngine;
using Black_Orbit.Scripts.AI.ScriptableObjects;

namespace Black_Orbit.Scripts.AI.Runtime.Core
{
    /// <summary>
    /// Привязка профиля настроек к конкретному экземпляру Runtime.AI.
    /// Позволяет автоматически применять профиль на Awake/Start и вручную через кнопку/ContextMenu.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Black_Orbit.Scripts.AI.Runtime.AI))]
    public class AIProfileBinder : MonoBehaviour
    {
        [Header("Profile")]
        public AIProfile profile;

        [Header("Apply Options")] 
        public bool applyOnAwake = false;
        public bool applyOnStart = true;
        public bool applyActions = true;
        public bool applyDebug = true;

        private Black_Orbit.Scripts.AI.Runtime.AI _ai;

        private void Awake()
        {
            _ai = GetComponent<Black_Orbit.Scripts.AI.Runtime.AI>();
            if (applyOnAwake) Apply();
        }

        private void Start()
        {
            if (applyOnStart) Apply();
        }

        /// <summary>
        /// Применить текущий профиль к связанному Runtime.AI.
        /// </summary>
        public void Apply()
        {
            if (profile == null) return;
            if (_ai == null) _ai = GetComponent<Black_Orbit.Scripts.AI.Runtime.AI>();
            profile.ApplyTo(_ai, applyActions, applyDebug);
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Profile (Editor)")]
        private void ContextApply()
        {
            Apply();
            UnityEditor.EditorUtility.SetDirty(this);
            if (_ai != null) UnityEditor.EditorUtility.SetDirty(_ai);
        }
#endif
    }
}
