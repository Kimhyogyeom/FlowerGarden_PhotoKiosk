using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class ClickFlowerEffect : MonoBehaviour
{
    [Header("Effect Canvas (씬에 있는 최상단 UI Canvas)")]
    [Tooltip("폭죽 이미지를 붙일 캔버스. 반드시 Hierarchy에 있는 Canvas의 RectTransform 할당")]
    [SerializeField] private RectTransform _effectCanvas;

    [Header("폭죽 파편 스타일 프리팹들 (Template 용도)")]
    [Tooltip("각기 다른 스프라이트/색/사이즈를 가진 Image 프리팹들. 여기 값만 복사해서 사용함.")]
    [SerializeField] private Image[] _sparkPrefabs;

    [Header("폭죽 허용 패널들")]
    [Tooltip("여기에 넣은 패널(및 자식) 위를 클릭할 때만 폭죽 생성. 비워두면 '어떤 UI 위'든 허용.")]
    [SerializeField] private RectTransform[] _clickPanels;

    [Header("Firework Settings")]
    [Tooltip("클릭 한 번당 파편 개수")]
    [SerializeField] private int _sparksPerClick = 14;

    [Tooltip("발사 각도 범위 (도 단위, 위쪽 반구)")]
    [SerializeField] private float _minAngleDeg = 60f;
    [SerializeField] private float _maxAngleDeg = 120f;

    [Tooltip("초기 속도 범위 (픽셀/초 느낌)")]
    [SerializeField] private float _minSpeed = 220f;
    [SerializeField] private float _maxSpeed = 380f;

    [Tooltip("중력 (음수로 아래로 끌어당김)")]
    [SerializeField] private float _gravity = -900f;

    [Tooltip("파편 생존 시간 (초)")]
    [SerializeField] private float _sparkLifetime = 0.7f;

    [Tooltip("초기 위치 랜덤 오프셋 반경")]
    [SerializeField] private float _spawnRadius = 10f;

    [Tooltip("파편 생성 딜레이 랜덤 (0 ~ max)")]
    [SerializeField] private float _maxSpawnDelay = 0.06f;

    [Header("Rotation Settings")]
    [Tooltip("파편 시작 회전 랜덤 범위 (도). 예: 180이면 -180 ~ 180 사이")]
    [SerializeField] private float _startRotationRange = 180f;

    [Tooltip("파편 회전 속도 범위 (도/초). 양수로 넣으면 방향은 랜덤")]
    [SerializeField] private float _minAngularSpeed = 90f;
    [SerializeField] private float _maxAngularSpeed = 360f;

    [Header("Pool Settings")]
    [Tooltip("미리 만들어 둘 파편 Image 개수 (최대 동시 파편 수 여유 있게)")]
    [SerializeField] private int _poolSize = 120;

    [Tooltip("풀 가득 찼을 때 새로 생성해서 확장할지 여부")]
    [SerializeField] private bool _allowPoolExpand = true;

    private Camera _uiCamera;
    private bool _validCanvas;

    private readonly List<Image> _pool = new List<Image>();

    private void Awake()
    {
        ValidateEffectCanvas();
        InitPool();
    }

    private void ValidateEffectCanvas()
    {
        _validCanvas = false;

        if (_effectCanvas == null)
        {
            Debug.LogError("[ClickFlowerEffect] _effectCanvas is null. 씬의 Canvas를 드래그해서 넣어주세요.");
            return;
        }

        if (!_effectCanvas.gameObject.scene.IsValid())
        {
            Debug.LogError("[ClickFlowerEffect] _effectCanvas가 Prefab/Asset입니다. Hierarchy에 있는 Canvas를 넣어주세요.");
            return;
        }

        var canvas = _effectCanvas.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ClickFlowerEffect] _effectCanvas 상위에서 Canvas를 찾지 못했습니다.");
            return;
        }

        _uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas.worldCamera;

        _validCanvas = true;
    }

    private void InitPool()
    {
        if (!_validCanvas) return;

        if (_sparkPrefabs == null || _sparkPrefabs.Length == 0)
        {
            Debug.LogError("[ClickFlowerEffect] _sparkPrefabs 비어있음. 최소 1개는 스타일 프리팹을 넣어주세요.");
            return;
        }

        _pool.Clear();

        // 기준 프리팹 하나를 사용해 동일한 구조의 Image를 미리 생성
        var basePrefab = _sparkPrefabs[0];

        for (int i = 0; i < _poolSize; i++)
        {
            var img = Instantiate(basePrefab, _effectCanvas);
            img.name = $"SparkPoolItem_{i}";
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            _pool.Add(img);


        }
    }

    private void Update()
    {
        if (!_validCanvas) return;
        if (_sparkPrefabs == null || _sparkPrefabs.Length == 0) return;
        if (EventSystem.current == null) return;

        // 마우스 클릭
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            TrySpawnFirework(pos);
        }

        // 터치 입력
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 pos = touch.position.ReadValue();
                    TrySpawnFirework(pos);
                }
            }
        }
    }

    private void TrySpawnFirework(Vector2 screenPos)
    {
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
            return;

        // 패널 제한이 있다면 검사
        if (_clickPanels != null && _clickPanels.Length > 0 && !IsOnTargetPanels(results))
            return;

        SpawnFirework(screenPos);
    }

    private bool IsOnTargetPanels(List<RaycastResult> results)
    {
        if (_clickPanels == null || _clickPanels.Length == 0)
            return true;

        foreach (var r in results)
        {
            var t = r.gameObject.transform;
            while (t != null)
            {
                for (int i = 0; i < _clickPanels.Length; i++)
                {
                    if (_clickPanels[i] != null && t == _clickPanels[i])
                        return true;
                }
                t = t.parent;
            }
        }

        return false;
    }

    private void SpawnFirework(Vector2 screenPos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _effectCanvas,
                screenPos,
                _uiCamera,
                out Vector2 centerLocalPos))
        {
            return;
        }

        for (int i = 0; i < _sparksPerClick; i++)
        {
            var img = GetPooledImage();
            if (img == null)
                continue;

            var rt = img.rectTransform;

            // 스타일 템플릿 랜덤 적용
            ApplyRandomStyle(img);

            // 시작 위치: 클릭 지점 주변 (로컬 좌표)
            Vector2 startPos = centerLocalPos + Random.insideUnitCircle * _spawnRadius;
            rt.anchoredPosition = startPos;

            // 발사 각도 (위쪽 반원)
            float angleDeg = Random.Range(_minAngleDeg, _maxAngleDeg);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // 속도
            float speed = Random.Range(_minSpeed, _maxSpeed);
            Vector2 velocity = new Vector2(
                Mathf.Cos(angleRad) * speed,
                Mathf.Sin(angleRad) * speed
            );

            // 생성 딜레이
            float delay = (_maxSpawnDelay > 0f)
                ? Random.Range(0f, _maxSpawnDelay)
                : 0f;

            // 시작 회전 랜덤
            float startRot =
                (_startRotationRange > 0f)
                ? Random.Range(-_startRotationRange, _startRotationRange)
                : 0f;

            // 회전 속도 랜덤 (방향 포함)
            float angularSpeed = 0f;
            if (_maxAngularSpeed > 0f)
            {
                float baseSpeed = Random.Range(_minAngularSpeed, _maxAngularSpeed);
                float dir = (Random.value < 0.5f) ? -1f : 1f;
                angularSpeed = baseSpeed * dir;
            }

            img.gameObject.SetActive(true);

            StartCoroutine(SparkRoutine(img, startPos, velocity, delay, startRot, angularSpeed));
        }
    }

    private Image GetPooledImage()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        if (_allowPoolExpand && _sparkPrefabs != null && _sparkPrefabs.Length > 0)
        {
            // 부족하면 하나 더 만든다 (옵션)
            var basePrefab = _sparkPrefabs[0];
            var img = Instantiate(basePrefab, _effectCanvas);
            img.name = $"SparkPoolItem_Extra_{_pool.Count}";
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            _pool.Add(img);
            return img;
        }

        return null; // 사용 가능한 파편 없음
    }

    private void ApplyRandomStyle(Image img)
    {
        if (_sparkPrefabs == null || _sparkPrefabs.Length == 0)
            return;

        var template = _sparkPrefabs[Random.Range(0, _sparkPrefabs.Length)];
        if (template == null) return;

        var rt = img.rectTransform;
        var trt = template.rectTransform;

        img.sprite = template.sprite;
        img.color = template.color;

        // 사이즈 & 피벗 & 앵커 동기화(필요에 따라)
        rt.sizeDelta = trt.sizeDelta;
        rt.pivot = trt.pivot;
        rt.anchorMin = trt.anchorMin;
        rt.anchorMax = trt.anchorMax;
    }

    private IEnumerator SparkRoutine(
        Image img,
        Vector2 startPos,
        Vector2 initialVelocity,
        float delay,
        float startRotation,
        float angularSpeedDegPerSec)
    {
        RectTransform rt = img.rectTransform;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t = 0f;
        float lifetime = _sparkLifetime;

        Vector2 pos = startPos;
        Vector2 velocity = initialVelocity;

        Color startColor = img.color;
        float startScale = Random.Range(0.1f, 0.5f);
        rt.localScale = Vector3.one * startScale;

        // 시작 회전 (Z축만)
        float currentRot = startRotation;
        rt.localRotation = Quaternion.Euler(0f, 0f, currentRot);

        while (t < lifetime)
        {
            float dt = Time.deltaTime;
            t += dt;

            // 중력
            velocity.y += _gravity * dt;
            pos += velocity * dt;
            rt.anchoredPosition = pos;

            // 회전
            currentRot += angularSpeedDegPerSec * dt;
            rt.localRotation = Quaternion.Euler(currentRot, currentRot, currentRot);
            //rt.localRotation = Quaternion.Euler(0f, 0f, currentRot); Test

            // 점점 작아지고 투명해짐
            float n = t / lifetime;
            float scale = Mathf.Lerp(startScale, 0f, n);
            rt.localScale = Vector3.one * scale;

            var c = startColor;
            c.a = Mathf.Lerp(1f, 0f, n);
            img.color = c;

            yield return null;
        }

        // 리셋 & 풀로 반환
        img.gameObject.SetActive(false);
    }
}
