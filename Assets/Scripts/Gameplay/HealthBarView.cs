using UnityEngine;

namespace BattleSim.Gameplay
{
    public class HealthBarView : MonoBehaviour
    {
        private Transform _fillTransform;
        private Transform _rootTransform;
        private float _width;
        private float _yOffset;
        private HealthComponent _health;

        public void Initialize(HealthComponent health, float width, float height, float yOffset)
        {
            _width = Mathf.Max(0.2f, width);
            _yOffset = yOffset;
            _health = health;

            GameObject root = new GameObject("HealthBar");
            _rootTransform = root.transform;
            _rootTransform.position = transform.position + Vector3.up * yOffset;
            _rootTransform.rotation = Quaternion.identity;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            SpriteRenderer bgRenderer = bg.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = Core.SimpleSpriteFactory.GetWhitePixel();
            bgRenderer.color = new Color(0f, 0f, 0f, 0.65f);
            bgRenderer.sortingOrder = 10;
            bg.transform.localScale = new Vector3(_width, height, 1f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(root.transform, false);
            SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = Core.SimpleSpriteFactory.GetWhitePixel();
            fillRenderer.color = new Color(0.2f, 0.95f, 0.25f, 1f);
            fillRenderer.sortingOrder = 11;
            fill.transform.localScale = new Vector3(_width, height * 0.85f, 1f);

            _fillTransform = fill.transform;
            UpdateVisual(1f);

            health.OnHealthChanged += OnHealthChanged;
        }

        private void LateUpdate()
        {
            if (_rootTransform == null)
            {
                return;
            }

            _rootTransform.position = transform.position + Vector3.up * _yOffset;
            _rootTransform.rotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= OnHealthChanged;
            }

            if (_rootTransform != null)
            {
                Destroy(_rootTransform.gameObject);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            float ratio = max <= 0f ? 0f : current / max;
            UpdateVisual(ratio);
        }

        private void UpdateVisual(float ratio)
        {
            if (_fillTransform == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            float currentWidth = Mathf.Max(0.001f, _width * ratio);
            _fillTransform.localScale = new Vector3(currentWidth, _fillTransform.localScale.y, 1f);
            _fillTransform.localPosition = new Vector3((-_width + currentWidth) * 0.5f, 0f, -0.01f);
        }
    }
}
