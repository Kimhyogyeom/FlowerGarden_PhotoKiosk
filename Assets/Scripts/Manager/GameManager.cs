using UnityEngine;

/// <summary>
/// 키오스크 상태 정의
/// - 전체 흐름: 결제 → 대기 → 프레임 선택 → 촬영 → 인쇄
/// </summary>
public enum KioskState
{
    Ready,              // 대기 화면 (시작/촬영 준비 상태) // 0-1

    Select,             // 프레임/옵션 선택 화면          // 1-2

    Quantity,           // 수량                         // 2-3

    Payment,            // 결제                         // 3-4

    WaitingForPayment,  // 결제 대기 화면 (결제 진행 전 상태) // 4-5

    Filming,            // 사진 촬영 진행 중 상태

    CutWindow,          // 4컷 화면

    Printing            // 인쇄 진행 중 상태
}

/// <summary>
/// 키오스크 전체 흐름을 관리하는 GameManager
/// - 현재 상태를 저장하고, 상태 변경 시 로그 출력
/// - 전역에서 접근 가능한 싱글톤(Instance)으로 사용
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 전역에서 접근하는 GameManager 인스턴스
    /// </summary>
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private KioskState _currentState = KioskState.WaitingForPayment; // 현재 키오스크 상태

    /// <summary>
    /// 현재 키오스크 상태 읽기 전용 프로퍼티
    /// </summary>
    public KioskState CurrentState => _currentState;

#pragma warning disable CS0414
    [Range(1f, 10f)]
    [Header("TimeScale Value")]
    [Tooltip("기본 : 1, 최대 : 10 (테스트용 타임스케일 조절)")]
    [SerializeField] private float _timeScale = 1.0f;
#pragma warning restore CS0414

    [Header("Test 삭제될 예정 Timer")]
    // ───────────────────────────────────────────── Test용 삭제될 예정
    public int _paymentToReadyTimer = 10;           // 결제 -> 래디 (현)
    public int _readyToSelectTimer = 10;            // 래디 -> 선택 (현)
    public int _selectToFilmingTimer = 10;          // 선택 -> 사진 (현)
    public int _filmingToPhotoTimer = 10;           // 사진 -> 촬영 (현)

    [Header("사용중인 Timer")]
    // ───────────────────────────────────────────── 사용중임
    public int _photoSelectToPrintTimer = 10;   // 포토선택 에서 프린트로 가는 타이머
    public int _printToSuccessTimer = 10;       // 프린트 완료 -> 래디로 가는 타이머

    private void Awake()
    {
        // 싱글톤 보장: 이미 다른 인스턴스가 있으면 자신을 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 여러 씬을 쓴다면 주석 해제해서 유지할 수도 있음
        // DontDestroyOnLoad(gameObject); 

        // ─────────────────────────────────────────────────────────
        // 테스트용: 게임 전체 타임스케일 초기화
        Time.timeScale = _timeScale;
        // Debug.Log($"TimeScale {Time.timeScale}");
        // ─────────────────────────────────────────────────────────
    }

    /// <summary>
    /// 키오스크 상태 변경
    /// - 내부 상태를 갱신하고, 디버그 로그로 상태 전환을 출력
    /// </summary>
    /// <param name="newState">변경할 상태</param>
    public void SetState(KioskState newState)
    {
        _currentState = newState;
        Debug.Log($"[KIOSK] State -> {newState}");
    }

    /// <summary>
    /// 현재 상태가 특정 상태인지 확인하는 헬퍼
    /// </summary>
    /// <param name="state">비교할 상태</param>
    /// <returns>현재 상태가 인자로 넘긴 상태와 같으면 true</returns>
    public bool Is(KioskState state) => _currentState == state;
}
