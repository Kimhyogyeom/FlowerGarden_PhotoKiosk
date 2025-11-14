using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 웹캠 프리뷰 컨트롤러
/// - 선택한 WebCam 장치를 RawImage 에 실시간으로 출력
/// </summary>
public class WebcamPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage webcamTarget;
    // 웹캠 화면을 표시할 RawImage

    [Header("Camera Selection")]
    [SerializeField] private string preferredDeviceKeyword = "C922";
    // 우선적으로 사용할 카메라 이름에 포함되었으면 하는 키워드 (예: "C922")

    [SerializeField] private int requestedWidth = 1920;   // 요청 해상도 가로 (예: 1920 - 1080p)
    [SerializeField] private int requestedHeight = 1080;  // 요청 해상도 세로 (예: 1080 - 1080p)
    [SerializeField] private int requestedFps = 30;       // 요청 프레임 수

    private WebCamTexture _tex;

    private void Start()
    {
        // 현재 연결된 웹캠 장치 목록 가져오기
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("WebCam 장치를 찾을 수 없습니다.");
            return;
        }

        // preferredDeviceKeyword 를 포함하는 장치를 우선 선택, 없으면 첫 번째 장치 사용
        var dev = devices.FirstOrDefault(d => d.name.Contains(preferredDeviceKeyword));
        if (string.IsNullOrEmpty(dev.name))
            dev = devices[0];

        // WebCamTexture 생성 (요청 해상도 / FPS)
        _tex = new WebCamTexture(dev.name, requestedWidth, requestedHeight, requestedFps);

        // RawImage 에 텍스처 연결 후 재생
        webcamTarget.texture = _tex;
        _tex.Play();
    }

    private void Update()
    {
        if (_tex == null || !_tex.isPlaying) return;

        // 카메라의 회전 각도 및 상하 반전 여부 읽기
        var rot = _tex.videoRotationAngle;       // 0 / 90 / 180 / 270
        var vert = _tex.videoVerticallyMirrored; // 상하 반전 여부 (true 이면 위아래가 뒤집혀 있음)

        // RawImage UV 보정 (상하 반전 처리)
        var uv = webcamTarget.uvRect;
        uv.y = vert ? 1 : 0;
        uv.height = vert ? -1 : 1;
        webcamTarget.uvRect = uv;

        // RawImage(캔버스) 회전 적용
        webcamTarget.rectTransform.localEulerAngles = new Vector3(0, 0, -rot);

        // (선택 사항) AspectRatioFitter 등을 사용해 화면 비율을 웹캠 해상도에 맞추고 싶다면 아래 로직 사용
        // 텍스처 width/height 가 유효해졌을 때 한 번만 비율을 갱신해주면 된다.
        // if (fitter != null && _tex.width > 16) fitter.aspectRatio = (float)_tex.width / _tex.height;
    }

    private void OnDisable()
    {
        // 비활성화 시 WebCamTexture 정리
        if (_tex != null)
        {
            if (_tex.isPlaying) _tex.Stop();
            Destroy(_tex);
        }
    }
}
