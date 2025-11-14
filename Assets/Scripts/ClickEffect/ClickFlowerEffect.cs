using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 클릭 / 터치한 지점 위로 UI 폭죽(꽃가루) 효과를 띄워주는 스크립트
/// - 특정 패널 위에서만 작동 가능(옵션)
/// - 파편 이미지는 풀링으로 관리하여 GC 최소화
/// - 마우스 / 터치(Input System) 모두 지원
/// </summary>
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
    [Tooltip("클릭 한 번당 생성할 파편 개수")]
    [SerializeField] private int _sparksPerClick = 14;

    [Tooltip("발사 각도 범위 (도 단위, 위쪽 반구 기준)")]
    [SerializeField] private float _minAngleDeg = 60f;
    [SerializeField] private float _maxAngleDeg = 120f;

    [Tooltip("초기 속도 범위 (픽셀/초 느낌)")]
    [SerializeField] private float _minSpeed = 220f;
    [SerializeField] private float _maxSpeed = 380f;

    [Tooltip("중력 가속도 (음수일 때 아래 방향으로 떨어짐)")]
    [SerializeField] private float _gravity = -900f;

    [Tooltip("파편 생존 시간 (초)")]
    [SerializeField] private float _sparkLifetime = 0.7f;

    [Tooltip("클릭 지점 기준, 파편 스폰 위치 랜덤 오프셋 반경")]
    [SerializeField] private float _spawnRadius = 10f;

    [Tooltip("각 파편 생성 사이의 랜덤 딜레이 최대값")]
    [SerializeField] private float _maxSpawnDelay = 0.06f;

    [Header("Rotation Settings")]
    [Tooltip("파편 시작 회전 랜덤 범위 (도). 예: 180이면 -180 ~ 180 사이")]
    [SerializeField] private float _startRotationRange = 180f;

    [Tooltip("파편 회전 속도 범위 (도/초). 양수 범위 내에서 방향은 랜덤 ±")]
    [SerializeField] private float _minAngularSpeed = 90f;
    [SerializeField] private float _maxAngularSpeed = 360f;

    [Header("Pool Settings")]
    [Tooltip("미리 만들어 둘 파편 Image 개수 (최대 동시 파편 수를 고려해서 여유 있게 설정)")]
    [SerializeField] private int _poolSize = 120;

    [Tooltip("풀에 여유가 없을 때 새로 생성해서 확장할지 여부")]
    [SerializeField] private bool _allowPoolExpand = true;

    private Camera _uiCamera;          // ScreenPoint → Canvas LocalPoint 변환에 사용
    private bool _validCanvas;         // Canvas 유효 여부

    private readonly List<Image> _pool = new List<Image>();   // 파편 풀

    private void Awake()
    {
        // 캔버스 유효성 체크 및 카메라 캐싱
        ValidateEffectCanvas();
        // 파편 풀 초기화
        InitPool();
    }

    /// <summary>
    /// 효과를 붙일 Canvas가 정상적으로 세팅되었는지 검사  
    /// - null 여부  
    /// - 씬에 실제로 존재하는지(Hierarchy 상에 있는지)  
    /// - 상위에 Canvas 컴포넌트가 있는지  
    /// - Canvas 모드에 따라 worldCamera 캐싱
    /// </summary>
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

        // ScreenSpaceOverlay는 카메라 필요 없음, 나머지는 Canvas의 worldCamera 사용
        _uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas.worldCamera;

        _validCanvas = true;
    }

    /// <summary>
    /// 파편 Image 풀 초기화  
    /// - _sparkPrefabs[0]을 기준 프리팹으로 사용  
    /// - poolSize 개수만큼 미리 생성 후 비활성화
    /// </summary>
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
            img.raycastTarget = false;         // 클릭 막지 않도록
            img.gameObject.SetActive(false);   // 풀에만 보관
            _pool.Add(img);
        }
    }

    private void Update()
    {
        if (!_validCanvas) return;
        if (_sparkPrefabs == null || _sparkPrefabs.Length == 0) return;
        if (EventSystem.current == null) return;

        // ───────────────────────────────── 마우스 클릭 ─────────────────────────────────
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            TrySpawnFirework(pos);
        }

        // ───────────────────────────────── 터치 입력 ─────────────────────────────────
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

    /// <summary>
    /// 실제 클릭/터치가 발생했을 때 폭죽을 생성할지 여부 판단  
    /// - UI Raycast로 어떤 UI 위인지 확인  
    /// - _clickPanels에 지정된 패널 안에서만 허용(옵션)  
    /// - 조건이 맞으면 SpawnFirework 호출
    /// </summary>
    private void TrySpawnFirework(Vector2 screenPos)
    {
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 아무 UI에도 닿지 않으면 폭죽 생성 안 함
        if (results.Count == 0)
            return;

        // 패널 제한이 설정된 경우, 해당 패널/자식 위에서만 허용
        if (_clickPanels != null && _clickPanels.Length > 0 && !IsOnTargetPanels(results))
            return;

        SpawnFirework(screenPos);
    }

    /// <summary>
    /// Raycast 결과가 허용된 패널(_clickPanels) 위인지 체크  
    /// - _clickPanels가 비어 있으면 항상 true 반환
    /// </summary>
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

    /// <summary>
    /// 실제 폭죽 파편 묶음을 생성하는 함수  
    /// - 클릭 지점을 Canvas 로컬 좌표로 변환  
    /// - sparksPerClick만큼 풀에서 꺼내어 각종 랜덤 값 설정 후 코루틴 재생
    /// </summary>
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

            // 각 파편마다 스타일 템플릿 랜덤 적용 (색, 스프라이트, 사이즈 등)
            ApplyRandomStyle(img);

            // 시작 위치: 클릭 지점 주변 랜덤 오프셋
            Vector2 startPos = centerLocalPos + Random.insideUnitCircle * _spawnRadius;
            rt.anchoredPosition = startPos;

            // 발사 각도 (위쪽 반구) 설정
            float angleDeg = Random.Range(_minAngleDeg, _maxAngleDeg);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // 초기 속도 벡터
            float speed = Random.Range(_minSpeed, _maxSpeed);
            Vector2 velocity = new Vector2(
                Mathf.Cos(angleRad) * speed,
                Mathf.Sin(angleRad) * speed
            );

            // 생성 딜레이 (0 ~ _maxSpawnDelay)
            float delay = (_maxSpawnDelay > 0f)
                ? Random.Range(0f, _maxSpawnDelay)
                : 0f;

            // 시작 회전 각도 (Z)
            float startRot =
                (_startRotationRange > 0f)
                ? Random.Range(-_startRotationRange, _startRotationRange)
                : 0f;

            // 회전 속도 (도/초) – 방향은 ± 랜덤
            float angularSpeed = 0f;
            if (_maxAngularSpeed > 0f)
            {
                float baseSpeed = Random.Range(_minAngularSpeed, _maxAngularSpeed);
                float dir = (Random.value < 0.5f) ? -1f : 1f;
                angularSpeed = baseSpeed * dir;
            }

            img.gameObject.SetActive(true);

            // 개별 파편 애니메이션 코루틴 시작
            StartCoroutine(SparkRoutine(img, startPos, velocity, delay, startRot, angularSpeed));
        }
    }

    /// <summary>
    /// 풀에서 비활성화된 Image 하나 꺼내오기  
    /// - 없으면 allowPoolExpand 옵션에 따라 새로 생성할 수 있음  
    /// - 그래도 없으면 null 반환
    /// </summary>
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

    /// <summary>
    /// 풀에서 꺼낸 Image의 스타일을 템플릿 프리팹 중 하나로 랜덤 복사  
    /// - sprite, color, size, pivot, anchor 등을 템플릿과 동일하게 맞춤
    /// </summary>
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

    /// <summary>
    /// 개별 폭죽 파편 하나의 생애를 담당하는 코루틴  
    /// - 딜레이 → 포물선 이동(중력 적용) → 회전 → 점점 작아짐 + 투명해짐 → 풀로 반환
    /// </summary>
    private IEnumerator SparkRoutine(
        Image img,
        Vector2 startPos,
        Vector2 initialVelocity,
        float delay,
        float startRotation,
        float angularSpeedDegPerSec)
    {
        RectTransform rt = img.rectTransform;

        // 스폰 딜레이가 있을 경우 잠시 대기
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t = 0f;
        float lifetime = _sparkLifetime;

        Vector2 pos = startPos;
        Vector2 velocity = initialVelocity;

        Color startColor = img.color;

        // 시작 시 스케일 약간 랜덤 (꽃 크기 다양하게)
        float startScale = Random.Range(0.1f, 0.5f);
        rt.localScale = Vector3.one * startScale;

        // 시작 회전 (Z축만 사용)
        float currentRot = startRotation;
        rt.localRotation = Quaternion.Euler(0f, 0f, currentRot);

        while (t < lifetime)
        {
            float dt = Time.deltaTime;
            t += dt;

            // 중력 적용
            velocity.y += _gravity * dt;

            // 위치 업데이트 (단순 포물선)
            pos += velocity * dt;
            rt.anchoredPosition = pos;

            // 회전 업데이트
            currentRot += angularSpeedDegPerSec * dt;
            rt.localRotation = Quaternion.Euler(currentRot, currentRot, currentRot);
            // 순수 Z 회전만 쓰고 싶으면 아래 라인 사용
            // rt.localRotation = Quaternion.Euler(0f, 0f, currentRot);

            // 0 ~ 1 진행률
            float n = t / lifetime;

            // 스케일 점점 줄어들기
            float scale = Mathf.Lerp(startScale, 0f, n);
            rt.localScale = Vector3.one * scale;

            // 알파 점점 감소
            var c = startColor;
            c.a = Mathf.Lerp(1f, 0f, n);
            img.color = c;

            yield return null;
        }

        // 생명 끝 → 풀로 반환 (비활성화)
        img.gameObject.SetActive(false);
    }
}
