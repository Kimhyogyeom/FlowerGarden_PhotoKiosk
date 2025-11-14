using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 웹캠 (시각화)
/// </summary>
public class WebcamPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage webcamTarget;

    [Header("Camera Selection")]
    [SerializeField] private string preferredDeviceKeyword = "C922"; // 디바이스 이름에 포함될 키워드
    [SerializeField] private int requestedWidth = 1920;   // 1080p
    [SerializeField] private int requestedHeight = 1080;  // 1080p
    [SerializeField] private int requestedFps = 30;

    private WebCamTexture _tex;

    private void Start()
    {
        // 사용 가능한 카메라 나열
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("WebCam 장치를 찾을 수 없습니다.");
            return;
        }

        // C922가 있으면 우선 선택, 없으면 첫 번째
        var dev = devices.FirstOrDefault(d => d.name.Contains(preferredDeviceKeyword));
        if (string.IsNullOrEmpty(dev.name))
            dev = devices[0];

        // 스트림 시작 (요청 해상도/프레임)
        _tex = new WebCamTexture(dev.name, requestedWidth, requestedHeight, requestedFps);
        webcamTarget.texture = _tex;
        _tex.Play();
    }

    private void Update()
    {
        if (_tex == null || !_tex.isPlaying) return;

        // 카메라의 회전/미러링 보정
        var rot = _tex.videoRotationAngle;      // 0/90/180/270
        var vert = _tex.videoVerticallyMirrored; // 일부 웹캠에서 true

        // RawImage UV 보정 (세로 뒤집힘)
        var uv = webcamTarget.uvRect;
        uv.y = vert ? 1 : 0;
        uv.height = vert ? -1 : 1;
        webcamTarget.uvRect = uv;

        // 캔버스 회전 보정
        webcamTarget.rectTransform.localEulerAngles = new Vector3(0, 0, -rot);

        // (선택) 런타임에 실제 해상도가 잡히면 비율 갱신
        // 일부 드라이버는 실제 w/h를 첫 프레임 후에 리포트함
        // if (fitter != null && _tex.width > 16) fitter.aspectRatio = (float)_tex.width / _tex.height;
    }

    private void OnDisable()
    {
        if (_tex != null)
        {
            if (_tex.isPlaying) _tex.Stop();
            Destroy(_tex);
        }
    }
}
