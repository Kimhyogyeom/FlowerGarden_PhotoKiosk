using UnityEngine;

public class SwingRotateHandle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float _amplitude = 20f;    // 최대 각도 (±값)
    [SerializeField] private float _speed = 1.5f;       // 회전 속도

    [Header("Object Setting")]
    [SerializeField] private Transform _handleTr;
    [SerializeField] private GameObject _parentObject;

    [Tooltip("true면 회전 일시정지")]
    public bool _animIsStopFlag = false;

    private float _baseZ;    // 시작 Z 각도
    private float _time;     // 내부용 타이머 (Time.time 대신)

    private void Start()
    {
        if (_handleTr == null)
            _handleTr = transform;

        _baseZ = _handleTr.localEulerAngles.z;
        _time = 0f;
    }

    private void Update()
    {
        // 부모 비활성화면 그냥 리턴 (타이머도 안 증가 → 현재 각도 유지)
        if (_parentObject != null && !_parentObject.activeInHierarchy)
            return;

        // 일시정지면 타이머 안 움직이고, 현재 각도 그대로 둠
        if (_animIsStopFlag)
            return;

        // 여기서만 시간 진행 → 재개 시 이어서 부드럽게
        _time += Time.deltaTime;

        float angleOffset = Mathf.Sin(_time * _speed) * _amplitude;
        float z = _baseZ + angleOffset;

        _handleTr.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}
