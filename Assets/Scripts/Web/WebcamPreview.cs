using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

/// <summary>
/// 웹캠 프리뷰 컨트롤러
/// - 선택한 WebCam 장치를 RawImage 에 실시간으로 출력
/// - 1920x1080 FHD 고정
/// - 활성화 체크 오브젝트가 꺼져있으면 실행 안 함
/// - 회전/뒤집기 값이 변경될 때만 UI 갱신(불필요 연산 감소)
/// </summary>
public class WebcamPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage _webcamTarget;

    [Header("Activation Control")]
    [SerializeField] private GameObject _activationCheckObject;
    [Tooltip("이 오브젝트가 비활성화되어 있으면 웹캠이 시작/업데이트되지 않음 (Inspector에서 설정)")]

    [Header("Camera Selection")]
    [SerializeField] private string _preferredDeviceKeyword = "C922";
    [SerializeField] private int _requestedWidth = 1920;   // FHD 고정
    [SerializeField] private int _requestedHeight = 1080;
    [SerializeField] private int _requestedFps = 30;

    [Header("Mirror Settings")]
    [SerializeField] private bool _mirrorHorizontal = true;

    [Header("Performance Settings")]
    [SerializeField] private bool _preInitializeOnStart = true;
    [Tooltip("게임 시작 시 웹캠을 미리 초기화 (렉 방지, 권장)")]

    private WebCamTexture _tex;
    private bool _isWebcamInitialized = false;
    private CanvasGroup _canvasGroup;
    private bool _isVisible = false;

    // 회전/플립 값 캐싱용
    private int _lastRotation = -999;
    private bool _lastVerticallyMirrored = false;
    private bool _lastHorizontalMirrored = false;

    // 현재 장치 이름
    private string _currentDeviceName;

    private void Start()
    {
        // CanvasGroup 가져오기 (없으면 추가)
        _canvasGroup = _webcamTarget.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = _webcamTarget.gameObject.AddComponent<CanvasGroup>();

        if (_preInitializeOnStart)
        {
            // 게임 시작 시 미리 초기화 (렉 방지)
            Debug.Log("[WebcamPreview] 게임 시작 시 웹캠 미리 초기화 (렉 방지)");
            InitAndStartWebcam();

            // 초기에는 숨김 (활성화 체크 오브젝트가 켜질 때만 보임)
            _canvasGroup.alpha = 0f;
            _isVisible = false;
        }
        else
        {
            // 기존 방식: 활성화 체크 오브젝트가 켜져있으면 초기화
            if (ShouldWebcamBeActive())
            {
                InitAndStartWebcam();
            }
        }
    }

    private void Update()
    {
        // === Pre-Initialize 모드 ===
        if (_preInitializeOnStart)
        {
            bool shouldBeVisible = ShouldWebcamBeActive();

            // 보여야 하는데 안 보임 → 페이드인
            if (shouldBeVisible && !_isVisible)
            {
                StartCoroutine(FadeIn());
                _isVisible = true;
            }
            // 숨겨야 하는데 보임 → 페이드아웃
            else if (!shouldBeVisible && _isVisible)
            {
                StartCoroutine(FadeOut());
                _isVisible = false;
            }
        }
        // === 기존 방식 ===
        else
        {
            bool shouldBeActive = ShouldWebcamBeActive();

            if (shouldBeActive && !_isWebcamInitialized)
            {
                // 켜야 하는데 아직 초기화 안 됨 → 초기화
                InitAndStartWebcam();
            }
            else if (!shouldBeActive && _isWebcamInitialized)
            {
                // 꺼야 하는데 켜져 있음 → 정지
                StopAndDisposeWebcam();
            }
        }
    }

    private void LateUpdate()
    {
        // 웹캠이 초기화되지 않았거나 재생 중이 아니면 스킵
        if (!_isWebcamInitialized || _tex == null || !_tex.isPlaying)
            return;

        // 카메라의 회전 각도 및 상하 반전 여부 읽기
        int rot = _tex.videoRotationAngle;
        bool vert = _tex.videoVerticallyMirrored;

        // === 좌우반전 값이 바뀐 경우에만 UV 갱신 ===
        if (_mirrorHorizontal != _lastHorizontalMirrored)
        {
            var uv = _webcamTarget.uvRect;
            uv.x = _mirrorHorizontal ? 1f : 0f;
            uv.width = _mirrorHorizontal ? -1f : 1f;
            _webcamTarget.uvRect = uv;

            _lastHorizontalMirrored = _mirrorHorizontal;
        }

        // === 상하 반전 값이 바뀐 경우에만 UV 갱신 ===
        if (vert != _lastVerticallyMirrored)
        {
            var uv = _webcamTarget.uvRect;
            uv.y = vert ? 1f : 0f;
            uv.height = vert ? -1f : 1f;
            _webcamTarget.uvRect = uv;

            _lastVerticallyMirrored = vert;
        }

        // === 회전 값이 바뀐 경우에만 RectTransform 회전 갱신 ===
        if (rot != _lastRotation)
        {
            _webcamTarget.rectTransform.localEulerAngles = new Vector3(0f, 0f, -rot);
            _lastRotation = rot;
        }
    }

    /// <summary>
    /// 활성화 체크 오브젝트가 활성화되어 있는지 확인
    /// </summary>
    private bool ShouldWebcamBeActive()
    {
        if (_activationCheckObject == null)
            return true; // null이면 항상 활성화

        return _activationCheckObject.activeInHierarchy;
    }

    /// <summary>
    /// 웹캠 초기화 + 재생
    /// </summary>
    private void InitAndStartWebcam()
    {
        if (_webcamTarget == null)
        {
            Debug.LogError("[WebcamPreview] _webcamTarget이 비어있습니다.");
            return;
        }

        // 현재 연결된 웹캠 장치 목록 가져오기
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("[WebcamPreview] WebCam 장치를 찾을 수 없습니다.");
            return;
        }

        // 첫 시작: _preferredDeviceKeyword 를 포함하는 장치 우선 선택
        // 재시작: 동일한 장치 사용
        if (string.IsNullOrEmpty(_currentDeviceName))
        {
            var dev = devices.FirstOrDefault(d => d.name.Contains(_preferredDeviceKeyword));
            if (string.IsNullOrEmpty(dev.name))
                dev = devices[0];

            _currentDeviceName = dev.name;
        }

        Debug.Log($"[WebcamPreview] 사용 장치: {_currentDeviceName}");
        Debug.Log($"[WebcamPreview] 요청 해상도: FHD ({_requestedWidth}x{_requestedHeight})");

        // WebCamTexture 생성 (요청 해상도 / FPS)
        _tex = new WebCamTexture(_currentDeviceName, _requestedWidth, _requestedHeight, _requestedFps);

        // 텍스처 품질 설정 (화질 개선)
        _tex.filterMode = FilterMode.Bilinear;  // Bilinear 필터링 (부드럽게)
        _tex.wrapMode = TextureWrapMode.Clamp;   // 가장자리 처리

        // RawImage 에 텍스처 연결 후 재생
        _webcamTarget.texture = _tex;
        _tex.Play();

        _isWebcamInitialized = true;

        // 실제 해상도 로그 출력 (1프레임 후 확인 필요)
        StartCoroutine(LogActualResolution());

        // 초기 값 리셋
        _lastRotation = -999;
        _lastVerticallyMirrored = !_tex.videoVerticallyMirrored;
        _lastHorizontalMirrored = !_mirrorHorizontal;

        Debug.Log("[WebcamPreview] 웹캠 초기화 완료");
    }

    private void StopAndDisposeWebcam()
    {
        if (_tex != null)
        {
            if (_tex.isPlaying)
                _tex.Stop();

            Destroy(_tex);
            _tex = null;
        }

        _isWebcamInitialized = false;
        Debug.Log("[WebcamPreview] 웹캠 정지");
    }

    private void OnDisable()
    {
        StopAndDisposeWebcam();
    }

    private void OnApplicationQuit()
    {
        StopAndDisposeWebcam();
    }

    // ===== Public API =====

    /// <summary>
    /// 외부에서 WebCamTexture를 가져갈 수 있게 하는 getter
    /// </summary>
    public WebCamTexture GetTexture()
    {
        return _tex;
    }

    /// <summary>
    /// 좌우반전 토글 (외부에서 호출 가능)
    /// </summary>
    public void ToggleMirror()
    {
        _mirrorHorizontal = !_mirrorHorizontal;
    }

    /// <summary>
    /// 좌우반전 설정
    /// </summary>
    public void SetMirror(bool mirror)
    {
        _mirrorHorizontal = mirror;
    }

    /// <summary>
    /// 외부에서 웹캠을 강제로 재시작
    /// </summary>
    public void RestartWebcam()
    {
        StopAndDisposeWebcam();
        if (ShouldWebcamBeActive())
        {
            InitAndStartWebcam();
        }
    }

    /// <summary>
    /// 페이드인 (Pre-Initialize 모드용)
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (_canvasGroup == null)
            yield break;

        float fadeDuration = 0.3f;
        float fadeElapsed = 0f;

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeElapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        Debug.Log("[WebcamPreview] 페이드인 완료");
    }

    /// <summary>
    /// 페이드아웃 (Pre-Initialize 모드용)
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (_canvasGroup == null)
            yield break;

        float fadeDuration = 0.3f;
        float fadeElapsed = 0f;

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        Debug.Log("[WebcamPreview] 페이드아웃 완료");
    }

    private IEnumerator LogActualResolution()
    {
        // 웹캠이 초기화될 때까지 대기
        yield return new WaitForSeconds(0.5f);

        if (_tex != null && _tex.isPlaying)
        {
            Debug.Log($"[WebcamPreview] 요청 해상도: {_requestedWidth}x{_requestedHeight}");
            Debug.Log($"[WebcamPreview] 실제 해상도: {_tex.width}x{_tex.height}");

            if (_tex.width != _requestedWidth || _tex.height != _requestedHeight)
            {
                Debug.LogWarning($"[WebcamPreview] 카메라가 요청 해상도를 지원하지 않음. 실제: {_tex.width}x{_tex.height}");
            }
            else
            {
                Debug.Log($"[WebcamPreview] 해상도 매칭 성공!");
            }
        }
    }
}
