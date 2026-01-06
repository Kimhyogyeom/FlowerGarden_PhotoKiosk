# HW_PhotKiosks

Unity 기반 포토 키오스크 애플리케이션

## 주요 기능

### 핵심 특징

- 사진 촬영, 프레임 선택, 결제, 인쇄 기능을 갖춘 포토 키오스크 시스템
- 세로/가로 모드 지원 (Height 4:6 / Width 6:4)
- 4컷 연속 촬영 (5초 카운트다운 + 촬영)
- 가상 배경 적용 (MediaPipe 기반 실시간 합성)
- 프레임 선택 (3종 프레임 제공)
- 고품질 인쇄 (3200x4800 해상도)
- QR 코드 생성 및 업로드 (Naver Cloud Platform)
- 페이드 전환 애니메이션

### 기술 스택

- **Unity**: 게임 엔진
- **C#**: 주 프로그래밍 언어
- **Unity Barracuda**: MediaPipe 모델 실행
- **ZXing.Net**: QR 코드 생성
- **TextMeshPro**: UI 텍스트 렌더링
- **PhotoPrinterBridge.exe**: DS-RX1 프린터 제어 (외부)

---

## 게임 플로우

```
WaitingForPayment (결제 대기)
    ↓ (결제 완료)
Ready (홈 화면)
    ↓ (모드 선택)
Mode (세로/가로 선택)
    ↓ (선택)
Select (프레임 선택) + Chroma (배경 선택, 실시간 합성)
    ↓ (선택)
Quantity (수량 선택, 1~10장)
    ↓ (선택)
Payment (결제)
    ↓ (결제 완료)
Filming (4컷 촬영)
    ↓ (촬영 완료)
CutWindow (사진 편집/확인)
    ↓ (편집 완료)
Printing (인쇄 + QR 생성)
    ↓ (인쇄 완료)
Ready (처음 화면으로)
```

---

## 주요 컴포넌트

### 1. 게임 상태 관리 (GameManager)

**싱글톤 패턴**으로 키오스크 전체 상태를 관리합니다.

```csharp
public enum KioskState
{
    Ready,                     // 대기 화면
    Mode,                      // 세로/가로 모드 선택
    Chroma,                    // 크로마키 배경 선택
    Select,                    // 프레임 선택
    Quantity,                  // 수량 선택
    Payment,                   // 결제 화면
    WaitingForPayment,         // 결제 대기
    Filming,                   // 사진 촬영
    CutWindow,                 // 4컷 편집
    Printing                   // 인쇄 중
}
```

### 2. 화면 전환 제어 (FadeAnimationCtrl)

모든 화면 전환은 "페이드 애니메이션"을 사용합니다. 페이드 상태는 `FadeState` enum으로 관리되며, 각 화면 간의 전환을 부드럽게 처리합니다.

### 3. 웹캠 프리뷰 (WebcamPreview)

- **프리뷰 해상도**: 1920x1080 FHD @ 30fps
- **캡처 해상도**: 3840x2160 4K (지원 카메라일 경우 자동 전환)
- **Pre-Initialize 모드**: 게임 시작 시 미리 초기화하여 렉 방지
- **페이드 전환**: 활성화 시 부드럽게 표시

### 4. 가상 배경 합성 (VirtualBackgroundController)

- **MediaPipe Selfie Segmentation** 기반
- 실시간 배경 합성 (Unity Barracuda)
- 3종 배경 이미지 제공

### 5. 인쇄 제어 (PrintController)

- **출력 해상도**: 3200x4800 (고품질)
- **합성 방식**: CPU 픽셀 직접 합성 (색공간 변환 없음, 원본 색상 보존)
- **리샘플링 모드**: Cover, Fit, ExactCrop, Stretch, ScaleUp
- **스티커 지원**: 투명 배경 스티커 알파 블렌딩
- PhotoPrinterBridge.exe 연동 (DS-RX1 프린터)

### 6. 스티커 시스템 (StickerPanelCtrl)

- 드래그 앤 드롭으로 스티커 배치
- 스티커 삭제 영역 지원
- 투명 배경 PNG 스티커 지원

---

## 빌드 및 실행

### 필수 요구사항

- Unity 2021.3 이상
- Windows 10/11
- DS-RX1 프린터
- PhotoPrinterBridge.exe (별도 제공)

### 빌드

1. Unity에서 `File → Build Settings` 열기
2. Platform: `PC, Mac & Linux Standalone` 선택
3. `Build` 클릭

### 실행

1. 빌드된 `.exe` 실행
2. 프린터 드라이버 확인
3. (선택) `StreamingAssets/ncp_secrets.json` 설정 (QR 업로드 사용 시)

---

## 문제 해결

### 웹캠이 나오지 않는 경우

1. Inspector에서 `Webcam Target`, `Activation Check Object` 드래그 확인
2. Console 로그에서 초기화 여부 확인
3. Windows 카메라 권한 확인

### 화질이 나쁜 경우

1. **LED 링 라이트 추가 권장** (가장 효과적)
2. USB 3.0 포트 사용
3. 카메라 설정 확인

### 프린터가 동작하지 않는 경우

1. PhotoPrinterBridge.exe 경로 확인
2. DS-RX1 프린터 전원/드라이버 확인
3. Console 로그 확인

---

## 최근 개선사항

### 2026-01
- **고해상도 캡처**: 4K 웹캠 지원 시 자동 전환 (프리뷰 FHD → 캡처 4K)
- **스티커 투명 배경 문제 해결**: CPU 픽셀 직접 합성 방식으로 변경
- **스티커 드래그앤드롭 시스템 추가**
- **출력 해상도 향상**: 1600x2400 → 3200x4800

### 2025-12
- 크로마키 경계 선명도 개선
- 가로모드 프린터 출력 수정 (DS-RX1 Landscape 미지원 대응)
- 10번 클릭 시 최소화 기능 추가
- WebcamPreview FHD 고정 + Pre-Initialize 모드
- FadeAnimation enum 캡슐화

---

**최종 업데이트**: 2026-01-06
