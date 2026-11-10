using Arsenal.Game.InputHelpers;
using UnityEngine;

namespace Arsenal.Game.ArsenalScene
{
    /// <summary>
    /// Présentoir interactif : rotation au doigt avec inertie, pincement/molette pour zoomer,
    /// glissement à deux doigts pour se déplacer le long de l'arme, double-toucher pour recentrer,
    /// rotation lente automatique après quelques secondes d'inactivité.
    /// </summary>
    public class WeaponTurntableViewer : MonoBehaviour
    {
        public Camera ViewCamera;
        /// <summary>Pivot qui reçoit l'arme (tourné par le joueur).</summary>
        public Transform Pivot;

        public float RotateSpeed = 0.25f;
        public float PitchLimit = 40f;
        public float IdleDelay = 4f;
        public float IdleSpeed = 9f;
        public float Damping = 5f;

        private float _yaw = 90f, _pitch = 8f;
        private float _yawVel, _pitchVel;
        private float _zoom = 1f, _zoomTarget = 1f;
        private Vector3 _pan, _panTarget;
        private float _baseDistance = 1f;
        private float _halfLength = 0.3f;
        private float _lastInteraction = -999f;
        private float _lastTap = -999f;
        private bool _dragging;
        private float _intro;
        private Vector3 _cameraDirection;
        private Vector3 _pivotHome;

        private void Start()
        {
            _pivotHome = Pivot.position;
            _cameraDirection = (ViewCamera.transform.position - Pivot.position).normalized;
        }

        /// <summary>Recadre la caméra pour un objet de cette taille (rayon englobant) et lance l'animation d'entrée.</summary>
        public void Frame(Bounds localBounds)
        {
            float radius = Mathf.Max(0.08f, localBounds.extents.magnitude);
            _halfLength = Mathf.Max(localBounds.extents.x, localBounds.extents.z);
            float fov = ViewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            // Le cadrage tient compte des panneaux d'interface latéraux (l'arme occupe ~45 % de la largeur).
            float aspect = Mathf.Max(1f, ViewCamera.aspect);
            float horizontalFov = Mathf.Atan(Mathf.Tan(fov) * aspect * 0.45f);
            _baseDistance = radius / Mathf.Sin(Mathf.Min(fov, horizontalFov)) * 1.05f;
            _zoomTarget = 1f;
            _panTarget = Vector3.zero;
            _intro = 1f;
        }

        public void ResetView()
        {
            _yaw = 90f; _pitch = 8f;
            _yawVel = _pitchVel = 0f;
            _zoomTarget = 1f;
            _panTarget = Vector3.zero;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            HandleInput(dt);

            if (!_dragging)
            {
                _yaw += _yawVel * dt;
                _pitch += _pitchVel * dt;
                float decay = Mathf.Exp(-Damping * dt);
                _yawVel *= decay;
                _pitchVel *= decay;
                if (Time.unscaledTime - _lastInteraction > IdleDelay)
                {
                    _yaw += IdleSpeed * dt;
                    _pitch = Mathf.Lerp(_pitch, 8f, dt * 0.5f);
                    _panTarget = Vector3.Lerp(_panTarget, Vector3.zero, dt);
                }
            }
            _pitch = Mathf.Clamp(_pitch, -PitchLimit, PitchLimit);

            // Animation d'entrée : rotation rapide qui ralentit.
            if (_intro > 0f)
            {
                _intro = Mathf.Max(0f, _intro - dt * 1.4f);
                _yaw += _intro * _intro * 360f * dt;
            }

            _zoom = Mathf.Lerp(_zoom, _zoomTarget, 1f - Mathf.Exp(-10f * dt));
            _pan = Vector3.Lerp(_pan, _panTarget, 1f - Mathf.Exp(-10f * dt));

            Pivot.rotation = Quaternion.AngleAxis(_pitch, ViewCamera.transform.right) * Quaternion.Euler(0f, _yaw, 0f);
            float bob = Mathf.Sin(Time.unscaledTime * 1.1f) * 0.006f;
            Pivot.position = _pivotHome + Vector3.up * bob;

            var target = _pivotHome + _pan;
            ViewCamera.transform.position = target + _cameraDirection * (_baseDistance * _zoom);
            ViewCamera.transform.rotation = Quaternion.LookRotation(target - ViewCamera.transform.position, Vector3.up);
        }

        private void HandleInput(float dt)
        {
            var f = PointerInput.Current;

            if (f.Scroll != 0f)
            {
                _zoomTarget = Mathf.Clamp(_zoomTarget * (1f - f.Scroll * 0.12f), 0.35f, 1.6f);
                _lastInteraction = Time.unscaledTime;
            }

            if (f.Count == 0) { _dragging = false; return; }

            if (f.Began0)
            {
                if (PointerInput.IsOverUi(f.Position0)) { _dragging = false; return; }
                _dragging = true;
                if (Time.unscaledTime - _lastTap < 0.3f) { ResetView(); _lastTap = -999f; }
                else _lastTap = Time.unscaledTime;
            }
            if (!_dragging) return;
            _lastInteraction = Time.unscaledTime;

            float scale = 1080f / Mathf.Max(1, Screen.height);
            if (f.Count == 1)
            {
                var d = f.Delta0 * scale;
                _yaw -= d.x * RotateSpeed;
                _pitch += d.y * RotateSpeed;
                if (dt > 0f)
                {
                    _yawVel = Mathf.Lerp(_yawVel, -d.x * RotateSpeed / dt, 0.5f);
                    _pitchVel = Mathf.Lerp(_pitchVel, d.y * RotateSpeed / dt, 0.5f);
                }
            }
            else
            {
                // Pincement
                var prev0 = f.Position0 - f.Delta0;
                var prev1 = f.Position1 - f.Delta1;
                float prevDist = Vector2.Distance(prev0, prev1);
                float dist = Vector2.Distance(f.Position0, f.Position1);
                if (prevDist > 10f && !f.Began1) _zoomTarget = Mathf.Clamp(_zoomTarget * (prevDist / dist), 0.35f, 1.6f);
                // Glissement à deux doigts : déplacement le long de l'arme
                var avg = (f.Delta0 + f.Delta1) * 0.5f * scale;
                var right = ViewCamera.transform.right;
                var up = ViewCamera.transform.up;
                float k = _baseDistance * _zoom * 0.0012f;
                _panTarget -= (right * avg.x + up * avg.y) * k;
                float limit = _halfLength * 0.9f;
                _panTarget = Vector3.ClampMagnitude(_panTarget, limit);
                _yawVel = _pitchVel = 0f;
            }
        }
    }
}
