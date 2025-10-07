using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Squad
{
    public class SquadController : MonoBehaviour
    {
        public static SquadController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        // Пример: широковещательный приказ (упрощенно)
        public void BroadcastOrder(SquadOrder order)
        {
            // В дальнейшем: хранение списка агентов, модификация их Blackboard
            // Здесь только лог
#if UNITY_EDITOR
            Debug.Log($"[SquadController] Order: {order}");
#endif
        }
    }
}
