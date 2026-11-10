using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && ARSENAL_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace Arsenal.Game.InputHelpers
{
    /// <summary>Instantané des pointeurs (doigts ou souris) pour la frame courante.</summary>
    public struct PointerFrame
    {
        public int Count;
        public Vector2 Position0, Delta0, Position1, Delta1;
        public bool Began0;
        public bool Began1;
        public float Scroll;
        public bool IsMouse;
    }

    /// <summary>
    /// Abstraction tactile / souris compatible avec le nouveau Input System ET l'ancien gestionnaire,
    /// quel que soit le réglage "Active Input Handling" du projet. Sur Windows, la souris simule le doigt :
    /// clic gauche + glisser = un doigt, molette = pincement.
    /// </summary>
    public static class PointerInput
    {
        private static int _frame = -1;
        private static PointerFrame _current;
#if !(ENABLE_INPUT_SYSTEM && ARSENAL_INPUT_SYSTEM) && ENABLE_LEGACY_INPUT_MANAGER
        private static Vector2 _lastMouse;
#endif
        private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>();

        public static PointerFrame Current
        {
            get
            {
                if (_frame != Time.frameCount)
                {
                    _frame = Time.frameCount;
                    _current = Sample();
                }
                return _current;
            }
        }

        private static PointerFrame Sample()
        {
            var f = new PointerFrame();
#if ENABLE_INPUT_SYSTEM && ARSENAL_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
            var touches = ETouch.activeTouches;
            if (touches.Count > 0)
            {
                f.Count = touches.Count;
                f.Position0 = touches[0].screenPosition;
                f.Delta0 = touches[0].delta;
                f.Began0 = touches[0].phase == ETouchPhase.Began;
                if (touches.Count > 1)
                {
                    f.Position1 = touches[1].screenPosition;
                    f.Delta1 = touches[1].delta;
                    f.Began1 = touches[1].phase == ETouchPhase.Began;
                }
                return f;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                f.IsMouse = true;
                float rawScroll = mouse.scroll.ReadValue().y;
                // Selon la version/plateforme, la molette renvoie ±120 par cran ou une valeur normalisée ±1.
                f.Scroll = Mathf.Abs(rawScroll) > 10f ? rawScroll / 120f : rawScroll;
                if (mouse.leftButton.isPressed)
                {
                    f.Count = 1;
                    f.Position0 = mouse.position.ReadValue();
                    f.Delta0 = mouse.delta.ReadValue();
                    f.Began0 = mouse.leftButton.wasPressedThisFrame;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                var t0 = Input.GetTouch(0);
                f.Count = Input.touchCount;
                f.Position0 = t0.position;
                f.Delta0 = t0.deltaPosition;
                f.Began0 = t0.phase == UnityEngine.TouchPhase.Began;
                if (Input.touchCount > 1)
                {
                    var t1 = Input.GetTouch(1);
                    f.Position1 = t1.position;
                    f.Delta1 = t1.deltaPosition;
                    f.Began1 = t1.phase == UnityEngine.TouchPhase.Began;
                }
                return f;
            }
            f.IsMouse = true;
            f.Scroll = Input.mouseScrollDelta.y;
            Vector2 mp = Input.mousePosition;
            if (Input.GetMouseButton(0))
            {
                f.Count = 1;
                f.Position0 = mp;
                f.Began0 = Input.GetMouseButtonDown(0);
                f.Delta0 = f.Began0 ? Vector2.zero : mp - _lastMouse;
            }
            _lastMouse = mp;
#endif
            return f;
        }

        /// <summary>Vrai si la position écran touche un élément d'interface (évite de pivoter l'arme en cliquant un bouton).</summary>
        public static bool IsOverUi(Vector2 screenPosition)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPosition };
            RaycastBuffer.Clear();
            es.RaycastAll(data, RaycastBuffer);
            return RaycastBuffer.Count > 0;
        }

        /// <summary>Crée l'EventSystem avec le module d'entrée adapté au projet.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && ARSENAL_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
