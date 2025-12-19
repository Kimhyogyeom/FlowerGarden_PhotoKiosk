# HW_PhotKiosks 프로젝트 가이드

## 프로젝트 개요
Unity 기반 포토 키오스크 애플리케이션
- 사진 촬영, 프레임 선택, 결제, 인쇄 기능
- 세로 모드(Height) / 가로 모드(Width) 지원

## 코딩 컨벤션

### 네이밍 규칙

#### 필드 (Fields)
```csharp
// private 필드: 언더스코어(_) 접두사 + camelCase
[SerializeField] private GameObject _currentPanel;
[SerializeField] private Button _photoButton;
private bool _isProcessing;
private float _timeElapsed;

// public 필드: 언더스코어(_) 접두사 + camelCase (Inspector 노출용)
public int _printCount = 2;
public SoundDatabase _soundDatabase;
```

#### 프로퍼티 (Properties)
```csharp
// PascalCase, 읽기 전용은 => 사용
public static GameManager Instance { get; private set; }
public KioskState CurrentState => _currentState;
public KioskMode CurrentMode => _currentMode;
```

#### 메서드 (Methods)
```csharp
// PascalCase
public void PlayBGM(AudioClip clip, float volume = 1f)
private void Awake()
private IEnumerator CaptureAndPrintRoutine()
```

#### 지역 변수 (Local Variables)
```csharp
// camelCase (언더스코어 없음)
int targetWidth = 1800;
string savePath = Path.Combine(folderPath, filename);
bool isLandscapeMode = false;
```

#### 상수 및 Enum
```csharp
// enum: PascalCase
public enum KioskState { Ready, Mode, Select, Filming, Printing }
public enum ResampleMode { None, Cover, Fit, ExactCrop, Stretch, ScaleUp }

// const: PascalCase 또는 UPPER_SNAKE_CASE
private const float KEYEVENTF_KEYUP = 0x0002;
private const byte VK_RETURN = 0x0D;
```

### SerializeField 어트리뷰트

```csharp
// Header로 그룹화
[Header("Setting Component")]
[SerializeField] private StepCountdownUI _stepCountdownUI;

// Tooltip으로 설명 추가
[Header("Print Settings")]
[Tooltip("같은 이미지를 몇 번 출력할지 (기본 2장)")]
[SerializeField, Min(1)] public int _printCount = 2;

// Range로 범위 제한
[SerializeField, Range(0f, 0.15f)] private float _scaleUpPortraitY = 0.03f;
```

### 클래스 구조

```csharp
public class ExampleCtrl : MonoBehaviour
{
    // 1. Header별로 SerializeField 그룹화
    [Header("Component Settings")]
    [SerializeField] private SomeComponent _component;

    [Header("UI References")]
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _panel;

    [Header("Settings")]
    [SerializeField] private float _duration = 1f;

    // 2. private 필드
    private bool _isActive;

    // 3. 프로퍼티
    public bool IsActive => _isActive;

    // 4. Unity 라이프사이클 (Awake, Start, Update 순서)
    private void Awake() { }
    private void Start() { }
    private void Update() { }

    // 5. public 메서드
    public void DoSomething() { }

    // 6. private 메서드
    private void InternalMethod() { }

    // 7. 코루틴
    private IEnumerator SomeRoutine() { }
}
```

### 싱글톤 패턴

```csharp
public class SomeManager : MonoBehaviour
{
    public static SomeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
```

### 주석 스타일

```csharp
// 한 줄 주석: 간단한 설명
private float _timeout = 5f; // 타임아웃 시간(초)

/// <summary>
/// XML 주석: public 메서드나 중요한 로직에 사용
/// </summary>
public void ImportantMethod() { }

// Header 주석으로 섹션 구분
// ===== Main Routine =====
// ===== Util / Helper =====
```

### 디버그 로그

```csharp
// 태그 형식 사용
Debug.Log("[Print] 저장 완료: " + path);
Debug.LogWarning("[Print] 파일을 찾지 못함");
Debug.LogError("[Print] 캡처 실패");
```

## 파일 구조

```
Assets/Scripts/
├── Manager/          # 전역 매니저 (GameManager, SoundManager)
├── Print/            # 인쇄 관련
├── Payment/          # 결제 관련
├── WindowXxx/        # 각 화면별 컨트롤러
│   ├── WindowFilming/
│   ├── WindowSelect/
│   └── ...
├── Helper/           # 유틸리티, 헬퍼 클래스
├── QR/               # QR 코드 생성/업로드
└── Web/              # 웹캠, 네트워크 관련
```

## 주요 클래스

- `GameManager`: 키오스크 상태 관리 (싱글톤)
- `PrintController`: 사진 캡처, 변환, 인쇄
- `SoundManager`: BGM/SFX 재생 (싱글톤)
- `PaymentCtrl`: 결제 처리

## 키오스크 상태 흐름

```
Ready → Mode → Chroma → Select → Quantity → Payment → WaitingForPayment → Filming → CutWindow → Printing
```

## 빌드/실행

- Unity 버전: (프로젝트 설정 확인 필요)
- 타겟 플랫폼: Windows Standalone
- 프린터: DS-RX1 (PhotoPrinterBridge.exe 사용)
