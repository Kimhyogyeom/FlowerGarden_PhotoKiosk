using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크로마키 카메라 배경 이미지를 선택하는 컨트롤러
/// - mode = 0 : A 이미지
/// - mode = 1 : B 이미지
/// - mode = 2 : C 이미지
///
/// ⚠ 크로마키 쉐이더에
///   - 배경용 텍스처 프로퍼티 (예: _BackgroundTex)
///   - 옵션: 배경 사용 여부 플로트 (예: _UseBackground)
/// 가 있다고 가정.
/// </summary>
public class ChromaKeyBackgroundSelector : MonoBehaviour
{
    [Header("웹캠이 출력되는 RawImage")]
    [SerializeField] private RawImage _webcamTarget;

    [Header("배경 이미지들 (크로마키 뒤에 깔릴 Texture)")]
    [SerializeField] private Texture _backgroundA;   // mode = 0
    [SerializeField] private Texture _backgroundB;   // mode = 1
    [SerializeField] private Texture _backgroundC;   // mode = 2

    [Header("현재 모드 (0=A, 1=B, 2=C)")]
    [Range(0, 2)]
    [SerializeField] private int _mode = 0;

    [Header("쉐이더 프로퍼티 이름")]
    [SerializeField] private string _backgroundTexPropertyName = "_BackgroundTex";
    // 크로마키 쉐이더에서 배경 텍스처 프로퍼티 이름

    [SerializeField] private string _useBackgroundPropertyName = "_UseBackground";
    // 선택사항: 배경 사용 여부(0 또는 1) 플로트 프로퍼티 이름

    // 런타임용 머티리얼 인스턴스 (공유 머티리얼 건드리지 않기 위함)
    private Material _runtimeMaterial;

    private void Awake()
    {
        if (_webcamTarget == null)
        {
            Debug.LogError("[ChromaKeyBackgroundSelector] _webcamTarget 이 비어 있습니다.");
            return;
        }

        if (_webcamTarget.material == null)
        {
            Debug.LogError("[ChromaKeyBackgroundSelector] _webcamTarget 에 머티리얼이 없습니다. 크로마키 쉐이더 머티리얼을 할당해 주세요.");
            return;
        }

        // 공유 머티리얼 복사해서 사용
        _runtimeMaterial = Instantiate(_webcamTarget.material);
        _webcamTarget.material = _runtimeMaterial;
    }

    private void Start()
    {
        ApplyMode();
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 바꾸면 에디터에서도 바로 반영
    private void OnValidate()
    {
        if (_webcamTarget == null) return;

        // 플레이 중이면 런타임 머티리얼, 아니면 그냥 현재 머티리얼
        var mat = Application.isPlaying ? _runtimeMaterial : _webcamTarget.material;
        if (mat == null) return;

        ApplyMode();
    }
#endif

    /// <summary>
    /// 외부에서 모드를 바꾸고 싶을 때 호출
    /// 예) 다른 스크립트: selector.SetMode(1);
    /// </summary>
    public void SetMode(int mode)
    {
        _mode = Mathf.Clamp(mode, 0, 2);
        ApplyMode();
    }

    /// <summary>
    /// 현재 _mode 값에 따라 머티리얼에 배경 텍스처 적용
    /// </summary>
    private void ApplyMode()
    {
        if (_webcamTarget == null) return;

        var mat = Application.isPlaying ? _runtimeMaterial : _webcamTarget.material;
        if (mat == null) return;

        Texture selectedBackground = null;

        switch (_mode)
        {
            case 0:
                selectedBackground = _backgroundA;
                break;
            case 1:
                selectedBackground = _backgroundB;
                break;
            case 2:
                selectedBackground = _backgroundC;
                break;
        }

        if (!string.IsNullOrEmpty(_backgroundTexPropertyName))
        {
            mat.SetTexture(_backgroundTexPropertyName, selectedBackground);
        }

        if (!string.IsNullOrEmpty(_useBackgroundPropertyName))
        {
            // 배경 텍스처가 있으면 1, 없으면 0
            mat.SetFloat(_useBackgroundPropertyName, selectedBackground != null ? 1f : 0f);
        }

        Debug.Log($"[ChromaKeyBackgroundSelector] mode={_mode}, tex={(selectedBackground ? selectedBackground.name : "null")}");
    }
}
