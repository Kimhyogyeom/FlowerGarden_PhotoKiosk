using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

/// <summary>
/// 웹캠에서 사람만 분리하고 배경을 3장의 이미지로 교체
/// </summary>
public class VirtualBackgroundController : MonoBehaviour
{
    [Header("Webcam Reference")]
    [SerializeField] private WebcamPreview _webcamPreview;

    [Header("Segmentation Model")]
    [SerializeField] private NNModel _modelAsset;

    [Header("Backgrounds")]
    [SerializeField] private Texture2D[] _backgroundTextures;
    private int _currentBackgroundIndex = 0;

    [Header("Output")]
    [SerializeField] private RawImage _outputImage;

    [Header("Performance Settings")]
    [SerializeField] private int _processingWidth = 256;
    [SerializeField] private int _processingHeight = 144;
    [SerializeField] private int _processEveryNFrames = 2;

    [Header("Quality Settings")]
    [SerializeField] private float _maskThreshold = 0.6f;
    [SerializeField] private float _edgeSmoothness = 0.05f;
    [SerializeField] private float _temporalStability = 0.7f;
    [SerializeField] private float _dilateAmount = 0.1f;
    [SerializeField] private float _fillHolesAmount = 0.9f;

    [Header("Mirror Settings")]
    [SerializeField] private bool _mirrorHorizontal = true;

    [Header("Mirror Settings")]

    // 내부 변수
    private IWorker _worker;
    private Material _compositeMaterial;
    private RenderTexture _maskTexture;
    private RenderTexture _previousMaskTexture;
    private RenderTexture _outputTexture;
    private int _frameCounter = 0;
    private bool _isFirstFrame = true;

    void Start()
    {
        InitializeModel();
        CreateRenderTextures();
        CreateCompositeMaterial();
    }

    private void InitializeModel()
    {
        // 방법 1: Inspector에서 연결된 경우
        if (_modelAsset != null)
        {
            var _model = ModelLoader.Load(_modelAsset);
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, _model);
            Debug.Log("[VirtualBackground] 세그멘테이션 모델 로드 완료 (Inspector)");
            return;
        }

        // 방법 2: Resources에서 직접 로드 (빌드 대비)
        NNModel runtimeModel = Resources.Load<NNModel>("Models/selfie_segmentation_landscape");
        if (runtimeModel == null)
        {
            Debug.LogError("[VirtualBackground] Resources/Models/selfie_segmentation_landscape 모델을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        var model = ModelLoader.Load(runtimeModel);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, model);
        Debug.Log("[VirtualBackground] 세그멘테이션 모델 로드 완료 (Resources)");
    }

    private void CreateRenderTextures()
    {
        _maskTexture = new RenderTexture(_processingWidth, _processingHeight, 0, RenderTextureFormat.RFloat);
        _previousMaskTexture = new RenderTexture(_processingWidth, _processingHeight, 0, RenderTextureFormat.RFloat); // ← 추가!
        _outputTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);

        if (_outputImage != null)
        {
            _outputImage.texture = _outputTexture;
        }
    }

    private void CreateCompositeMaterial()
    {
        Shader shader = Shader.Find("Custom/PersonBackgroundComposite");
        if (shader == null)
        {
            Debug.LogError("[VirtualBackground] 'Custom/PersonBackgroundComposite' 쉐이더를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        _compositeMaterial = new Material(shader);
    }

    void Update()
    {
        if (_worker == null || _compositeMaterial == null) return;

        // 프레임 스킵
        _frameCounter++;
        if (_frameCounter % _processEveryNFrames != 0) return;

        // 웹캠 텍스처 가져오기
        WebCamTexture webcamTex = GetWebcamTexture();
        if (webcamTex == null || !webcamTex.isPlaying) return;

        // 1. 사람 세그멘테이션 실행
        ProcessSegmentation(webcamTex);

        // 2. 배경과 합성
        CompositeWithBackground(webcamTex);
    }

    private WebCamTexture GetWebcamTexture()
    {
        if (_webcamPreview == null) return null;
        return _webcamPreview.GetTexture();
    }

    private void ProcessSegmentation(WebCamTexture input)
    {
        using (var tensor = TextureToTensor(input))
        {
            _worker.Execute(tensor);
            var output = _worker.PeekOutput();

            // 임시 텍스처에 현재 마스크 저장
            RenderTexture tempMask = RenderTexture.GetTemporary(
                _processingWidth, _processingHeight, 0, RenderTextureFormat.RFloat);
            TensorToRenderTexture(output, tempMask);

            // 시간적 안정화 적용
            if (!_isFirstFrame && _temporalStability > 0.01f)
            {
                ApplyTemporalSmoothing(tempMask, _maskTexture);
            }
            else
            {
                Graphics.Blit(tempMask, _maskTexture);
            }

            // 다음 프레임을 위해 저장
            Graphics.Blit(_maskTexture, _previousMaskTexture);

            RenderTexture.ReleaseTemporary(tempMask);
            _isFirstFrame = false;
        }
    }

    private void ApplyTemporalSmoothing(RenderTexture current, RenderTexture output)
    {
        // 이전 프레임과 현재 프레임을 가중 평균
        // 예: 이전 * 0.7 + 현재 * 0.3

        Material smoothMat = new Material(Shader.Find("Hidden/TemporalSmooth"));
        if (smoothMat.shader == null)
        {
            // 폴백: 간단한 블렌딩
            Graphics.Blit(current, output);
            return;
        }

        smoothMat.SetTexture("_PrevTex", _previousMaskTexture);
        smoothMat.SetFloat("_Stability", _temporalStability);
        Graphics.Blit(current, output, smoothMat);

        Destroy(smoothMat);
    }

    private Tensor TextureToTensor(Texture input)
    {
        RenderTexture temp = RenderTexture.GetTemporary(
            _processingWidth,
            _processingHeight,
            0,
            RenderTextureFormat.ARGB32
        );
        Graphics.Blit(input, temp);

        var tensor = new Tensor(temp, 3);
        RenderTexture.ReleaseTemporary(temp);

        return tensor;
    }

    private void TensorToRenderTexture(Tensor tensor, RenderTexture target)
    {
        tensor.ToRenderTexture(target);
    }

    private void CompositeWithBackground(WebCamTexture webcam)
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0)
        {
            Debug.LogWarning("[VirtualBackground] 배경 이미지가 없습니다!");
            return;
        }

        _compositeMaterial.SetTexture("_MainTex", webcam);
        _compositeMaterial.SetTexture("_MaskTex", _maskTexture);
        _compositeMaterial.SetTexture("_BackgroundTex", _backgroundTextures[_currentBackgroundIndex]);
        _compositeMaterial.SetFloat("_Threshold", _maskThreshold);
        _compositeMaterial.SetFloat("_Smoothness", _edgeSmoothness);
        _compositeMaterial.SetFloat("_Dilate", _dilateAmount);
        _compositeMaterial.SetFloat("_FillHoles", _fillHolesAmount);
        _compositeMaterial.SetFloat("_MirrorHorizontal", _mirrorHorizontal ? 1f : 0f);  // ← 추가!

        Graphics.Blit(webcam, _outputTexture, _compositeMaterial);
    }

    public void NextBackground()
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;

        _currentBackgroundIndex = (_currentBackgroundIndex + 1) % _backgroundTextures.Length;
        Debug.Log($"[VirtualBackground] 배경 전환: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

    public void PreviousBackground()
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;

        _currentBackgroundIndex--;
        if (_currentBackgroundIndex < 0)
            _currentBackgroundIndex = _backgroundTextures.Length - 1;

        Debug.Log($"[VirtualBackground] 배경 전환: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

    /// <summary>
    /// 특정 번호의 배경으로 직접 변경 (0부터 시작)
    /// </summary>
    public void SetBackground(int index)
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0)
        {
            Debug.LogWarning("[VirtualBackground] 배경 이미지가 없습니다!");
            return;
        }

        if (index < 0 || index >= _backgroundTextures.Length)
        {
            Debug.LogError($"[VirtualBackground] 유효하지 않은 인덱스: {index} (0~{_backgroundTextures.Length - 1} 범위)");
            return;
        }

        _currentBackgroundIndex = index;
        Debug.Log($"[VirtualBackground] 배경 설정: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

    /// <summary>
    /// 현재 배경 인덱스 가져오기
    /// </summary>
    public int GetCurrentBackgroundIndex()
    {
        return _currentBackgroundIndex;
    }

    /// <summary>
    /// 전체 배경 개수 가져오기
    /// </summary>
    public int GetBackgroundCount()
    {
        return _backgroundTextures?.Length ?? 0;
    }

    void OnDestroy()
    {
        _worker?.Dispose();

        if (_maskTexture != null) _maskTexture.Release();
        if (_previousMaskTexture != null) _previousMaskTexture.Release(); // ← 추가!
        if (_outputTexture != null) _outputTexture.Release();
        if (_compositeMaterial != null) Destroy(_compositeMaterial);
    }
}
