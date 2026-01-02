using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;
using System.IO;
using System;

/// <summary>
/// 웹캠에서 사람만 분리하고 배경을 3장의 이미지로 교체
/// MediaPipe Selfie Segmentation 기반
/// </summary>
public class VirtualBackgroundController : MonoBehaviour
{
    [Header("Webcam Reference")]
    [SerializeField] private WebcamPreview _webcamPreview;

    [Header("Segmentation Model")]
    [SerializeField] private NNModel _modelAsset;
    [Tooltip("GPU 문제 시 CSharpBurst로 변경")]
    [SerializeField] private WorkerFactory.Type _workerType = WorkerFactory.Type.Auto;

    [Header("Backgrounds")]
    [SerializeField] private Texture2D[] _backgroundTextures;
    private int _currentBackgroundIndex = 0;

    [Header("임시 비활성화 (true면 index 0은 원본 웹캠 출력)")]
    [SerializeField] private bool _disableBackgroundA = false;

    [Header("Output")]
    [SerializeField] private RawImage _outputImage;

    [Header("Performance Settings")]
    [Tooltip("모델 입력 해상도 (256x144 landscape 모델 권장)")]
    [SerializeField] private int _modelInputWidth = 256;
    [SerializeField] private int _modelInputHeight = 144;
    [Tooltip("마스크 업스케일 해상도 (높을수록 경계 부드러움)")]
    [SerializeField] private int _maskUpscaleWidth = 1920;
    [SerializeField] private int _maskUpscaleHeight = 1080;
    [SerializeField] private int _processEveryNFrames = 1;

    [Header("Quality Settings")]
    [SerializeField] private float _maskThreshold = 0.5f;
    [SerializeField] private float _edgeSmoothness = 0.15f;
    [SerializeField] private float _temporalStability = 0.3f;
    [SerializeField] private float _dilateAmount = 0.08f;
    [SerializeField] private float _fillHolesAmount = 0.80f;
    [SerializeField, Range(0f, 0.05f)] private float _edgeInset = 0.015f;

    [Header("Mirror Settings")]
    [SerializeField] private bool _mirrorHorizontal = true;

    [Header("Mask Settings")]
    [Tooltip("마스크가 반전되어 나오는 경우 체크 (일부 GPU/환경에서 필요)")]
    [SerializeField] private bool _invertMask = false;

    [Header("Output Resolution")]
    [SerializeField] private int _outputResWidth = 1920;
    [SerializeField] private int _outputResHeight = 1080;

    // 내부 변수
    private IWorker _worker;
    private Material _compositeMaterial;
    private Material _maskBlurMaterial;
    private RenderTexture _rawMaskTexture;      // 모델 출력 (저해상도)
    private RenderTexture _maskTexture;         // 업스케일 + 블러된 마스크
    private RenderTexture _previousMaskTexture;
    private RenderTexture _outputTexture;
    private int _frameCounter = 0;
    private bool _isFirstFrame = true;
    private bool _autoInvertDetected = false;   // 자동 반전 감지
    private int _autoInvertCheckFrames = 0;     // 자동 감지용 프레임 카운터

    void Start()
    {
        // 설정값 강제 고정 (씬 저장값 무시)
        _modelInputWidth = 256;
        _modelInputHeight = 144;
        _maskUpscaleWidth = 960;  // 1920은 너무 무거움
        _maskUpscaleHeight = 540;

        InitializeModel();
        CreateRenderTextures();
        CreateCompositeMaterial();
        ValidateBackgroundTextures();
    }

    private void ValidateBackgroundTextures()
    {
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VirtualBackground_Log.txt");
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"=== VirtualBackground 디버그 로그 ===");
        sb.AppendLine($"시간: {DateTime.Now}");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"GPU 벤더: {SystemInfo.graphicsDeviceVendor}");
        sb.AppendLine($"GPU 타입: {SystemInfo.graphicsDeviceType}");
        sb.AppendLine($"Unity 버전: {Application.unityVersion}");
        sb.AppendLine();

        // 모델 상태
        sb.AppendLine($"[Model] Worker: {(_worker != null ? "OK" : "NULL")}");
        sb.AppendLine($"[Model] ModelAsset: {(_modelAsset != null ? _modelAsset.name : "NULL")}");
        sb.AppendLine();

        // 배경 텍스처 검증
        if (_backgroundTextures == null || _backgroundTextures.Length == 0)
        {
            sb.AppendLine("[ERROR] 배경 텍스처가 할당되지 않았습니다!");
        }
        else
        {
            sb.AppendLine($"[Background] 배경 텍스처 개수: {_backgroundTextures.Length}");
            for (int i = 0; i < _backgroundTextures.Length; i++)
            {
                if (_backgroundTextures[i] == null)
                {
                    sb.AppendLine($"[ERROR] 배경 텍스처[{i}]: NULL");
                }
                else
                {
                    sb.AppendLine($"[OK] 배경 텍스처[{i}]: {_backgroundTextures[i].name}, " +
                                  $"{_backgroundTextures[i].width}x{_backgroundTextures[i].height}, " +
                                  $"format={_backgroundTextures[i].format}");
                }
            }
        }
        sb.AppendLine();

        // 마스크 텍스처 상태
        sb.AppendLine($"[Mask] RawMask: {(_rawMaskTexture != null ? $"{_rawMaskTexture.width}x{_rawMaskTexture.height}" : "NULL")}");
        sb.AppendLine($"[Mask] MaskTexture: {(_maskTexture != null ? $"{_maskTexture.width}x{_maskTexture.height}" : "NULL")}");
        sb.AppendLine($"[Mask] OutputTexture: {(_outputTexture != null ? $"{_outputTexture.width}x{_outputTexture.height}" : "NULL")}");
        sb.AppendLine();

        // 설정값
        sb.AppendLine($"[Settings] Threshold: {_maskThreshold}");
        sb.AppendLine($"[Settings] Smoothness: {_edgeSmoothness}");
        sb.AppendLine($"[Settings] Dilate: {_dilateAmount}");
        sb.AppendLine($"[Settings] FillHoles: {_fillHolesAmount}");
        sb.AppendLine($"[Settings] InvertMask: {_invertMask}");

        // 파일 저장
        try
        {
            File.WriteAllText(logPath, sb.ToString());
            // Debug.Log($"[VirtualBackground] 로그 저장됨: {logPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VirtualBackground] 로그 저장 실패: {e.Message}");
        }
    }

    private void InitializeModel()
    {
        Model model = null;

        // Inspector에서 연결된 경우
        if (_modelAsset != null)
        {
            model = ModelLoader.Load(_modelAsset);
            // Debug.Log("[VirtualBackground] 모델 로드됨 (Inspector)");
        }
        else
        {
            // Resources에서 직접 로드
            NNModel runtimeModel = Resources.Load<NNModel>("selfie_segmentation_landscape");
            if (runtimeModel == null)
            {
                Debug.LogError("[VirtualBackground] Resources/selfie_segmentation_landscape 모델을 찾을 수 없습니다!");
                enabled = false;
                return;
            }
            model = ModelLoader.Load(runtimeModel);
            // Debug.Log("[VirtualBackground] 모델 로드됨 (Resources)");
        }

        // Worker 생성 (GPU Compute 우선)
        var workerType = _workerType;
        if (workerType == WorkerFactory.Type.Auto)
        {
            workerType = WorkerFactory.Type.ComputePrecompiled;
        }

        try
        {
            _worker = WorkerFactory.CreateWorker(workerType, model);
            // Debug.Log($"[VirtualBackground] Worker 생성 완료: {workerType}");
            AppendToLog($"[Worker] 타입: {workerType}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VirtualBackground] {workerType} 실패, CSharpBurst로 폴백: {e.Message}");
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
            AppendToLog($"[Worker] 폴백: CSharpBurst");
        }
    }

    private void CreateRenderTextures()
    {
        // 모델 출력용 (저해상도)
        _rawMaskTexture = new RenderTexture(_modelInputWidth, _modelInputHeight, 0, RenderTextureFormat.RFloat);

        // 업스케일된 마스크 (고해상도)
        _maskTexture = new RenderTexture(_maskUpscaleWidth, _maskUpscaleHeight, 0, RenderTextureFormat.RFloat);
        _maskTexture.filterMode = FilterMode.Bilinear;  // 부드러운 업스케일

        _previousMaskTexture = new RenderTexture(_maskUpscaleWidth, _maskUpscaleHeight, 0, RenderTextureFormat.RFloat);
        _previousMaskTexture.filterMode = FilterMode.Bilinear;

        _outputTexture = new RenderTexture(_outputResWidth, _outputResHeight, 0, RenderTextureFormat.ARGB32);

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

        _frameCounter++;
        if (_frameCounter % _processEveryNFrames != 0) return;

        WebCamTexture webcamTex = GetWebcamTexture();
        if (webcamTex == null || !webcamTex.isPlaying) return;

        ProcessSegmentation(webcamTex);
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

            // 모델 출력을 저해상도 텍스처에 저장
            TensorToRenderTexture(output, _rawMaskTexture);

            // 자동 마스크 반전 감지 (처음 30프레임 동안)
            if (!_autoInvertDetected && _autoInvertCheckFrames < 30)
            {
                _autoInvertCheckFrames++;
                float avgMask = CalculateMaskAverage(output);

                // 마스크 평균이 0.6 이상이면 반전 필요 (사람 없이도 대부분이 마스크됨)
                if (avgMask > 0.6f)
                {
                    _invertMask = true;
                    _autoInvertDetected = true;
                    // Debug.Log($"[VirtualBackground] 마스크 자동 반전 활성화 (avgMask={avgMask:F2})");
                    AppendToLog($"[AutoDetect] 마스크 자동 반전 활성화 (avgMask={avgMask:F2})");
                }
                else if (_autoInvertCheckFrames >= 30)
                {
                    _autoInvertDetected = true;
                    // Debug.Log($"[VirtualBackground] 마스크 정상 (avgMask={avgMask:F2})");
                }
            }

            // 저해상도 마스크를 고해상도로 업스케일 (Bilinear 필터링)
            RenderTexture upscaledMask = RenderTexture.GetTemporary(
                _maskUpscaleWidth, _maskUpscaleHeight, 0, RenderTextureFormat.RFloat);
            upscaledMask.filterMode = FilterMode.Bilinear;
            Graphics.Blit(_rawMaskTexture, upscaledMask);

            // 시간적 안정화 적용
            if (!_isFirstFrame && _temporalStability > 0.01f)
            {
                ApplyTemporalSmoothing(upscaledMask, _maskTexture);
            }
            else
            {
                Graphics.Blit(upscaledMask, _maskTexture);
            }

            Graphics.Blit(_maskTexture, _previousMaskTexture);
            RenderTexture.ReleaseTemporary(upscaledMask);
            _isFirstFrame = false;
        }
    }

    private float CalculateMaskAverage(Tensor tensor)
    {
        // 텐서에서 일부 샘플링하여 평균 계산
        float sum = 0f;
        int sampleCount = 0;
        int width = tensor.shape[3];
        int height = tensor.shape[2];

        // 10x10 그리드로 샘플링
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                int px = (x * width) / 10;
                int py = (y * height) / 10;
                sum += tensor[0, 0, py, px];
                sampleCount++;
            }
        }

        return sum / sampleCount;
    }

    private void AppendToLog(string message)
    {
        try
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VirtualBackground_Log.txt");
            File.AppendAllText(logPath, $"\n{DateTime.Now}: {message}");
        }
        catch { }
    }

    private void ApplyTemporalSmoothing(RenderTexture current, RenderTexture output)
    {
        Material smoothMat = new Material(Shader.Find("Hidden/TemporalSmooth"));
        if (smoothMat.shader == null)
        {
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
        // 모델 입력 해상도로 리사이즈 (256x144 권장)
        RenderTexture temp = RenderTexture.GetTemporary(
            _modelInputWidth, _modelInputHeight, 0, RenderTextureFormat.ARGB32);
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

        // index 0 && 비활성화 플래그 ON이면 원본 웹캠만 출력 (좌우반전만 적용)
        if (_disableBackgroundA && _currentBackgroundIndex == 0)
        {
            if (_mirrorHorizontal)
            {
                // 좌우반전 적용을 위해 scale -1
                Graphics.Blit(webcam, _outputTexture, new Vector2(-1f, 1f), new Vector2(1f, 0f));
            }
            else
            {
                Graphics.Blit(webcam, _outputTexture);
            }
            return;
        }

        _compositeMaterial.SetTexture("_MainTex", webcam);
        _compositeMaterial.SetTexture("_MaskTex", _maskTexture);
        _compositeMaterial.SetTexture("_BackgroundTex", _backgroundTextures[_currentBackgroundIndex]);
        _compositeMaterial.SetFloat("_Threshold", _maskThreshold);
        _compositeMaterial.SetFloat("_Smoothness", _edgeSmoothness);
        _compositeMaterial.SetFloat("_Dilate", _dilateAmount);
        _compositeMaterial.SetFloat("_FillHoles", _fillHolesAmount);
        _compositeMaterial.SetFloat("_EdgeInset", _edgeInset);
        _compositeMaterial.SetFloat("_MirrorHorizontal", _mirrorHorizontal ? 1f : 0f);
        _compositeMaterial.SetFloat("_InvertMask", _invertMask ? 1f : 0f);
        _compositeMaterial.SetFloat("_UseBackground", 1f);

        Graphics.Blit(webcam, _outputTexture, _compositeMaterial);
    }

    public void NextBackground()
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;
        _currentBackgroundIndex = (_currentBackgroundIndex + 1) % _backgroundTextures.Length;
        // Debug.Log($"[VirtualBackground] 배경 전환: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

    public void PreviousBackground()
    {
        if (_backgroundTextures == null || _backgroundTextures.Length == 0) return;
        _currentBackgroundIndex--;
        if (_currentBackgroundIndex < 0)
            _currentBackgroundIndex = _backgroundTextures.Length - 1;
        // Debug.Log($"[VirtualBackground] 배경 전환: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

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
        // Debug.Log($"[VirtualBackground] 배경 설정: {_currentBackgroundIndex + 1}/{_backgroundTextures.Length}");
    }

    public int GetCurrentBackgroundIndex() => _currentBackgroundIndex;
    public int GetBackgroundCount() => _backgroundTextures?.Length ?? 0;

    void OnDestroy()
    {
        _worker?.Dispose();

        if (_rawMaskTexture != null) _rawMaskTexture.Release();
        if (_maskTexture != null) _maskTexture.Release();
        if (_previousMaskTexture != null) _previousMaskTexture.Release();
        if (_outputTexture != null) _outputTexture.Release();
        if (_compositeMaterial != null) Destroy(_compositeMaterial);
        if (_maskBlurMaterial != null) Destroy(_maskBlurMaterial);
    }
}
