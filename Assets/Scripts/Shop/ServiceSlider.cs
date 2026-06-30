using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StoreSim.Shop
{
    /// <summary>
    /// Скрипт на префабе слайдера обслуживания (World Space Canvas), который спавнится над головой NPC.
    /// Прогресс анимируется кодом за заданную длительность, по завершении дёргается коллбэк.
    ///
    /// Поддерживает два способа получить событие окончания:
    ///   1) UnityEvent <see cref="_onComplete"/> — настраивается в инспекторе.
    ///   2) Action onComplete — передаётся в <see cref="Play"/> из кода (его использует CheckoutManager).
    ///
    /// Если в проекте есть DOTween/LeanTween — внутренний корутинный твин можно заменить на них,
    /// но колбэк-контракт (Play(duration, onComplete)) остаётся тем же.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("StoreSim/Service Slider")]
    public class ServiceSlider : MonoBehaviour
    {
        [Header("Визуал прогресса (любой из них)")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fillImage;

        [Header("Поведение")]
        [Tooltip("Поворачивать канвас лицом к камере каждый кадр.")]
        [SerializeField] private bool _faceCamera = true;

        [Tooltip("Событие окончания анимации (можно подписаться в инспекторе).")]
        [SerializeField] private UnityEvent _onComplete;

        private Transform _follow;
        private Vector3 _worldOffset;
        private Camera _camera;
        private bool _running;

        private void Awake()
        {
            if (_slider == null)
                _slider = GetComponentInChildren<Slider>(true);
            if (_fillImage == null && _slider != null && _slider.fillRect != null)
                _fillImage = _slider.fillRect.GetComponent<Image>();
        }

        /// <summary>
        /// Запускает анимацию прогресса.
        /// </summary>
        /// <param name="duration">Длительность обслуживания, сек.</param>
        /// <param name="onComplete">Коллбэк по завершении (начисление денег).</param>
        /// <param name="follow">Кость/якорь над головой NPC, за которым слайдер следует. Может быть null.</param>
        /// <param name="worldOffset">Смещение относительно follow.</param>
        public void Play(float duration, Action onComplete = null, Transform follow = null, Vector3 worldOffset = default)
        {
            _follow = follow;
            _worldOffset = worldOffset;
            _camera = Camera.main;

            if (follow != null)
                transform.position = follow.position + worldOffset;

            if (_running)
                StopAllCoroutines();

            StartCoroutine(Run(Mathf.Max(0.01f, duration), onComplete));
        }

        private IEnumerator Run(float duration, Action onComplete)
        {
            _running = true;
            SetProgress(0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetProgress(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            SetProgress(1f);
            _running = false;

            _onComplete?.Invoke();
            onComplete?.Invoke();
        }

        private void SetProgress(float value01)
        {
            if (_slider != null)
                _slider.value = value01;
            if (_fillImage != null)
                _fillImage.fillAmount = value01;
        }

        private void LateUpdate()
        {
            if (_follow != null)
                transform.position = _follow.position + _worldOffset;

            if (_faceCamera)
            {
                if (_camera == null)
                    _camera = Camera.main;
                if (_camera != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);
            }
        }
    }
}
