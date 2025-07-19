using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.Core.Base; // Здесь IGameSystem

namespace Black_Orbit.Scripts.Core
{
    public class GameRunner : MonoBehaviour
    {
        public static GameRunner Instance { get; private set; }

        [Header("Системы для запуска (Префабы)")]
        [SerializeField] private List<MonoBehaviour> systemPrefabs = new();

        private readonly List<IGameSystem> _activeSystems = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSystems()
        {
            foreach (var prefab in systemPrefabs)
            {
                if (prefab is IGameSystem)
                {
                    var instance = Instantiate(prefab, transform);
                    instance.name = prefab.GetType().Name; // Для читаемости в иерархии

                    var system = instance.GetComponent<IGameSystem>();
                    if (system != null)
                    {
                        system.Initialize();
                        _activeSystems.Add(system);
#if UNITY_EDITOR
                        Debug.Log($"[GameRunner] Initialized: {instance.name}");
#endif
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[GameRunner] {prefab.name} не реализует IGameSystem и не будет инициализирован.");
#endif
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var system in _activeSystems)
            {
                try
                {
                    system.Shutdown();

                    if (system is MonoBehaviour mb && mb.gameObject.scene.IsValid())
                        Destroy(mb.gameObject);
                }
                catch (System.Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError($"[GameRunner] Error shutting down {system.GetType().Name}: {ex}");
#endif
                }
            }

            _activeSystems.Clear();
        }
    }
}
