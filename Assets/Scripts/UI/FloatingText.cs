using TMPro;
using UnityEngine;

namespace StoreSim.UI
{
    /// <summary>
    /// Всплывающий TextMesh Pro текст (например "+$300"): поднимается вверх и плавно исчезает.
    ///
    /// Рекомендуемый префаб: 3D TextMesh Pro (без Canvas):
    ///   GameObject + TextMeshPro + FloatingText
    ///
    /// Также поддерживается TextMeshProUGUI на World Space Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Floating Text")]
    public class FloatingText : MonoBehaviour
    {
        [Header("TextMesh Pro (один из двух)")]
        [Tooltip("3D TextMesh Pro в мире — предпочтительный вариант.")]
        [SerializeField] private TextMeshPro _worldText;

        [Tooltip("UI TextMesh Pro на Canvas (World Space).")]
        [SerializeField] private TextMeshProUGUI _uiText;

        [Header("Анимация")]
        [SerializeField] private float _lifetime = 1.2f;
        [SerializeField] private float _riseSpeed = 1.2f;
        [SerializeField] private bool _faceCamera = true;
        [SerializeField] private bool _animate = true;

        [Tooltip("Опционально для UI-варианта. Для 3D TMP фейд идёт через цвет текста.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        private TMP_Text _label;
        private Color _baseColor = Color.white;
        private Camera _camera;
        private float _elapsed;
        private bool _playing;

        private void Awake() => ResolveLabel();

        public void Show(string text)
        {
            ResolveLabel();

            if (_label == null)
            {
                Debug.LogWarning("FloatingText: назначь TextMeshPro или TextMeshProUGUI.", this);
                return;
            }

            _label.text = text;
            _baseColor = _label.color;
            _baseColor.a = 1f;
            _label.color = _baseColor;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _camera = Camera.main;
            _elapsed = 0f;
            _playing = true;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (!_playing)
                return;

            _elapsed += Time.deltaTime;
            float fade = Mathf.Clamp01(1f - _elapsed / _lifetime);

            if (_animate)
            {
                transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
                SetAlpha(fade);
            }

            if (_faceCamera)
            {
                if (_camera == null)
                    _camera = Camera.main;
                if (_camera != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);
            }

            if (_elapsed >= _lifetime)
            {
                _playing = false;
                Destroy(gameObject);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_label != null)
            {
                Color c = _baseColor;
                c.a = alpha;
                _label.color = c;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = alpha;
        }

        private void ResolveLabel()
        {
            if (_worldText != null)
            {
                _label = _worldText;
                return;
            }

            if (_uiText != null)
            {
                _label = _uiText;
                return;
            }

            _worldText = GetComponent<TextMeshPro>();
            if (_worldText != null)
            {
                _label = _worldText;
                return;
            }

            _uiText = GetComponentInChildren<TextMeshProUGUI>(true);
            _label = _uiText;
        }

        private void OnValidate()
        {
            _lifetime = Mathf.Max(0.1f, _lifetime);
            _riseSpeed = Mathf.Max(0f, _riseSpeed);
            ResolveLabel();
        }
    }
}
