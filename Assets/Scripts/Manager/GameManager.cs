using UnityEngine;

/// <summary>
/// 키오스크 상태 정의
/// - 전체 흐름: 결제 → 대기 → 프레임 선택 → 촬영 → 인쇄
/// </summary>
public enum KioskState
{
    WaitingForPayment,  // 결제 대기 화면 (결제 진행 전 상태)
    Ready,              // 대기 화면 (시작/촬영 준비 상태)
    Select,             // 프레임/옵션 선택 화면
    Frame,              // 프레임 상세 선택/편집 상태
    Filming,            // 사진 촬영 진행 중 상태
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
