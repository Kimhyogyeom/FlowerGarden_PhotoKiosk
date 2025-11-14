using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 출력 패널 활성화 브로드캐스터
/// - 이 오브젝트가 활성화될 때(OnEnable) 다음 프레임에 OnOutputEnabled 이벤트를 1회 발사
/// - 같은 프레임에 리스너가 아직 구독되지 않은 경우를 피하기 위해 한 프레임 늦게 호출
/// - Enable/Disable 사이클마다 한 번씩만 이벤트를 쏘도록 보장
/// </summary>
public class OutputEnableBroadcaster : MonoBehaviour
{
    /// <summary>
    /// 출력(예: 인쇄 패널)이 활성화되었음을 알리는 정적 이벤트
    /// - PrintButtonHandler 등에서 이 이벤트를 구독해서,
    ///   패널이 켜지는 타이밍에 카운트다운 시작 등의 로직을 실행
    /// </summary>
    public static event Action OnOutputEnabled;

    private Coroutine _pending;        // 대기 중인 코루틴 참조
    private bool _firedThisEnable;     // 이번 Enable 사이클에서 이미 발사했는지 여부

    private void OnEnable()
    {
        // 새 Enable 사이클 시작 → 발사 여부 플래그 초기화
        _firedThisEnable = false;

        // 이미 대기 코루틴이 있다면 또 시작하지 않음(중복 방지)
        if (_pending == null)
            _pending = StartCoroutine(InvokeNextFrame());
    }

    private void OnDisable()
    {
        // 비활성화되면 대기 중이던 코루틴 정리 (비활성화된 뒤 유령 이벤트가 나가지 않도록)
        if (_pending != null)
        {
            StopCoroutine(_pending);
            _pending = null;
        }
        _firedThisEnable = false;
    }

    /// <summary>
    /// 다음 프레임에 OnOutputEnabled 이벤트를 발사하는 코루틴
    /// - 같은 프레임에 리스너(AddListener/+=) 등록이 끝나지 않은 상태를 피하기 위함
    /// </summary>
    private IEnumerator InvokeNextFrame()
    {
        // “같은 프레임에 구독 미완료” 문제 회피
        yield return null; // 다음 프레임까지 대기 (필요하면 WaitForEndOfFrame() 로 더 늦출 수도 있음)

        if (!_firedThisEnable)
        {
            _firedThisEnable = true;
            OnOutputEnabled?.Invoke();
        }

        _pending = null;
    }
}
