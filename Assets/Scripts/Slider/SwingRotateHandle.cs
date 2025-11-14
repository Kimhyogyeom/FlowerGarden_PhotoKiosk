using UnityEngine;

/// <summary>
/// 손잡이(혹은 포인터 등)를 좌우로 살짝 흔들리게 만드는 회전 컨트롤러
/// - sin 파형으로 좌우 회전
/// - 부모 비활성화 시, 혹은 _animIsStopFlag 가 true 일 때는 회전 일시정지
/// </summary>
public class SwingRotateHandle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float _amplitude = 20f;    // 최대 각도 (±값, 예: 20 → -20도 ~ +20도)
    [SerializeField] private float _speed = 1.5f;       // 회전 속도 (값이 클수록 더 빠르게 흔들림)

    [Header("Object Setting")]
    [SerializeField] private Transform _handleTr;
    // 회전을 적용할 실제 대상 Transform
    // 비워두면 자신의 Transform을 자동으로 사용

    [SerializeField] private GameObject _parentObject;
    // 부모 오브젝트 (이 오브젝트가 비활성화되면 회전 업데이트를 멈춤)

    [Tooltip("true면 회전 일시정지")]
    public bool _animIsStopFlag = false;
    // 외부에서 true/false 로 제어하는 회전 정지 플래그
    // true → Update 에서 회전 각도 계산을 멈추고 현재 각도를 유지

    private float _baseZ;    // 시작 시점의 Z 각도 (회전 기준값)
    private float _time;     // 내부용 타이머 (Time.time 대신 별도 누적)

    private void Start()
    {
        // 회전 대상이 설정되지 않았다면 자신을 기본으로 사용
        if (_handleTr == null)
            _handleTr = transform;

        // 시작 시점의 Z 축 각도를 기준값으로 저장
        _baseZ = _handleTr.localEulerAngles.z;
        _time = 0f;
    }

    private void Update()
    {
        // 부모 오브젝트가 비활성화라면 아무 것도 하지 않음
        // (타이머도 증가시키지 않아, 다시 활성화될 때 현재 각도 유지)
        if (_parentObject != null && !_parentObject.activeInHierarchy)
            return;

        // 일시정지 플래그가 true면 회전 업데이트 중단
        if (_animIsStopFlag)
            return;

        // 회전 애니메이션을 위한 시간 누적
        // (일시정지 중에는 증가하지 않아서 재개 시 자연스럽게 이어짐)
        _time += Time.deltaTime;

        // sin 파형을 이용해 -amplitude ~ +amplitude 범위 각도 계산
        float angleOffset = Mathf.Sin(_time * _speed) * _amplitude;
        float z = _baseZ + angleOffset;

        // Z 축 기준으로 좌우 회전 적용
        _handleTr.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}
