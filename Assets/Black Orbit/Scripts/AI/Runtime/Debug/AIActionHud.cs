using System.Text;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime;
using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;

namespace Black_Orbit.Scripts.AI.Runtime.Debugging
{
    /// <summary>
    /// Простое overlay-HUD для отладки: текущее действие AI, каналы и подавление.
    /// Добавьте один экземпляр на сцену или повесьте на Camera.
    /// Горячая клавиша: F8 — включить/выключить.
    /// </summary>
    public class AIActionHud : MonoBehaviour
    {
        [Tooltip("Цель AI для отображения. Если не задан, возьмём ближайшего к камере.")]
        public AI targetAI;

        [Tooltip("Макс. дистанция поиска AI от камеры, если targetAI не указан")] public float searchRadius = 50f;
        [Tooltip("Масштаб GUI")] public float guiScale = 1f;
        [Tooltip("Прозрачность плашки")] [Range(0f,1f)] public float alpha = 0.9f;
        [Tooltip("Точка экрана (пиксели) для верхнего левого угла")] public Vector2 screenPos = new Vector2(16, 16);

        [Tooltip("Как часто переоценивать/переискать цель (сек)")] public float reacquireInterval = 0.5f;

        private bool _enabled = true;
        private Camera _cam;
        private GUIStyle _label;
        private readonly StringBuilder _sb = new StringBuilder(256);
        private float _reacquireTimer = 0f;

        void Awake()
        {
            _cam = Camera.main;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) _enabled = !_enabled;
            if (!_enabled) return;
            _reacquireTimer -= Time.deltaTime;
            bool needsReacquire = targetAI == null
                                  || !targetAI.gameObject.activeInHierarchy
                                  || (_cam != null && Vector3.Distance(_cam.transform.position, targetAI.transform.position) > searchRadius)
                                  || _reacquireTimer <= 0f;
            if (needsReacquire)
            {
                targetAI = FindBestAI();
                _reacquireTimer = reacquireInterval;
            }
        }

        AI FindBestAI()
        {
            var cam = _cam != null ? _cam : Camera.main;
            if (cam == null) return null;
            var all = FindObjectsOfType<AI>();
            AI best = null; float bestDot = -1f;
            foreach (var ai in all)
            {
                if (ai == null || !ai.gameObject.activeInHierarchy) continue;
                float dist = Vector3.Distance(cam.transform.position, ai.transform.position);
                if (dist > searchRadius) continue;
                Vector3 dir = (ai.transform.position - cam.transform.position).normalized;
                float dot = Vector3.Dot(cam.transform.forward, dir);
                if (dot > bestDot) { bestDot = dot; best = ai; }
            }
            return best;
        }

        void OnGUI()
        {
            if (!_enabled || targetAI == null) return;
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1));

            var rect = new Rect(screenPos.x, screenPos.y, 420, 220);
            var bg = new Color(0f, 0f, 0f, alpha);
            EditorLikeBox(rect, bg);

            _sb.Length = 0;
            _sb.AppendLine($"<b>AI:</b> {targetAI.name}");
            _sb.AppendLine($"<b>Suppression:</b> {targetAI.SuppressionLevel:F2}");
            _sb.AppendLine($"<b>Target:</b> {targetAI.AttackTarget?.name ?? "<none>"}");
            _sb.AppendLine($"<b>Current Action:</b> {targetAI.CurrentAction?.name ?? "<none>"}");
            _sb.AppendLine($"<b>Movement:</b> {targetAI.CurrentMovementAction?.name ?? "<none>"}");
            _sb.AppendLine($"<b>Combat:</b> {targetAI.CurrentCombatAction?.name ?? "<none>"}");
            _sb.AppendLine($"Hotkey: F8 — toggle, Click HUD to pin/unpin target");

            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), _sb.ToString(), _label);

            GUI.matrix = old;
        }

        static void EditorLikeBox(Rect r, Color bg)
        {
            Color old = GUI.color;
            GUI.color = bg;
            GUI.Box(r, GUIContent.none);
            GUI.color = old;
        }

        void OnMouseDown()
        {
            // щелчок по HUD — открепление/закрепление цели
            targetAI = targetAI == null ? FindBestAI() : targetAI;
        }
    }
}
