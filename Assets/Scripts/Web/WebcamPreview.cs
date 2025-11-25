using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 웹캠 프리뷰 컨트롤러
/// - 선택한 WebCam 장치를 RawImage 에 실시간으로 출력
/// - 카메라 패널이 꺼져있으면 업데이트 스킵
/// - 회전/뒤집기 값이 변경될 때만 UI 갱신(불필요 연산 감소)
/// </summary>
public class WebcamPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage webcamTarget;
    // 웹캠 화면을 표시할 RawImage

    [Header("Camera Selection")]
    [SerializeField] private string preferredDeviceKeyword = "C922";
    // 우선적으로 사용할 카메라 이름에 포함되었으면 하는 키워드 (예: "C922")

    [SerializeField] private int requestedWidth = 1280;  // 1920 → 1280으로 조금 낮춰서 테스트 권장
    [SerializeField] private int requestedHeight = 720;   // 1080 → 720
    [SerializeField] private int requestedFps = 30;    // 요청 프레임 수

    [Header("Setting Object")]
    [SerializeField] private GameObject _cameraPanel;
    // 카메라가 실제로 보이는 패널(ON일 때만 업데이트)

    private WebCamTexture _tex;

    // 회전/플립 값 캐싱용
    private int _lastRotation = -999;
    private bool _lastVerticallyMirrored = false;

    private void Start()
    {
        InitAndStartWebcam();
    }

    /// <summary>
    /// 웹캠 초기화 + 재생
    /// </summary>
    private void InitAndStartWebcam()
    {
        if (webcamTarget == null)
        {
            Debug.LogError("[WebcamPreview] webcamTarget이 비어있습니다.");
            return;
        }

        // 현재 연결된 웹캠 장치 목록 가져오기
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("[WebcamPreview] WebCam 장치를 찾을 수 없습니다.");
            return;
        }

        // preferredDeviceKeyword 를 포함하는 장치를 우선 선택, 없으면 첫 번째 장치 사용
        var dev = devices.FirstOrDefault(d => d.name.Contains(preferredDeviceKeyword));
        if (string.IsNullOrEmpty(dev.name))
            dev = devices[0];

        Debug.Log($"[WebcamPreview] 사용 장치: {dev.name}");

        // WebCamTexture 생성 (요청 해상도 / FPS)
        _tex = new WebCamTexture(dev.name, requestedWidth, requestedHeight, requestedFps);

        // RawImage 에 텍스처 연결 후 재생
        webcamTarget.texture = _tex;
        _tex.Play();

        // 초기 값 리셋
        _lastRotation = -999;
        _lastVerticallyMirrored = !_tex.videoVerticallyMirrored; // 강제로 처음 한 번 갱신되게
    }

    private void LateUpdate()
    {
        // 카메라 패널이 꺼져 있으면 업데이트 할 필요 없음
        if (_cameraPanel != null && !_cameraPanel.activeInHierarchy)
            return;

        if (_tex == null || !_tex.isPlaying)
            return;

        // 카메라의 회전 각도 및 상하 반전 여부 읽기
        int rot = _tex.videoRotationAngle;       // 0 / 90 / 180 / 270
        bool vert = _tex.videoVerticallyMirrored; // 상하 반전 여부 (true 이면 위아래가 뒤집혀 있음)

        // === 상하 반전 값이 바뀐 경우에만 UV 갱신 ===
        if (vert != _lastVerticallyMirrored)
        {
            var uv = webcamTarget.uvRect;
            uv.y = vert ? 1f : 0f;
            uv.height = vert ? -1f : 1f;
            webcamTarget.uvRect = uv;

            _lastVerticallyMirrored = vert;
        }

        // === 회전 값이 바뀐 경우에만 RectTransform 회전 갱신 ===
        if (rot != _lastRotation)
        {
            webcamTarget.rectTransform.localEulerAngles = new Vector3(0f, 0f, -rot);
            _lastRotation = rot;
        }

        // (선택 사항) AspectRatioFitter 등을 사용해 화면 비율을 웹캠 해상도에 맞추고 싶다면,
        // _tex.width / _tex.height 값이 유효해졌을 때 한 번만 갱신해도 됨.
    }

    /// <summary>
    /// 이 스크립트가 비활성화될 때(WebcamPreview 컴포넌트 꺼짐 / 오브젝트 비활성화 등)
    /// 웹캠 정리
    /// </summary>
    private void OnDisable()
    {
        StopAndDisposeWebcam();
    }

    private void OnApplicationQuit()
    {
        StopAndDisposeWebcam();
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
    }

    // ===== (선택) 외부에서 호출할 수 있는 함수 예시 =====

    /// <summary>
    /// 외부에서 카메라 패널이 켜졌을 때 웹캠을 다시 켜고 싶다면 호출
    /// </summary>
    public void ResumeWebcamIfNeeded()
    {
        if (_tex != null && !_tex.isPlaying)
        {
            _tex.Play();
        }
    }

    /// <summary>
    /// 외부에서 웹캠을 잠깐 멈추고 싶을 때
    /// </summary>
    public void PauseWebcam()
    {
        if (_tex != null && _tex.isPlaying)
        {
            _tex.Pause();
        }
    }
}
