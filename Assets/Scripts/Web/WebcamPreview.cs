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

    [Header("Resolution Settings")]
    [Tooltip("true면 4K 지원 시 4K, 아니면 FHD로 자동 선택")]
    [SerializeField] private bool _prefer4KIfAvailable = true;
    [SerializeField] private int _requestedWidth = 1920;   // FHD 기본값
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
            // Debug.Log("[WebcamPreview] 게임 시작 시 웹캠 미리 초기화 (렉 방지)");
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

        // 4K 자동 감지: 먼저 4K로 시도하고, 실패하면 FHD로 폴백
        int targetWidth = _requestedWidth;
        int targetHeight = _requestedHeight;
        int targetFps = _requestedFps;

        if (_prefer4KIfAvailable)
        {
            targetWidth = 3840;
            targetHeight = 2160;
            targetFps = 30; // 4K는 보통 30fps
        }

        // WebCamTexture 생성 (요청 해상도 / FPS)
        _tex = new WebCamTexture(_currentDeviceName, targetWidth, targetHeight, targetFps);

        // 텍스처 품질 설정 (화질 개선)
        _tex.filterMode = FilterMode.Bilinear;  // Bilinear 필터링 (부드럽게)
        _tex.wrapMode = TextureWrapMode.Clamp;   // 가장자리 처리

        // RawImage 에 텍스처 연결 후 재생
        _webcamTarget.texture = _tex;
        _tex.Play();

        _isWebcamInitialized = true;

        // 실제 해상도 확인 및 4K 폴백 처리
        StartCoroutine(CheckResolutionAndFallback(targetWidth, targetHeight));

        // 초기 값 리셋
        _lastRotation = -999;
        _lastVerticallyMirrored = !_tex.videoVerticallyMirrored;
        _lastHorizontalMirrored = !_mirrorHorizontal;
    }

    /// <summary>
    /// 실제 해상도 확인 후 4K 미지원 시 FHD로 폴백
    /// </summary>
    private IEnumerator CheckResolutionAndFallback(int requestedWidth, int requestedHeight)
    {
        yield return new WaitForSeconds(0.5f);

        if (_tex == null || !_tex.isPlaying)
            yield break;

        int actualWidth = _tex.width;
        int actualHeight = _tex.height;

        Debug.Log($"[WebcamPreview] 요청 해상도: {requestedWidth}x{requestedHeight}");
        Debug.Log($"[WebcamPreview] 실제 해상도: {actualWidth}x{actualHeight}");

        // 빌드 폴더에 로그 파일 생성 (덮어쓰기)
        try
        {
            string logPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "print_log.txt");
            string logContent = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] 웹캠 요청: {requestedWidth}x{requestedHeight}, 실제: {actualWidth}x{actualHeight}\n";
            System.IO.File.WriteAllText(logPath, logContent);
        }
        catch { }

        // 4K가 아니면 FHD로 재시작 (중간 해상도는 부하만 주고 화질 이점 적음)
        if (_prefer4KIfAvailable && requestedWidth == 3840)
        {
            bool is4K = actualWidth >= 3840 || actualHeight >= 2160;

            if (is4K)
            {
                Debug.Log("[WebcamPreview] ✅ 4K 카메라 감지됨!");
            }
            else
            {
                Debug.Log($"[WebcamPreview] 4K 미지원 → FHD로 재시작 (실제: {actualWidth}x{actualHeight})");

                // 웹캠 정지 후 FHD로 재시작
                if (_tex.isPlaying)
                    _tex.Stop();
                Destroy(_tex);

                _tex = new WebCamTexture(_currentDeviceName, _requestedWidth, _requestedHeight, _requestedFps);
                _tex.filterMode = FilterMode.Bilinear;
                _tex.wrapMode = TextureWrapMode.Clamp;
                _webcamTarget.texture = _tex;
                _tex.Play();

                yield return new WaitForSeconds(0.3f);
                if (_tex != null && _tex.isPlaying)
                {
                    Debug.Log($"[WebcamPreview] ✅ FHD로 안정화 완료: {_tex.width}x{_tex.height}");
                }
            }
        }
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
        // Debug.Log("[WebcamPreview] 웹캠 정지");
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
    /// 촬영 순간에 고해상도로 전환 후 캡처 (4K 지원 카메라일 경우)
    /// </summary>
    /// <param name="onHighResReady">고해상도 준비 완료 시 콜백 (Texture2D 전달)</param>
    /// <param name="targetWidth">목표 너비 (기본 3840)</param>
    /// <param name="targetHeight">목표 높이 (기본 2160)</param>
    public void CaptureHighResolution(System.Action<Texture2D> onHighResReady, int targetWidth = 3840, int targetHeight = 2160)
    {
        StartCoroutine(CaptureHighResolutionCoroutine(onHighResReady, targetWidth, targetHeight));
    }

    private IEnumerator CaptureHighResolutionCoroutine(System.Action<Texture2D> onHighResReady, int targetWidth, int targetHeight)
    {
        if (_tex == null || !_tex.isPlaying)
        {
            Debug.LogWarning("[WebcamPreview] 웹캠이 활성화되지 않음");
            onHighResReady?.Invoke(null);
            yield break;
        }

        // 현재 해상도 저장
        int originalWidth = _tex.width;
        int originalHeight = _tex.height;

        // 이미 목표 해상도 이상이면 바로 캡처
        if (originalWidth >= targetWidth || originalHeight >= targetHeight)
        {
            Debug.Log($"[WebcamPreview] 현재 해상도가 충분함: {originalWidth}x{originalHeight}");
            Texture2D captured = CaptureCurrentFrame();
            onHighResReady?.Invoke(captured);
            yield break;
        }

        Debug.Log($"[WebcamPreview] 고해상도 캡처 시도: {originalWidth}x{originalHeight} → {targetWidth}x{targetHeight}");

        // 웹캠 정지
        _tex.Stop();
        Destroy(_tex);

        // 고해상도로 재시작
        _tex = new WebCamTexture(_currentDeviceName, targetWidth, targetHeight, 30);
        _tex.filterMode = FilterMode.Bilinear;
        _tex.wrapMode = TextureWrapMode.Clamp;
        _webcamTarget.texture = _tex;
        _tex.Play();

        // 안정화 대기
        yield return new WaitForSeconds(0.5f);

        // 실제 해상도 확인
        int actualWidth = _tex.width;
        int actualHeight = _tex.height;
        Debug.Log($"[WebcamPreview] 고해상도 전환 결과: {actualWidth}x{actualHeight}");

        // 캡처
        Texture2D highResTex = CaptureCurrentFrame();

        // 원래 해상도로 복원 (4K 미지원이면 어차피 FHD로 돌아감)
        if (actualWidth < targetWidth && actualHeight < targetHeight)
        {
            // 4K 미지원 → FHD로 복원
            Debug.Log("[WebcamPreview] 4K 미지원 → FHD로 복원");
            _tex.Stop();
            Destroy(_tex);

            _tex = new WebCamTexture(_currentDeviceName, _requestedWidth, _requestedHeight, _requestedFps);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
            _webcamTarget.texture = _tex;
            _tex.Play();
        }

        onHighResReady?.Invoke(highResTex);
    }

    /// <summary>
    /// 현재 웹캠 프레임을 Texture2D로 캡처
    /// </summary>
    public Texture2D CaptureCurrentFrame()
    {
        if (_tex == null || !_tex.isPlaying)
        {
            Debug.LogWarning("[WebcamPreview] 웹캠이 활성화되지 않음");
            return null;
        }

        Texture2D captured = new Texture2D(_tex.width, _tex.height, TextureFormat.RGB24, false);
        captured.SetPixels(_tex.GetPixels());
        captured.Apply();

        Debug.Log($"[WebcamPreview] 프레임 캡처 완료: {captured.width}x{captured.height}");
        return captured;
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
        // Debug.Log("[WebcamPreview] 페이드인 완료");
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
        // Debug.Log("[WebcamPreview] 페이드아웃 완료");
    }
}
